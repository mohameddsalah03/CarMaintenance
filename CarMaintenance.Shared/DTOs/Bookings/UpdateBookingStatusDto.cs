using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Bookings
{
    public class UpdateBookingStatusDto
    {
        [JsonIgnore]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "الحالة مطلوبة")]
        public string Status { get; set; } = null!; // "InProgress" or "Completed"

        //public string? Notes { get; set; }

    }
}
