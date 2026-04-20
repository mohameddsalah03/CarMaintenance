using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Shared.DTOs.Payment.Auth;
using CarMaintenance.Shared.DTOs.Payment.Callback;
using CarMaintenance.Shared.DTOs.Payment.Order;
using CarMaintenance.Shared.DTOs.Payment.PaymentKey;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.PaymobServices
{
    public class PaymobService(
        HttpClient _httpClient,
        IOptions<PaymobSettings> _options    
        ) : IPaymobService
    {
        private readonly PaymobSettings _settings = _options.Value;
        private const string BaseUrl = "https://accept.paymob.com/api";

        public async Task<string> GetAuthTokenAsync()
        {
            var body = new PaymobAuthRequest { ApiKey = _settings.ApiKey };
            var response = await PostAsync<PaymobAuthResponse>("/auth/tokens", body);

            if (string.IsNullOrEmpty(response?.Token))
                throw new BadRequestException("Paymob: فشل الحصول على Auth Token");

            return response.Token;
        }

        public async Task<int> CreateOrderAsync( string authToken,int amountCents,string merchantOrderId)
        {
            var body = new PaymobOrderRequest
            {
                AuthToken = authToken,
                AmountCents = amountCents,
                MerchantOrderId = merchantOrderId
            };

            var response = await PostAsync<PaymobOrderResponse>( "/ecommerce/orders", body);

            if (response == null || response.Id == 0)
                throw new BadRequestException("Paymob: فشل إنشاء الطلب");
            return response.Id;
        }

        public async Task<string> GetPaymentKeyAsync(
            string authToken,
            int orderId,
            int amountCents,
            string integrationId,
            string email,
            string phone)
        {
            var body = new PaymobPaymentKeyRequest
            {
                AuthToken = authToken,
                OrderId = orderId,
                AmountCents = amountCents,
                IntegrationId = int.Parse(integrationId),
                BillingData = new PaymobBillingData
                {
                    Email = email,
                    PhoneNumber = phone
                }
            };

            var response = await PostAsync<PaymobPaymentKeyResponse>(
                "/acceptance/payment_keys", body);

            if (string.IsNullOrEmpty(response?.Token))
                throw new BadRequestException("Paymob: فشل الحصول على Payment Key");

            return response.Token;
        }

        public string BuildIFrameUrl(string paymentToken)
        {
            return $"https://accept.paymob.com/api/acceptance/iframes/" + 
                $"{_settings.IFrameId}?payment_token={paymentToken}";
        }

        public bool VerifyHmac(PaymobTransactionObj obj, string receivedHmac)
        {
            if (string.IsNullOrEmpty(receivedHmac)) return false;

            try
            {
                var data = string.Concat(
                    obj.AmountCents,
                    obj.CreatedAt,
                    obj.Currency,
                    obj.ErrorOccured.ToString().ToLower(),
                    obj.HasParentTransaction.ToString().ToLower(),
                    obj.Id,
                    obj.IntegrationId,
                    obj.Is3dSecure.ToString().ToLower(),
                    obj.IsAuth.ToString().ToLower(),
                    obj.IsCapture.ToString().ToLower(),
                    obj.IsRefunded.ToString().ToLower(),
                    obj.IsStandalonePayment.ToString().ToLower(),
                    obj.IsVoided.ToString().ToLower(),
                    obj.Order?.Id,
                    obj.Owner,
                    obj.Pending.ToString().ToLower(),
                    obj.SourceData?.Pan ?? "NA",
                    obj.SourceData?.SubType ?? "NA",
                    obj.SourceData?.Type ?? "NA",
                    obj.Success.ToString().ToLower()
                );

                using var hmac = new HMACSHA512(
                    Encoding.UTF8.GetBytes(_settings.HmacSecret));

                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var computed = Convert.ToHexString(hash).ToLower();
                var isValid = computed == receivedHmac.ToLower();

                return isValid;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<T?> PostAsync<T>(string endpoint, object body)
        {
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}{endpoint}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new BadRequestException($"Paymob API Error: {response.StatusCode}");
            }

            return JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}