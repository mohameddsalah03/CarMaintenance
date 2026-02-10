namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto
{
    public class BookingDto
    {
        public int Id { get; set; }

        public string BookingNumber { get; set; } = null!;
        public DateTime ScheduledDate { get; set; }
        public string Description { get; set; } = null!;
        public decimal TotalCost { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;


        // Customer Info
        public string CustomerId { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;

        // Vehicle Info

        public int VehicleId { get; set; }
        public string Model { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string PlateNumber { get; set; } = null!;

        // Technician Info (optional)
        public string TechnicianId { get; set; } = null!;
        public string TechnicianName { get; set; } = null!;

        // Services
        public List<BookingServiceDetailsDto> BookingServiceDetailsDtos { get; set; } = new();
    }
}
