using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Callback
{
    public class PaymobCallbackDto
    {
        [JsonPropertyName("obj")]
        public PaymobTransactionObj? Obj { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
