namespace CarMaintenance.Shared.DTOs.Notifications
{
    public class RealTimeNotificationDto //Used when sending real-time notification via SignalR
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
