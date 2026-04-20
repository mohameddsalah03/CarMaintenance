using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class RefreshTokenDto
    {
        [Required]
        public required string Token { get; set; } 

        [Required]
        public required string RefreshToken { get; set; }
    }
}