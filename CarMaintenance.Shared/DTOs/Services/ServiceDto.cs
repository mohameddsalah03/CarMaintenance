namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int EstimatedDurationMinutes { get; set; }

    }
}
