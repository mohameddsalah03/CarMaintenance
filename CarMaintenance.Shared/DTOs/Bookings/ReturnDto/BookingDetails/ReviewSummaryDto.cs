namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class ReviewSummaryDto
    {
        public int Id { get; set; }
        public int ServiceRating { get; set; }
        public int TechnicianRating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
