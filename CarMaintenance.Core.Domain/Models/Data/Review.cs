using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Review : BaseEntity<int>
    {
        public decimal Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign Keys
        public string UserId { get; set; } = null!;
        public string TechnicianId { get; set; } = null!;
        public int BookingId { get; set; }

        // Navigation Properties
        public ApplicationUser User { get; set; } = null!;
        public Technician Technician { get; set; } = null!;
        public Booking Booking { get; set; } = null!;
    }
}