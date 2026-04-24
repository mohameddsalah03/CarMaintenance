using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

public class AdditionalIssue : BaseEntity<int>
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal EstimatedCost { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public AdditionalIssueStatus Status { get; set; } = AdditionalIssueStatus.Pending;

    public bool? IsApproved
    {
        get
        {
            if (Status == AdditionalIssueStatus.Pending) return null;  
            return Status == AdditionalIssueStatus.Approved;          
        }
    }

    public DateTime CreatedAt { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
}