namespace CarMaintenance.Shared.DTOs.AI.Request
{
    public class AiAssignmentRequestDto
    {
        public int BookingId { get; set; }
        public List<AiServiceInfoDto> Services { get; set; } = new();
        public DateTime ScheduledDate { get; set; }
        public string Priority { get; set; } = "normal";

        /// <summary>
        /// Pre-qualified technician candidates with their schedule data.
        /// Sent to the AI so it can factor in actual available minutes per technician.
        /// Only technicians that already passed specialization + capacity validation
        /// on the .NET side are included here.
        /// </summary>
        public List<AiTechnicianCandidateDto> Candidates { get; set; } = new();
    }
}