using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Notification : BaseEntity<int>
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign Key
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}