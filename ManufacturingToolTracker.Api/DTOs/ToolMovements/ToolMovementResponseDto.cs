namespace ManufacturingToolTracker.Api.DTOs.ToolMovements;
public class ToolMovementResponseDto
{
    public int Id { get; set; }

    public int ToolId { get; set; }

    public string Action { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? PerformedBy { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
}
