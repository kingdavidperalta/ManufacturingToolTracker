using System.ComponentModel.DataAnnotations;

namespace ManufacturingToolTracker.Api.DTOs.Tools
{
    public class CreateToolDto
    {
        [Required]
        [StringLength(30)]
        public string ToolCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
