using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class GoogleLoginDto
    {
        [Required]
        public required string IdToken { get; set; }
    }
}
