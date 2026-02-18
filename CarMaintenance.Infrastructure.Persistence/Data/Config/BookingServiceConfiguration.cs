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

            // Primary Key على Id
            builder.HasKey(bs => bs.Id);

            // Unique Index على الـ Composite
            builder.HasIndex(bs => new { bs.BookingId, bs.ServiceId })
                   .IsUnique();
            // Properties
            builder.Property(bs => bs.Duration)
                .IsRequired();

            // الطريقة الأفضل (أكثر وضوحاً)
            builder.Property(b => b.Status)
                .HasColumnType("nvarchar(50)")
                .HasConversion(
                    status => status.ToString(),
                    status => Enum.Parse<BookingStatus>(status, true))
                .IsRequired();

           
            // Relationships
            // BookingService -> Booking (Many-to-One)
            builder.HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            // BookingService -> Service (Many-to-One)
            builder.HasOne(bs => bs.Service)
                .WithMany(s => s.BookingServices)
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();


        }
    }
}