using ManufacturingToolTracker.Api.Data;
using ManufacturingToolTracker.Api.DTOs.ToolMovements;
using ManufacturingToolTracker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingToolTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToolMovementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ToolMovementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ToolMovementResponseDto>>> GetMovements(int toolId)
        {
            var toolExists = await _context.Tools
                .AnyAsync(tool => tool.Id == toolId);

            if (!toolExists)
            {
                return NotFound(new
                {
                    message = "Tool not found."
                });
            }

            var movements = await _context.ToolMovements
                .AsNoTracking()
                .Where(movement => movement.ToolId == toolId)
                .OrderByDescending(movement => movement.CreatedAt)
                .Select(movement => new ToolMovementResponseDto
                {
                    Id = movement.Id,
                    ToolId = movement.ToolId,
                    Action = movement.Action,
                    Quantity = movement.Quantity,
                    PerformedBy = movement.PerformedBy,
                    Remarks = movement.Remarks,
                    CreatedAt = movement.CreatedAt
                })
                .ToListAsync();

            return Ok(movements);
        }

        [HttpPost]
        public async Task<ActionResult<ToolMovementResponseDto>>CreateMovement(
            int toolId, 
            CreateToolMovementDto dto)
        {
            var tool = await _context.Tools
                .FirstOrDefaultAsync(tool => tool.Id == toolId);

            if (tool is null)
            {
                return NotFound(new
                {
                    message = "Tool not found."
                });
            }

            var movement = new ToolMovement
            {
                ToolId = toolId,
                Action = dto.Action,
                Quantity = dto.Quantity,
                PerformedBy = dto.PerformedBy,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.ToolMovements.Add(movement);
            await _context.SaveChangesAsync();

            var response = new ToolMovementResponseDto
            {
                Id = movement.Id,
                ToolId = movement.ToolId,
                Action = movement.Action,
                Quantity = movement.Quantity,
                PerformedBy = movement.PerformedBy,
                Remarks = movement.Remarks,
                CreatedAt = movement.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetMovements),
                new { toolId },
                response);
        }
    }
}
