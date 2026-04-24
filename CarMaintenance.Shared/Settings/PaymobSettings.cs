namespace CarMaintenance.Shared.Settings
{
    public class PaymobSettings
    {
        public required string ApiKey { get; set; }
        public required string CardIntegrationId { get; set; }

        public required string WalletIntegrationId { get; set; }

        public required string IFrameId { get; set; }
        public required string HmacSecret { get; set; }
    }
}