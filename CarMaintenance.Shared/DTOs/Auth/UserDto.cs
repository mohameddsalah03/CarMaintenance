namespace CarMaintenance.Shared.DTOs.Auth
{
    public class UserDto
    {
        public required string Id { get; set; }
        public required string DisplayName { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime TokenExpiry { get; set; }

        public bool RememberMe { get; set; }

        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}