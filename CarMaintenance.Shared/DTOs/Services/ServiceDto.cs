namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Description { get; set; } = null!;

        public decimal BasePrice { get; set; }
        public int EstimatedDurationMinutes { get; set; }

        public List<string> IncludedItems { get; set; } = new();
        public List<string> ExcludedItems { get; set; } = new();
        public List<string> Requirements { get; set; } = new();

        /// <summary>
        /// Average technician + service rating for bookings that used this service.
        /// Shown on the service listing and detail pages.
        /// </summary>
        public double AverageRating { get; set; }

        /// <summary>Total number of reviews related to this service.</summary>
        public int ReviewCount { get; set; }
    }
}