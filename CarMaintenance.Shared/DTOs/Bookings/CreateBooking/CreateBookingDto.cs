using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Bookings.CreateBooking
{
    public class CreateBookingDto
    {


        [Required(ErrorMessage = "السيارة مطلوبة")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "تاريخ الحجز مطلوب")]
        public DateTime ScheduledDate { get; set; }

        [Required(ErrorMessage = "وصف المشكلة مطلوب")]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "يجب اختيار خدمة واحدة على الأقل")]
        [MinLength(1, ErrorMessage = "يجب اختيار خدمة واحدة على الأقل")]
        public List<BookingServiceDto> Services { get; set; } = new();

        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        public string PaymentMethod { get; set; } = null!; // "Cash" or "CreditCard"


    }
}
