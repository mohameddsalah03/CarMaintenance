using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
   

    public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
    {
        public void Configure(EntityTypeBuilder<BookingService> builder)
        {
            // Table 
            builder.ToTable("BookingServices");

            builder.HasKey(bs => bs.Id);

            builder.HasIndex(bs => new { bs.BookingId, bs.ServiceId })
                   .IsUnique();
            // Properties
            builder.Property(bs => bs.Duration)
                .IsRequired();

            builder.Property(b => b.Status)
                .HasColumnType("nvarchar(50)")
                .HasConversion(
                    status => status.ToString(),
                    status => Enum.Parse<BookingStatus>(status, true))
                .IsRequired();

           
            // Relationships
            builder.HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(bs => bs.Service)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();


        }
    }
}