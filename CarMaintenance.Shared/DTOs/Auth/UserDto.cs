using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Auth
{
    public class UserDto
    {
        [Required]
        public required string Id { get; set; }

        [Required]
        public required string DisplayName { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Token { get; set; }


        //  إضافة Refresh Token
        [Required]
        public required string RefreshToken { get; set; }

        //  إضافة Expiry (اختياري - للفرونت يعرف امتى ينتهي)
        public DateTime TokenExpiry { get; set; }

        public IEnumerable<string> Roles { get; set; } = new List<string>();


    }
}
