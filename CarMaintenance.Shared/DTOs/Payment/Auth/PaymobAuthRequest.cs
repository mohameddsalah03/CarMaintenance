using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Auth
{
    public class PaymobAuthRequest
    {
        [JsonPropertyName("api_key")]
        public required string ApiKey { get; set; }
    }

}
