namespace CarMaintenance.Shared.DTOs.AI.Response
{
    public class AiAlternativeDto
    {
        public string TechnicianId { get; set; } = null!;
        public double Confidence { get; set; }
        public string? Reason { get; set; }
    }
}
