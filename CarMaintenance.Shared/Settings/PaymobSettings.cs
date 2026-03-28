namespace CarMaintenance.Shared.Settings
{
    public class PaymobSettings
    {
        // من Paymob Dashboard → Settings → Account Info
        public required string ApiKey { get; set; }

        // من Paymob Dashboard → Developers → Payment Integrations
        public required string CardIntegrationId { get; set; }
        public required string VodafoneIntegrationId { get; set; }

        // من Paymob Dashboard → Developers → iFrames
        public required string IFrameId { get; set; }

        // من Paymob Dashboard → Settings → Security Settings
        public required string HmacSecret { get; set; }
    }
}