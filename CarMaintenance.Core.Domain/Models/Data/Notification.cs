using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Notification : BaseEntity<int>
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public NotificationType Type { get; set; }

        // URL the frontend navigates to when clicking the notification
        public string? ActionUrl { get; set; }

        // Foreign Key
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
} 