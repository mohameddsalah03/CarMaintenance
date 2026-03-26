using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Payment.PaymentKey
{
    public class PaymobPaymentKeyResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = null!;
    }
}
