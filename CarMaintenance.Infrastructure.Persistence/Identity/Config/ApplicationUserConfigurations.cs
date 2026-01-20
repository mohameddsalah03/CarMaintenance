using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Identity.Config
{
    [DbContextType(typeof(CarIdentityDbContext))]
    public class ApplicationUserConfigurations : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.DisplayName)
                .HasColumnType("nvarchar")
                .HasMaxLength(100)
                .IsRequired(true);

           
        }
    }
}
