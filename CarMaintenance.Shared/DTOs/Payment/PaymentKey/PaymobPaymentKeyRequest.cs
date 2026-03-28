using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.PaymentKey
{
    public class PaymobPaymentKeyRequest
    {
        [JsonPropertyName("auth_token")]
        public required string AuthToken { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("expiration")]
        public int Expiration { get; set; } = 3600;

        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [JsonPropertyName("billing_data")]
        public required PaymobBillingData BillingData { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "EGP";

        [JsonPropertyName("integration_id")]
        public int IntegrationId { get; set; }
    }
}
