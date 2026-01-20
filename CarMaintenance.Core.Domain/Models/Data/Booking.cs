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
    public string UserId { get; set; } = null!; // Mandatory
    public int VehicleId { get; set; } // Mandatory
    public string? TechnicianId { get; set; } // Optional (nullable)

    // Navigation Properties
    public Vehicle Vehicle { get; set; } = null!; // Mandatory
    public Technician? AssignedTechnician { get; set; } // Optional
    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    public ICollection<AdditionalIssue> AdditionalIssues { get; set; } = new List<AdditionalIssue>();
    public Review? Review { get; set; } // Optional (One-to-One)
}