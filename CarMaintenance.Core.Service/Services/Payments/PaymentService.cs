using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
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
        IOptions<PaymobSettings> _paymobOptions
    ) : IPaymentService
    {
        private readonly PaymobSettings _paymobSettings = _paymobOptions.Value;

        public async Task<PaymentInitiatedDto> InitiatePaymentAsync( InitiatePaymentDto dto, string userId)
        {
            var spec = new BookingSpecification(dto.BookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), dto.BookingId);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لدفع هذا الحجز");

            if (booking.PaymentStatus == PaymentStatus.Paid)
                throw new BadRequestException("تم دفع هذا الحجز بالفعل");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("لا يمكن دفع حجز ملغى");

            if (booking.PaymentMethod == PaymentMethod.Cash)
                throw new BadRequestException("هذا الحجز مسجل بطريقة الدفع نقداً عند الاستلام. " +  "لا يمكن استخدام الدفع الإلكتروني لهذا الحجز.");

            //
            if (booking.Status == BookingStatus.Pending)
                throw new BadRequestException( "لا يمكن الدفع الآن — الحجز لم يبدأ بعد وقد تتغير التكلفة النهائية.");

            if (booking.Status == BookingStatus.WaitingClientApproval)
                throw new BadRequestException( "لا يمكن الدفع الآن — يوجد تكلفة إضافية في انتظار موافقتك. " + "يرجى الرد على التكلفة الإضافية أولاً.");

            var correctCost = booking.BookingServices.Sum(bs => bs.Service?.BasePrice ?? 0) 
                            + booking.AdditionalIssues.Where(ai => ai.Status == AdditionalIssueStatus.Approved)
                                                      .Sum(ai => ai.EstimatedCost);

            // لو التكلفة المحفوظة مختلفة → نصلحها قبل الدفع
            if (booking.TotalCost != correctCost && correctCost > 0)
            {
                booking.TotalCost = correctCost;
                _unitOfWork.GetRepo<Booking, int>().Update(booking);
                await _unitOfWork.SaveChangesAsync();
            }


            var integrationId = dto.PaymentMethod.ToLower() switch
            {
                "card" => _paymobSettings.CardIntegrationId,
                "wallet" => _paymobSettings.WalletIntegrationId,
                _ => throw new BadRequestException(
                    "طريقة دفع غير صحيحة. القيم المقبولة: card, wallet")
            };

            var amountCents    = (int)(booking.TotalCost * 100);
            var email          = booking.User?.Email        ?? "noemail@fixora.com";
            var phone          = booking.User?.PhoneNumber  ?? "01000000000";
            var merchantOrderId = $"FIXORA-{booking.BookingNumber}";

            var authToken    = await _paymobService.GetAuthTokenAsync();
            var orderId      = await _paymobService.CreateOrderAsync(authToken, amountCents, merchantOrderId);
            var paymentToken = await _paymobService.GetPaymentKeyAsync( authToken, orderId, amountCents, integrationId, email, phone);

            var iFrameUrl = _paymobService.BuildIFrameUrl(paymentToken);
            return new PaymentInitiatedDto
            {
                IFrameUrl    = iFrameUrl,
                PaymentToken = paymentToken
            };
        }

        public async Task HandleCallbackAsync(string rawBody, string hmac)
        {
            PaymobCallbackDto? callback;
            try
            {
                callback = JsonSerializer.Deserialize<PaymobCallbackDto>(rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return;
            }

            if (callback?.Type != "TRANSACTION" || callback.Obj == null)
            {
                return;
            }

            if (!_paymobService.VerifyHmac(callback.Obj, hmac))
            {
                throw new BadRequestException("Invalid HMAC signature");
            }

            var merchantOrderId = callback.Obj.Order?.MerchantOrderId;
            if (string.IsNullOrEmpty(merchantOrderId))
            {
                return;
            }

            var bookingNumber = merchantOrderId.Replace("FIXORA-", "");
            var bookingSpec   = new BookingByNumberSpecification(bookingNumber);
            var booking       = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(bookingSpec);

            if (booking == null)
            {
                return;
            }

            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                return;
            }

            if (callback.Obj.Success && !callback.Obj.Pending)
            {
                booking.PaymentStatus = PaymentStatus.Paid;
            }
            else if (!callback.Obj.Success && !callback.Obj.Pending)
            {
                booking.PaymentStatus = PaymentStatus.Failed;
            }
            else
            {
                return;
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}