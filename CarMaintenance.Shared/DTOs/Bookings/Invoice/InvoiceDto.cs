namespace CarMaintenance.Shared.DTOs.Bookings.Invoice
{
    public class InvoiceDto
    {
        // Book
        public string BookingNumber { get; set; } = null!;
        public DateTime ScheduledDate { get; set; }
        public string BookingStatus { get; set; } = null!;

        // Customer
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;

        // Vehicle
        public string VehicleBrand { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public int VehicleYear { get; set; }
        public string VehiclePlateNumber { get; set; } = null!;

        // Technician
        public string TechnicianName { get; set; } = null!;
        public string TechnicianSpecialization { get; set; } = null!;

        // Services
        public List<InvoiceServiceItemDto> Services { get; set; } = new();

        // Additional Issues 
        public List<InvoiceAdditionalIssueDto> ApprovedIssues { get; set; } = new();

        // Totals
        public decimal ServicesCost { get; set; }
        public decimal AdditionalCost { get; set; }
        public decimal TotalCost { get; set; }

        // Payment
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
    }
}