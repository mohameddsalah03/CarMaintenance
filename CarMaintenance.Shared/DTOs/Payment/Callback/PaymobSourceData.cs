using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Callback
{
    public class PaymobSourceData
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("sub_type")]
        public string? SubType { get; set; }

        [JsonPropertyName("pan")]
        public string? Pan { get; set; }
    }
}