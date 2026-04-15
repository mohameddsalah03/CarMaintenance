namespace CarMaintenance.Shared.DTOs.AI.Response
{
    
    public class AiRecommendedServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public double Confidence { get; set; }
    }
}