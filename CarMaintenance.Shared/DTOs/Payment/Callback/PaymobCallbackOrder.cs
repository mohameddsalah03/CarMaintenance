using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Callback
{
    public class PaymobCallbackOrder
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merchant_order_id")]
        public string? MerchantOrderId { get; set; }

        
    }
}