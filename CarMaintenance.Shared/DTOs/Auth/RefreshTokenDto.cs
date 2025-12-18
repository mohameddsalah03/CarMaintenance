using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required]
        public required string Token { get; set; } // Access Token القديم

        [Required]
        public required string RefreshToken { get; set; }
    }
}