using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Common;
using CarMaintenance.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    [DbContextType(typeof(CarIdentityDbContext))]

    public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
    {
        public void Configure(EntityTypeBuilder<Technician> builder)
        {
            // Table & Schema
            builder.ToTable("Technicians");

            // Primary Key (Id = UserId)
            builder.HasKey(t => t.Id);

            // Properties
            builder.Property(t => t.Id)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(t => t.Specialization)
                .HasMaxLength(200)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(t => t.Rating)
                .HasColumnType("decimal(3,2)")
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(t => t.IsAvailable)
                .HasDefaultValue(true)
                .IsRequired();

            // Indexes
            builder.HasIndex(t => t.Id)
                .IsUnique();

            // Relationships
            // Technician -> Booking (One-to-Many)
            builder.HasMany(t => t.AssignedBookings)
                .WithOne(b => b.AssignedTechnician)
                .HasForeignKey(b => b.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            // Technician -> Review (One-to-Many)
            builder.HasMany(t => t.ReviewsReceived)
                .WithOne(r => r.Technician)
                .HasForeignKey(r => r.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}