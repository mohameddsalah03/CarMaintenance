namespace CarMaintenance.Shared.Settings
{
    public class JwtSettings
    {
        public required string Key { get; set; }
        public required string Audience { get; set; }
        public required string Issuer { get; set; }
        public required double DurationInMinutes { get; set; } 
        public required int RefreshTokenShortDurationInDays { get; set; }   // rememberMe = False => 1
        public required int RefreshTokenDurationInDays { get; set; }         // RememberMe = True => 30
    }
}