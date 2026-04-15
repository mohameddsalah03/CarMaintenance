namespace CarMaintenance.Shared.DTOs.AI.Request
{
    public class AiDiagnosisRequestDto
    {
        public string ProblemDescription { get; set; } = null!;
        public AiVehicleContextDto? VehicleContext { get; set; }
    }
}