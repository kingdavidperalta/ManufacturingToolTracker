using ManufacturingToolTracker.Api.Data;
using ManufacturingToolTracker.Api.DTOs.Tools;
using ManufacturingToolTracker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingToolTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ToolsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToolResponseDto>>> GetTools()
        {
            var tools = await _context.Tools
                .AsNoTracking()
                .Select(tool => new ToolResponseDto
                {
                    Id = tool.Id,
                    ToolCode = tool.ToolCode,
                    Name = tool.Name,
                    Description = tool.Description,
                    Quantity = tool.Quantity,
                    Status = tool.Status
                })
                .ToListAsync();

            return Ok(tools);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ToolResponseDto>> GetTool(int id)
        {
            var tool = await _context.Tools
                .AsNoTracking()
                .Where(tool => tool.Id == id)
                .Select(tool => new ToolResponseDto
                {
                    Id = tool.Id,
                    ToolCode = tool.ToolCode,
                    Name = tool.Name,
                    Description = tool.Description,
                    Quantity = tool.Quantity,
                    Status = tool.Status
                })
                .FirstOrDefaultAsync();

            if (tool is null)
            {
                return NotFound();
            }

            return Ok(tool);
        }

        [HttpPost]
        public async Task<ActionResult<ToolResponseDto>> CreateTool(
            CreateToolDto dto)
        {
            var tool = new Tool
            {
                ToolCode = dto.ToolCode,
                Name = dto.Name,
                Description = dto.Description,
                Quantity = dto.Quantity,
                Status = "Available"
            };

            _context.Tools.Add(tool);
            await _context.SaveChangesAsync();

            var response = new ToolResponseDto
            {
                Id = tool.Id,
                ToolCode = tool.ToolCode,
                Name = tool.Name,
                Description = tool.Description,
                Quantity = tool.Quantity,
                Status = tool.Status
            };

            return CreatedAtAction(
                nameof(GetTool),
                new { id = tool.Id },
                response);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTool(
            int id,
            UpdateToolDto dto)
        {
            var tool = await _context.Tools.FindAsync(id);

            if (tool is null)
            {
                return NotFound();
            }

            tool.ToolCode = dto.ToolCode;
            tool.Name = dto.Name;
            tool.Description = dto.Description;
            tool.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTool(int id)
        {
            var tool = await _context.Tools.FindAsync(id);

            if (tool is null)
            {
                return NotFound();
            }

            _context.Tools.Remove(tool);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
