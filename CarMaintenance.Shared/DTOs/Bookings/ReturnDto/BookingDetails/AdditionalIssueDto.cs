namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class AdditionalIssueDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal EstimatedCost { get; set; }
        //public string? Description { get; set; } 

        public bool IsApproved { get; set; }

    }
}
