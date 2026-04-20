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
    public bool IsApproved => Status == AdditionalIssueStatus.Approved;

    public DateTime CreatedAt { get; set; }

    // Foreign Key
    public int BookingId { get; set; }

    // Navigation Property
    public Booking Booking { get; set; } = null!;
}