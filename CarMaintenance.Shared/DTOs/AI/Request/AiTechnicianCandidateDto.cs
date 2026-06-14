namespace CarMaintenance.Shared.DTOs.AI.Request
{
    /// <summary>
    /// Represents a pre-qualified technician candidate sent to the AI assignment service.
    /// Contains schedule/capacity data so the AI can make time-aware decisions.
    /// </summary>
    public class AiTechnicianCandidateDto
    {
        public string TechnicianId { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public double Rating { get; set; }
        public int ExperienceYears { get; set; }

        /// <summary>
        /// Total minutes already booked for this technician on the requested day.
        /// AI uses this to understand actual workload — not just booking count.
        /// </summary>
        public int UsedMinutesOnDay { get; set; }

        /// <summary>
        /// Remaining available minutes for this technician on the requested day.
        /// = 480 - UsedMinutesOnDay
        /// </summary>
        public int AvailableMinutesOnDay { get; set; }

        /// <summary>
        /// Duration in minutes of the new booking being assigned.
        /// Lets AI verify fit without recalculating.
        /// </summary>
        public int RequiredMinutes { get; set; }

        /// <summary>
        /// Number of currently active bookings (not completed/cancelled).
        /// Kept for backward compatibility with the AI workload factor.
        /// </summary>
        public int ActiveBookingsCount { get; set; }
    }
}