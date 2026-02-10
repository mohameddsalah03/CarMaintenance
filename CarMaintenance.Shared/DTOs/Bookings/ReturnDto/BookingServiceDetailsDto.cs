namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto
{
    public class BookingServiceDetailsDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal ServicePrice { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = null!;
    }
}
