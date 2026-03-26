using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Order
{
    public class PaymobOrderResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
