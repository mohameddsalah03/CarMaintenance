namespace CarMaintenance.Shared.DTOs.Payment
{
    public class InitiatePaymentDto
    {
        public int BookingId { get; set; }
        public string PaymentMethod { get; set; } = "card";
    }
}