
namespace ManufacturingToolTracker.Api.Models;
public class Tool
{
    public int Id { get; set; }

    public string ToolCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = "Available";
}
