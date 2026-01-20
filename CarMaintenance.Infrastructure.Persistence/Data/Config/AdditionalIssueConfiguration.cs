using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Common;
using CarMaintenance.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    [DbContextType(typeof(CarIdentityDbContext))]
    public class AdditionalIssueConfiguration : IEntityTypeConfiguration<AdditionalIssue>
    {
        public void Configure(EntityTypeBuilder<AdditionalIssue> builder)
        {
            // Table 
            builder.ToTable("AdditionalIssues");

            // Properties
            builder.Property(ai => ai.Title)
                .HasMaxLength(300)
                .HasColumnType("nvarchar")
                .IsRequired();

            builder.Property(ai => ai.EstimatedCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ai => ai.IsApproved)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(ai => ai.BookingId)
                .IsRequired();

            // Indexes
            builder.HasIndex(ai => ai.BookingId);

            // Relationships
            // AdditionalIssue -> Booking (Many-to-One)
            builder.HasOne(ai => ai.Booking)
                .WithMany(b => b.AdditionalIssues)
                .HasForeignKey(ai => ai.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}