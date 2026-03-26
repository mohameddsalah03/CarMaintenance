using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.Auth
{
    public class PaymobAuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = null!;
    }
}
