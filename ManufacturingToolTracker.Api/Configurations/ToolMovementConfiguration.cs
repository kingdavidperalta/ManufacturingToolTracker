using ManufacturingToolTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManufacturingToolTracker.Api.Configurations;
public class ToolMovementConfiguration: IEntityTypeConfiguration<ToolMovement>
{
    public void Configure(EntityTypeBuilder<ToolMovement> builder)
    {
        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Action)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(movement => movement.PerformedBy)
            .HasMaxLength(100);

        builder.Property(movement => movement.Remarks)
            .HasMaxLength(500);

        builder.HasOne(movement => movement.Tool)
            .WithMany()
            .HasForeignKey(movement => movement.ToolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
