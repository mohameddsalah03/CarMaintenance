using CarMaintenance.Core.Domain.Models.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarMaintenance.Infrastructure.Persistence.Data.Config
{
    public class AdditionalIssueConfiguration : IEntityTypeConfiguration<AdditionalIssue>
    {
        public void Configure(EntityTypeBuilder<AdditionalIssue> builder)
        {
            builder.ToTable("AdditionalIssues");

            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(ai => ai.Title)
                .HasMaxLength(300)
                .HasColumnType("nvarchar(300)")
                .IsRequired();
            builder.Property(ai => ai.Description)
                    .HasMaxLength(1000)
                    .HasColumnType("nvarchar(1000)")
                    .IsRequired(false);


            builder.Property(ai => ai.EstimatedCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(ai => ai.EstimatedDurationMinutes)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(ai => ai.CreatedAt).IsRequired();

            // Store Status as string, default = Pending
            builder.Property(ai => ai.Status)
                .HasColumnType("nvarchar(20)")
                .HasConversion(
                    s => s.ToString(),
                    s => Enum.Parse<AdditionalIssueStatus>(s, true))
                .HasDefaultValue(AdditionalIssueStatus.Pending)
                .IsRequired();

            // Ignore computed property — not mapped to DB column
            builder.Ignore(ai => ai.IsApproved);

            builder.Property(ai => ai.BookingId).IsRequired();

            builder.HasIndex(ai => ai.BookingId);

            builder.HasOne(ai => ai.Booking)
                .WithMany(b => b.AdditionalIssues)
                .HasForeignKey(ai => ai.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }
    }
}