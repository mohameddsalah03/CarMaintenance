namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class AvailableSlotDto
    {
        public DateTime SlotDateTime { get; set; }
        public string Label { get; set; } = null!;
    }
}