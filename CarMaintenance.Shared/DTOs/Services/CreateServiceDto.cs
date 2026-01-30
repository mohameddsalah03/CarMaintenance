using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Services
{
    public class CreateServiceDto
    {
        [Required(ErrorMessage = "اسم الخدمة مطلوب")]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "الفئة مطلوبة")]
        [MaxLength(100)]
        public string Category { get; set; } = null!;

        [Required]
        [Range(0.01, 100000, ErrorMessage = "السعر يجب أن يكون بين 0.01 و 100000")]
        public decimal BasePrice { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "المدة يجب أن تكون بين 1 و 1440 دقيقة")]
        public int EstimatedDurationMinutes { get; set; }
    }
}