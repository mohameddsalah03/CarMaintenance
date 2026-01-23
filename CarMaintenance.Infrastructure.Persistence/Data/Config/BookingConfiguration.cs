using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Contexts.Config
{
       
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");


            builder.Property(E => E.Id)
                   .ValueGeneratedOnAdd();

            builder.Property(b => b.BookingNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(b => b.ScheduledDate)
                .IsRequired();

            builder.Property(b => b.Description)
                .HasMaxLength(1000)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(b => b.TotalCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // To Store String Not Int in DB
            builder.Property(b => b.Status)
                .HasColumnType("nvarchar(50)")
                .HasConversion(
                    status => status.ToString(),
                    status => Enum.Parse<BookingStatus>(status, true))
                .IsRequired();

            builder.Property(b => b.PaymentStatus)
                .HasColumnType("nvarchar(50)")
                .HasConversion(
                    status => status.ToString(),
                    status => Enum.Parse<PaymentStatus>(status, true))
                .IsRequired();

            builder.Property(b => b.PaymentMethod)
                .HasColumnType("nvarchar(50)")
                .HasConversion(
                    method => method.ToString(),
                    method => Enum.Parse<PaymentMethod>(method, true))
                .IsRequired();

            builder.Property(b => b.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(b => b.VehicleId)
                .IsRequired();

            builder.Property(b => b.TechnicianId)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.HasIndex(b => b.BookingNumber)
                .IsUnique();

            builder.HasIndex(b => b.UserId);
            builder.HasIndex(b => b.VehicleId);
            builder.HasIndex(b => b.TechnicianId);

            builder.HasOne(b => b.Vehicle)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(b => b.AssignedTechnician)
                .WithMany(t => t.AssignedBookings)
                .HasForeignKey(b => b.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.HasMany(b => b.BookingServices)
                .WithOne(bs => bs.Booking)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasMany(b => b.AdditionalIssues)
                .WithOne(ai => ai.Booking)
                .HasForeignKey(ai => ai.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(b => b.Review)
                .WithOne(r => r.Booking)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}