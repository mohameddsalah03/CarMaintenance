namespace CarMaintenance.Shared.DTOs.AI.Response
{
    public class AiFactorsDto
    {
        public double SpecializationMatch { get; set; }
        public double Rating { get; set; }
        public string Workload { get; set; } = null!;
    }
}
