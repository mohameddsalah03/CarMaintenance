namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class AdditionalIssueDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal EstimatedCost { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public string Status { get; set; } = null!;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}