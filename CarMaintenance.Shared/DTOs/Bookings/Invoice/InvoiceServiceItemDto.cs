namespace CarMaintenance.Shared.DTOs.Bookings.Invoice
{
    public class InvoiceServiceItemDto
    {
        public string ServiceName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Price { get; set; }
        public int Duration { get; set; }
    }
}