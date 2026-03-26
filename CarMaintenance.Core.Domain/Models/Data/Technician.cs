using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Technician : BaseEntity<string>
    {
        public string Specialization { get; set; } = null!;
        public decimal Rating { get; set; }
        public bool IsAvailable { get; set; }
        public int ExperienceYears { get; set; }

        // FK to User
        public string UserId { get; set; } = null!; 

        // Navigation Properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
        public ICollection<Booking> AssignedBookings { get; set; } = new List<Booking>();
    }
}