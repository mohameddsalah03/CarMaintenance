using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class BookingService : BaseEntity<int>
    {
        // Composite Key 
        public int BookingId { get; set; }
        public int ServiceId { get; set; }

        public int Duration { get; set; }
        public BookingStatus Status { get; set; }

        // Navigation Properties
        public Booking Booking { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}