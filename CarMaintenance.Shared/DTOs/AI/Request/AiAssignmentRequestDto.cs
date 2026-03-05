namespace CarMaintenance.Shared.DTOs.AI.Request
{
    public class AiAssignmentRequestDto
    {
        public int BookingId { get; set; }
        public List<AiServiceInfoDto> Services { get; set; } = new();
        public DateTime ScheduledDate { get; set; }
        public string Priority { get; set; } = "normal";
    }

   
}