using Microsoft.AspNetCore.Identity;

namespace CarMaintenance.Core.Domain.Models.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = null!;

        // Refresh Token properties
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> ReviewsWritten { get; set; } = new List<Review>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}