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

        public string? TechnicianReport { get; set; }

        // Customer Info
        public string CustomerId { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public string? CustomerEmail { get; set; } 

        // Vehicle Info

        public int VehicleId { get; set; }
        public string Model { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string PlateNumber { get; set; } = null!;

        // Technician Info (optional)
        public string? TechnicianId { get; set; } 
        public string? TechnicianName { get; set; }
        public string? TechnicianSpecialization { get; set; }
        public decimal? TechnicianRate { get; set; }
        public int? TechnicianExperienceYears { get; set; }

        // Services
        public List<BookingServiceDetailsDto> BookingServiceDetailsDtos { get; set; } = new();
    }
}
