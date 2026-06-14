using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;

namespace CarMaintenance.Shared.DTOs.Bookings.AvailableSlots
{
    public class AvailableSlotsResponseDto
    {
        public List<int> ServiceIds { get; set; } = new();
        public int TotalDurationMinutes { get; set; }
        public List<TechnicianWithSlotsDto> Technicians { get; set; } = new();
    }

    public class TechnicianWithSlotsDto
    {
        public string TechnicianId { get; set; } = null!;
        public string TechnicianName { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public decimal Rating { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsFullMatch { get; set; }
        public List<AvailableSlotDto> AvailableSlots { get; set; } = new();
    }
}