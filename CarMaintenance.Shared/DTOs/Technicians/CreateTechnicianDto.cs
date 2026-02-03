using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Technicians
{
    public class CreateTechnicianDto
    {
        [Required]
        public required string DisplayName { get; set; }

        [Required]
        public required string UserName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }

        [Required]
        [RegularExpression("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[@#$%&*()_+\\-={}\\[\\]|;:,.<>?/~])[A-Za-z\\d@#$%&*()_+\\-={}\\[\\]|;:,.<>?/~]{6,}$",
            ErrorMessage = "Password must have 1 uppercase, 1 lowercase, 1 number, 1 non-alphanumeric and at least 6 characters")]
        public required string Password { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Specialization { get; set; }
    }
}