using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Display name is required")]
        [MaxLength(200, ErrorMessage = "Display name cannot exceed 200 characters")]
        [MinLength(3, ErrorMessage = "Display name must be at least 3 characters")]
        public required string DisplayName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(256, ErrorMessage = "Email address is too long")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(010|011|012|015)\d{8}$",
            ErrorMessage = "Phone number must be 11 digits and start with 010, 011, 012, or 015")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@#$%&*()_+\-={}\[\]|;:,.<>?/~!^])[A-Za-z\d@#$%&*()_+\-={}\[\]|;:,.<>?/~!^]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, one special character, and be at least 8 characters long.")]
        public required string Password { get; set; }
    }
}