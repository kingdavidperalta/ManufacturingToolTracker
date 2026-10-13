using System.ComponentModel.DataAnnotations;

namespace ManufacturingToolTracker.Api.DTOs.ToolMovements;
public class CreateToolMovementDto
{
    [Required]
    [StringLength(30)]
    public string Action { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(100)]
    public string? PerformedBy { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}
