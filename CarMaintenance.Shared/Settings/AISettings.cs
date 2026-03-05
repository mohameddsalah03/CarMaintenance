namespace CarMaintenance.Shared.Settings
{
    public class AISettings
    {
        public required string TechnicianAssignmentUrl { get; set; }
        public required string ApiKey { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }
}