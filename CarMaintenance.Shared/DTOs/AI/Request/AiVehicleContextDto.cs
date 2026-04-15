namespace CarMaintenance.Shared.DTOs.AI.Request
{
    public class AiVehicleContextDto
    {
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
    }
}