using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Bookings.Additionallssues
{
    public class ApproveAdditionalIssueDto
    {
        [JsonIgnore]
        public int IssueId { get; set; }

        [Required]
        public bool IsApproved { get; set; }
    }
}