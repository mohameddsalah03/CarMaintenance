namespace CarMaintenance.Core.Domain.Models.Data.Enums
{
    public enum AdditionalIssueStatus
    {
        Pending,   // Awaiting client response
        Approved,  // Client approved — cost added to booking
        Rejected   // Client rejected — booking returns to InProgress
    }
}
