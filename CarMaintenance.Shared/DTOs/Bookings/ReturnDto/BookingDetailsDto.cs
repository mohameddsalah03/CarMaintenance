namespace CarMaintenance.Shared.DTOs.Bookings.ReturnDto
{
    public class BookingDetailsDto : BookingDto
    {
        //
        public List<AdditionalIssueDto> AdditionalIssueDtos { get; set; } = new List<AdditionalIssueDto>();

       

    }
}
