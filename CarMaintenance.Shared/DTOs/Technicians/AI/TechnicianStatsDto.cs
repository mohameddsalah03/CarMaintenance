namespace CarMaintenance.Shared.DTOs.Technicians.AI
{
    public class TechnicianStatsDto
    {
        public string TechnicianId { get; set; } = null!;
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public double SuccessRate { get; set; }
    }
}