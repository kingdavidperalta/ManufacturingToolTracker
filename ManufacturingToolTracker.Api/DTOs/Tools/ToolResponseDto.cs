namespace ManufacturingToolTracker.Api.DTOs.Tools
{
    public class ToolResponseDto
    {
        public int Id { get; set; }

        public string ToolCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Quantity { get; set; }

        public string Status { get; set; } = string.Empty;
        public byte[] Version { get; set; } = [];
    }
}
