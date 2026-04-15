namespace CarMaintenance.Shared.DTOs.Services.AnalyzeProblem
{
   
    public class AnalyzeProblemResponseDto
    {
        public string Status { get; set; } = null!;
        public List<ValidatedServiceSuggestionDto> SuggestedServices { get; set; } = new();
        public string? Message { get; set; }
    }
}