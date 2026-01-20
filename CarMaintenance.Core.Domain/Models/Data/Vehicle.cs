using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Vehicle : BaseEntity<int>
    {
        public string Model { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public int Year { get; set; }
        public string PlateNumber { get; set; } = null!;

        // Foreign Key
        public string UserId { get; set; } = null!;

        // Navigation Properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}