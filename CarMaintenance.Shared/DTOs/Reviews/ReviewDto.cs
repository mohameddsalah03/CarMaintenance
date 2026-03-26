namespace CarMaintenance.Shared.DTOs.Reviews
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int ServiceRating { get; set; }
        public int TechnicianRating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Booking Info
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = null!;

        // Customer Info
        public string CustomerName { get; set; } = null!;

        // Technician Info
        public string TechnicianName { get; set; } = null!;

    }
}
