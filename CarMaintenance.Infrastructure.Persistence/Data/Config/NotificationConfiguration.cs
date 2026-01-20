using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Common;
using CarMaintenance.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    [DbContextType(typeof(CarIdentityDbContext))]

    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Table 
            builder.ToTable("Notifications");

            // Properties
            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(n => n.Message)
                .HasMaxLength(1000)
                .HasColumnType("nvarchar")
                .IsRequired(false);

            builder.Property(n => n.IsRead)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(n => n.CreatedAt)
                .IsRequired();

            builder.Property(n => n.UserId)
                .HasMaxLength(500)
                .IsRequired();

            // Indexes
            builder.HasIndex(n => n.UserId);
            builder.HasIndex(n => n.CreatedAt);
        }
    }
}