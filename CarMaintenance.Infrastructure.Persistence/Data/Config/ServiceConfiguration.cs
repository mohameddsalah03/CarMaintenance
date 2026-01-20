using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Common;
using CarMaintenance.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    [DbContextType(typeof(CarIdentityDbContext))]

    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            // Table & Schema
            builder.ToTable("Services");

            // Properties
            builder.Property(s => s.Name)
                .HasMaxLength(200)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(s => s.Category)
                .HasMaxLength(100)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(s => s.BasePrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(s => s.EstimatedDurationMinutes)
                .IsRequired();

            // Relationships
            // Service -> BookingService (One-to-Many)
            builder.HasMany(s => s.BookingServices)
                .WithOne(bs => bs.Service)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}