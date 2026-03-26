using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Bookings.CreateBooking
{
    public class BookingServiceDto
    {
        [Required]
        public int ServiceId { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "المدة يجب أن تكون بين 1 و 1440 دقيقة")]
        public int Duration { get; set; }

    }
}
