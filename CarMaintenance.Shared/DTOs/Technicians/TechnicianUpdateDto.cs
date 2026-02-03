using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Technicians
{
    public class TechnicianUpdateDto
    {
        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [RegularExpression(@"^[0-9+\-\(\)\s]{10,20}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Specialization { get; set; }

        public bool? IsAvailable { get; set; }
    }
}