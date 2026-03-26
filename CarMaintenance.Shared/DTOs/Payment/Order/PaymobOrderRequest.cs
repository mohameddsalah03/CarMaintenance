using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Order
{
    public class PaymobOrderRequest
    {
        [JsonPropertyName("auth_token")]
        public required string AuthToken { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "EGP";

        [JsonPropertyName("delivery_needed")]
        public bool DeliveryNeeded { get; set; } = false;

        [JsonPropertyName("items")]
        public List<object> Items { get; set; } = new();

        [JsonPropertyName("merchant_order_id")]
        public string? MerchantOrderId { get; set; }
    }
}
