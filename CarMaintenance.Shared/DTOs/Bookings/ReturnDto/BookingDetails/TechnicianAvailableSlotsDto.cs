namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class TechnicianAvailableSlotsDto
    {
        public string TechnicianId { get; set; } = null!;
        public string TechnicianName { get; set; } = null!;
        public List<AvailableSlotDto> AvailableSlots { get; set; } = new();
    }
}