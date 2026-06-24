using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Core.Service.Abstraction.Services.Payments;
using CarMaintenance.Shared.DTOs.Payment;
using CarMaintenance.Shared.DTOs.Payment.Callback;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarMaintenance.Core.Service.Services.Payments
{
    public class PaymentService(
        IPaymobService _paymobService,
        IUnitOfWork _unitOfWork,
        IOptions<PaymobSettings> _paymobOptions,
        INotificationService _notificationService
    ) : IPaymentService
    {
        private readonly PaymobSettings _paymobSettings = _paymobOptions.Value;

        #region Public API

        public async Task<PaymentInitiatedDto> InitiatePaymentAsync(InitiatePaymentDto dto, string userId)
        {
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(dto.BookingId));

            if (booking is null)
                throw new NotFoundException(nameof(Booking), dto.BookingId);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية للدفع على هذا الحجز");

            if (booking.PaymentStatus == PaymentStatus.Paid)
                throw new BadRequestException("هذا الحجز مدفوع بالفعل");

            // not completed 
            if (booking.Status != BookingStatus.Completed)
            {
                var reason = booking.Status switch
                {
                    BookingStatus.Pending => "الحجز لم يبدأ بعد",
                    BookingStatus.InProgress => "الحجز قيد التنفيذ — الدفع بعد الانتهاء",
                    BookingStatus.WaitingClientApproval => "يوجد تكلفة إضافية في انتظار ردك",
                    BookingStatus.Cancelled => "لا يمكن دفع حجز ملغى",
                    _ => $"الحالة '{booking.Status}' لا تسمح بالدفع"
                };
                throw new BadRequestException(reason);
            }

            // validate on paymentMethod  // convert string to enum 
            if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method))
                throw new BadRequestException($"طريقة دفع غير صحيحة: '{dto.PaymentMethod}'. القيم المقبولة: Cash, CreditCard");


            var correctCost = booking.BookingServices.Sum(bs => bs.Service?.BasePrice ?? 0)
                            + booking.AdditionalIssues.Where(ai => ai.Status == AdditionalIssueStatus.Approved)
                                                      .Sum(ai => ai.EstimatedCost);

            if (correctCost > 0 && booking.TotalCost != correctCost)
                booking.TotalCost = correctCost;

            booking.PaymentMethod = method;

            // 7️ Cash flow 
            if (method == PaymentMethod.Cash)
            {
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.PaidAt = DateTime.UtcNow;                  
                booking.PaymentProcessedByUserId = userId;         

                _unitOfWork.GetRepo<Booking, int>().Update(booking);
                await _unitOfWork.SaveChangesAsync();

                await _notificationService.SendAsync(
                    userId: userId,
                    title: "تم تأكيد الدفع نقداً",
                    message: $"تم تسجيل دفع الحجز {booking.BookingNumber} نقداً ({booking.TotalCost} ج.م).",
                    type: NotificationType.PaymentCompleted,
                    actionUrl: $"/bookings/{booking.Id}");

                return new PaymentInitiatedDto
                {
                    IFrameUrl = string.Empty,
                    PaymentToken = "cash" // paymob معناها مفيش  
                };
            }

            // 8 CreditCard flow 
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            return await InitiatePaymobPaymentAsync(booking);
        }

        public async Task HandleCallbackAsync(string rawBody, string hmac)
        {
            PaymobCallbackDto? callback;
            try
            {
                callback = JsonSerializer.Deserialize<PaymobCallbackDto>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return;
            }

            if (callback?.Type != "TRANSACTION" || callback.Obj == null)
                return;

            if (!_paymobService.VerifyHmac(callback.Obj, hmac))
                throw new BadRequestException("Invalid HMAC signature");

            var merchantOrderId = callback.Obj.Order?.MerchantOrderId;
            if (string.IsNullOrEmpty(merchantOrderId))
                return;

            var bookingNumber = merchantOrderId.Replace("FIXORA-", "");
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingByNumberSpecification(bookingNumber));

            if (booking == null)
                return;

            // Idempotency
            if (booking.PaymentStatus == PaymentStatus.Paid)
                return;

            bool isSuccess = callback.Obj.Success && !callback.Obj.Pending;
            bool isFailure = !callback.Obj.Success && !callback.Obj.Pending;

            if (isSuccess)
            {
                booking.PaymentStatus = PaymentStatus.Paid;
                booking.PaymobTransactionId = callback.Obj.Id.ToString();   
                booking.PaidAt = DateTime.UtcNow;                           
                                                                            
            }
            else if (isFailure)
            {
                booking.PaymentStatus = PaymentStatus.Failed;
            }
            else
            {
                return;  // Pending 
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            
            if (isSuccess)
            {
                await _notificationService.SendAsync(
                    userId: booking.UserId,
                    title: "تم تأكيد الدفع",
                    message: $"تم استلام دفع الحجز {booking.BookingNumber} ({booking.TotalCost} ج.م).",
                    type: NotificationType.PaymentCompleted,
                    actionUrl: $"/bookings/{booking.Id}");
            }
            else
            {
                await _notificationService.SendAsync(
                    userId: booking.UserId,
                    title: "فشل الدفع",
                    message: $"فشلت عملية دفع الحجز {booking.BookingNumber}. يمكنك المحاولة مرة أخرى.",
                    type: NotificationType.PaymentFailed,
                    actionUrl: $"/bookings/{booking.Id}");
            }
        }

        #endregion

        #region Private Helpers

        
        private async Task<PaymentInitiatedDto> InitiatePaymobPaymentAsync(Booking booking)
        {
            var integrationId = _paymobSettings.CardIntegrationId;

            var amountCents = (int)(booking.TotalCost * 100);
            var email = booking.User?.Email ?? "noemail@fixora.com";
            var phone = booking.User?.PhoneNumber ?? "01000000000";
            var merchantOrderId = $"FIXORA-{booking.BookingNumber}";

            var authToken = await _paymobService.GetAuthTokenAsync();
            var orderId = await _paymobService.CreateOrderAsync(authToken, amountCents, merchantOrderId);
            var paymentToken = await _paymobService.GetPaymentKeyAsync(authToken, orderId, amountCents, integrationId, email, phone);

            return new PaymentInitiatedDto
            {
                IFrameUrl = _paymobService.BuildIFrameUrl(paymentToken),
                PaymentToken = paymentToken
            };
        }

        #endregion
    }
}