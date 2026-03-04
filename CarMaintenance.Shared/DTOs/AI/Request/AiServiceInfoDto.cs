namespace CarMaintenance.Shared.DTOs.AI.Request
{
    public class AiServiceInfoDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string Category { get; set; } = null!;
    }
}
