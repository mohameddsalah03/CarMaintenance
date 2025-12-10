using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
