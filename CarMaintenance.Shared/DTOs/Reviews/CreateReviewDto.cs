using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Reviews
{
    public class CreateReviewDto
    {
        [JsonIgnore]
        public int BookingId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5")]
        public int ServiceRating { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "التقييم يجب أن يكون بين 1 و 5")]
        public int TechnicianRating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
