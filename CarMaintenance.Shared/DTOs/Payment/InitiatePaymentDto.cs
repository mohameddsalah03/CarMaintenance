namespace CarMaintenance.Shared.DTOs.Payment
{
    public class InitiatePaymentDto
    {
        // id الحجز اللي عايز تدفعه
        public int BookingId { get; set; }

        // "card" أو "vodafone_cash"
        public string PaymentMethod { get; set; } = "card";
    }

}
