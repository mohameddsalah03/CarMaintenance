using CarMaintenance.Shared.DTOs.Payment;

namespace CarMaintenance.Core.Service.Abstraction.Services.Payments
{
    public interface IPaymentService
    {
        Task<PaymentInitiatedDto> InitiatePaymentAsync(InitiatePaymentDto dto, string userId);
        Task HandleCallbackAsync(string rawBody, string hmac);
    }
}