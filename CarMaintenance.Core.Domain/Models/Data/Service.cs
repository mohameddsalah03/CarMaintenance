using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class Service : BaseEntity<int>
    {
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int EstimatedDurationMinutes { get; set; }

        // Navigation Properties
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }
}