using System.ComponentModel.DataAnnotations;

namespace ManufacturingToolTracker.Api.Models
{
    public class ToolMovement
    {
        public int Id { get; set; }

        public int ToolId { get; set; }

        public Tool Tool { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string Action { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [StringLength(100)]
        public string? PerformedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
