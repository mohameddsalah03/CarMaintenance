using CarMaintenance.Core.Domain.Models.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    

    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            // Table 
            builder.ToTable("Vehicles");

            // Properties
            builder.Property(v => v.Model)
                .HasMaxLength(100)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(v => v.Brand)
                .HasMaxLength(50)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(v => v.Year)
                .IsRequired();

            builder.Property(v => v.PlateNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(v => v.UserId)
                .HasMaxLength(450)
                .IsRequired();

            // Indexes
            builder.HasIndex(v => v.PlateNumber)
                .IsUnique();

            builder.HasIndex(v => v.UserId);

            // Relationships
            // Vehicle -> Booking (One-to-Many)
            builder.HasMany(v => v.Bookings)
                .WithOne(b => b.Vehicle)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(v => v.Owner)
            .WithMany(u => u.Vehicles)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}