using CarMaintenance.Core.Domain.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    

    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            // Table 
            builder.ToTable("Reviews");

            // Properties
            builder.Property(E => E.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(r => r.TechnicianRating)
                .IsRequired();
            builder.Property(r=>r.ServiceRating)
                .IsRequired();

            builder.Property(r => r.Comment)
                .HasMaxLength(1000)
                .HasColumnType("nvarchar")
                .IsRequired(false); 

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(r => r.TechnicianId)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(r => r.BookingId)
                .IsRequired();

            // Indexes
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.TechnicianId);
            builder.HasIndex(r => r.BookingId).IsUnique(); // One-to-One with Booking

           
            // Relationships
            builder.HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(r => r.Technician)
                .WithMany(t => t.ReviewsReceived)
                .HasForeignKey(r => r.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(r => r.User)
            .WithMany(u => u.ReviewsWritten)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}