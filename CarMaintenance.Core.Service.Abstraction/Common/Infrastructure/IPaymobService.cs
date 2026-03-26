using CarMaintenance.Shared.DTOs.Payment.Callback;

namespace CarMaintenance.Core.Service.Abstraction.Common.Infrastructure
{
    public interface IPaymobService
    {
        Task<string> GetAuthTokenAsync();

        Task<int> CreateOrderAsync(
            string authToken,
            int amountCents,
            string merchantOrderId);

        Task<string> GetPaymentKeyAsync(
            string authToken,
            int orderId,
            int amountCents,
            string integrationId,
            string email,
            string phone);

        string BuildIFrameUrl(string paymentToken);

        bool VerifyHmac(PaymobTransactionObj transaction, string receivedHmac);
    }
}