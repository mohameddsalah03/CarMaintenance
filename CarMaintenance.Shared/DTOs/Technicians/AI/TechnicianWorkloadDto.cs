namespace CarMaintenance.Shared.DTOs.Technicians.AI
{
    public class TechnicianWorkloadDto
    {
        public string TechnicianId { get; set; } = null!;
        public int CurrentWorkload { get; set; }
    }
}