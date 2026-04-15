namespace CarMaintenance.Shared.DTOs.Services.AnalyzeProblem
{
    
    public class ValidatedServiceSuggestionDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int EstimatedDurationMinutes { get; set; }

        public double Confidence { get; set; }
    }
}