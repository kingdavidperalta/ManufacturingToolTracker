
using ManufacturingToolTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ManufacturingToolTracker.Api.Configurations;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.HasIndex(tool => tool.ToolCode)
            .IsUnique();
    }
}
