
namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails
{
    public class BookingDetailsDto : BookingDto
    {
        //
        public List<AdditionalIssueDto> AdditionalIssueDtos { get; set; } = new List<AdditionalIssueDto>();

        public TechnicianAvailableSlotsDto? TechnicianAvailableSlots { get; set; }

        public ReviewSummaryDto? Review { get; set; }
    }
}
