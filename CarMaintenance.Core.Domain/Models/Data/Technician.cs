using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Technician : BaseEntity<string>
    {
        public string Specialization { get; set; } = null!;
        public decimal Rating { get; set; }
        public bool IsAvailable { get; set; }
        
        //
        //public string UserId { get; set; } = null!; // FK to User


        // Navigation Properties
        public ICollection<Review>? ReviewsReceived { get; set; } = new List<Review>();
        public ICollection<Booking> AssignedBookings { get; set; } = new List<Booking>();
    }
}