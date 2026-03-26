using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Core.Domain.Models.Data.Enums;


namespace CarMaintenance.Core.Domain.Models.Data;
public class Booking : BaseEntity<int>
{
    public string BookingNumber { get; set; } = null!;
    public DateTime ScheduledDate { get; set; }
    public BookingStatus Status { get; set; }
    public string Description { get; set; } = null!;
    public decimal TotalCost { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = null!;
    public int VehicleId { get; set; } 
    public string? TechnicianId { get; set; } 

    // Navigation Properties
    public ApplicationUser User { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Technician? AssignedTechnician { get; set; }
    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    public ICollection<AdditionalIssue> AdditionalIssues { get; set; } = new List<AdditionalIssue>();
    public Review? Review { get; set; }
}