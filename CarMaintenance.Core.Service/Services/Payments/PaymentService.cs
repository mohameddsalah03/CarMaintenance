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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarMaintenance.Core.Service.Services.Payments
{
    public class PaymentService(
        IPaymobService _paymobService,
        IUnitOfWork _unitOfWork,
        IOptions<PaymobSettings> _paymobOptions,
        ILogger<PaymentService> _logger
    ) : IPaymentService
    {
        private readonly PaymobSettings _paymobSettings = _paymobOptions.Value;

        public async Task<PaymentInitiatedDto> InitiatePaymentAsync(
            InitiatePaymentDto dto, string userId)
        {
            // 1. Fetch and validate booking
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

            // 2. Resolve integration ID by payment method
            var integrationId = dto.PaymentMethod.ToLower() switch
            {
                "card" => _paymobSettings.CardIntegrationId,
                "vodafone_cash" => _paymobSettings.VodafoneIntegrationId,
                _ => throw new BadRequestException(
                    "طريقة دفع غير صحيحة. القيم المقبولة: card, vodafone_cash")
            };

            var amountCents = (int)(booking.TotalCost * 100);
            var email = booking.User?.Email ?? "noemail@fixora.com";
            var phone = booking.User?.PhoneNumber ?? "01000000000";
            var merchantOrderId = $"FIXORA-{booking.BookingNumber}";

            // 3. Paymob 3-step flow
            var authToken = await _paymobService.GetAuthTokenAsync();
            var orderId = await _paymobService.CreateOrderAsync(authToken, amountCents, merchantOrderId);
            var paymentToken = await _paymobService.GetPaymentKeyAsync(
                authToken, orderId, amountCents, integrationId, email, phone);

            var iFrameUrl = _paymobService.BuildIFrameUrl(paymentToken);

            _logger.LogInformation(
                "[Payment] Initiated for Booking {BookingNumber}, Amount: {Amount} cents",
                booking.BookingNumber, amountCents);

            return new PaymentInitiatedDto
            {
                IFrameUrl = iFrameUrl,
                PaymentToken = paymentToken
            };
        }

        public async Task HandleCallbackAsync(string rawBody, string hmac)
        {
            // 1. Deserialize callback
            PaymobCallbackDto? callback;
            try
            {
                callback = JsonSerializer.Deserialize<PaymobCallbackDto>(rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Payment] Failed to deserialize Paymob callback body.");
                return;
            }

            // 2. Only handle TRANSACTION type
            if (callback?.Type != "TRANSACTION" || callback.Obj == null)
            {
                _logger.LogInformation("[Payment] Ignored non-TRANSACTION callback: {Type}", callback?.Type);
                return;
            }

            // 3. Verify HMAC
            if (!_paymobService.VerifyHmac(callback.Obj, hmac))
            {
                _logger.LogWarning("[Payment] HMAC mismatch — possible fake callback rejected.");
                throw new BadRequestException("Invalid HMAC signature");
            }

            // 4. Resolve booking from MerchantOrderId
            var merchantOrderId = callback.Obj.Order?.MerchantOrderId;
            if (string.IsNullOrEmpty(merchantOrderId))
            {
                _logger.LogWarning("[Payment] Callback received with no MerchantOrderId.");
                return;
            }

            // "FIXORA-BK-20260325-A3F9C2" → "BK-20260325-A3F9C2"
            var bookingNumber = merchantOrderId.Replace("FIXORA-", "");
            var bookingSpec = new BookingByNumberSpecification(bookingNumber);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(bookingSpec);

            if (booking == null)
            {
                _logger.LogWarning("[Payment] No booking found for BookingNumber: {BookingNumber}", bookingNumber);
                return;
            }

            // 5. Idempotency — skip if already processed
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                _logger.LogInformation(
                    "[Payment] Duplicate callback ignored — Booking {BookingNumber} already marked Paid.",
                    bookingNumber);
                return;
            }

            // 6. Update payment status
            if (callback.Obj.Success && !callback.Obj.Pending)
            {
                booking.PaymentStatus = PaymentStatus.Paid;
                _logger.LogInformation("[Payment] ✅ Booking {BookingNumber} marked as Paid.", bookingNumber);
            }
            else if (!callback.Obj.Success && !callback.Obj.Pending)
            {
                booking.PaymentStatus = PaymentStatus.Failed;
                _logger.LogWarning("[Payment] ❌ Booking {BookingNumber} payment Failed.", bookingNumber);
            }
            else
            {
                _logger.LogInformation("[Payment] Booking {BookingNumber} payment still Pending.", bookingNumber);
                return; // Don't update status for pending transactions
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}