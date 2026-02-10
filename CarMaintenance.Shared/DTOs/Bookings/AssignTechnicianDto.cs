using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Bookings
{
    public class AssignTechnicianDto
    {
        [JsonIgnore]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "يجب اختيار فني")]
        public string TechnicianId { get; set; } = null!;
    }
}
