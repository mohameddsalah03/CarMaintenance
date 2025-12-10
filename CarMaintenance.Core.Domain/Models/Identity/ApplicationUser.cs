using Microsoft.AspNetCore.Identity;

namespace CarMaintenance.Core.Domain.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = null!;
    }
}
