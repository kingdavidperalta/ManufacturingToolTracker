using ManufacturingToolTracker.Api.Data;
using ManufacturingToolTracker.Api.DTOs.ToolMovements;
using ManufacturingToolTracker.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingToolTracker.Api.Controllers
{
    [Route("api/Tools/{toolId:int}/Movements")]
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
            var allowedActions = new[]
          {
            ToolMovementActions.Checkout,
            ToolMovementActions.Return,
            ToolMovementActions.Repair,
            ToolMovementActions.Damage
        };

            var action = allowedActions.FirstOrDefault(
                allowedAction => string.Equals(
                    allowedAction,
                    dto.Action,
                    StringComparison.OrdinalIgnoreCase));

            if (action is null)
            {
                return BadRequest(new
                {
                    message = "Invalid action. Allowed actions are Checkout, Return, Repair, and Damage."
                });
            }

            var tool = await _context.Tools
                .FirstOrDefaultAsync(tool => tool.Id == toolId);

            if (tool is null)
            {
                return NotFound(new
                {
                    message = "Tool not found."
                });
            }

            var decreasesQuantity =
                action == ToolMovementActions.Checkout ||
                action == ToolMovementActions.Repair ||
                action == ToolMovementActions.Damage;

            if (decreasesQuantity && tool.Quantity < dto.Quantity)
            {
                return BadRequest(new
                {
                    message = "Insufficient available quantity."
                });
            }

            if (decreasesQuantity)
            {
                tool.Quantity -= dto.Quantity;
            }
            else if (action == ToolMovementActions.Return)
            {
                if (tool.Quantity > int.MaxValue - dto.Quantity)
                {
                    return BadRequest(new
                    {
                        message = "The resulting quantity exceeds the supported limit."
                    });
                }

                tool.Quantity += dto.Quantity;
            }

            var movement = new ToolMovement
            {
                ToolId = toolId,
                Action = action,
                Quantity = dto.Quantity,
                PerformedBy = dto.PerformedBy,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.ToolMovements.Add(movement);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message = "This tool was updated by another user. Retrieve the latest quantity and try again."
                });
            }

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
