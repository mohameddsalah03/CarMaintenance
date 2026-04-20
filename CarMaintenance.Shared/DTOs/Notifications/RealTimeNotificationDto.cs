namespace CarMaintenance.Shared.DTOs.Notifications
{
    public class RealTimeNotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
