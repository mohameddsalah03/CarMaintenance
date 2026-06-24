using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Payment
{
    public class InitiatePaymentDto
    {
        public int BookingId { get; set; }

        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        public string PaymentMethod { get; set; } = "Cash"; 
    }
}