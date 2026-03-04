namespace CarMaintenance.Shared.DTOs.AI.Response
{
    public class AiAssignmentResponseDto
    {
        public string RecommendedTechnicianId { get; set; } = null!;
        public double Confidence { get; set; }
        public string Reason { get; set; } = null!;
        public List<AiAlternativeDto> Alternatives { get; set; } = new();
        public AiFactorsDto? Factors { get; set; }
    }
}
