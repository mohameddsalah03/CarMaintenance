
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Bookings.Additionallssues
{
    public class AddAdditionalIssueDto
    {
        [JsonIgnore]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "عنوان المشكلة مطلوب")]
        [MaxLength(300)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; } 

        [Required(ErrorMessage = "التكلفة المتوقعة مطلوبة")]
        [Range(0.01, 100000)]
        public decimal EstimatedCost { get; set; }
    }
}