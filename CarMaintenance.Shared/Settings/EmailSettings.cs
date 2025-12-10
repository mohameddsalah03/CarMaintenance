namespace CarMaintenance.Shared.Settings
{
    public class EmailSettings
    {
        public required string From { get; set; }
        public required string SmtpServer { get; set; }
        public required int SmtpPort { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
