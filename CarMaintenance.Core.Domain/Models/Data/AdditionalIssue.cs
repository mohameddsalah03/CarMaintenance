using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class AdditionalIssue : BaseEntity<int>
    {
        public string Title { get; set; } = null!;
        public decimal EstimatedCost { get; set; }
        public bool IsApproved { get; set; }

        // Foreign Key
        public int BookingId { get; set; }

        // Navigation Property
        public Booking Booking { get; set; } = null!;
    }
}