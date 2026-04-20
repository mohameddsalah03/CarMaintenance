using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(128, ErrorMessage = "Password is too long")]
        public required string Password { get; set; }

        public bool RememberMe { get; set; } = false;
    }
}