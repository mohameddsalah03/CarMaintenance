namespace CarMaintenance.Shared.DTOs.AI.Response
{
    
    public class AiDiagnosisResponseDto
    {
        public string Status { get; set; } = null!;
        public List<AiRecommendedServiceDto> RecommendedServices { get; set; } = new();
        public string? Message { get; set; }
    }
}