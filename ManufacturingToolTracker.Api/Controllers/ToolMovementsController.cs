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
                ToolMovementActions.Damage,
                ToolMovementActions.Repair
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
                    message = "Invalid action. Allowed actions are Checkout, Return, Damage, and Repair."
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

            // Validate that a return does not exceed
            // the quantity checked out and not yet returned.
            if (action == ToolMovementActions.Return)
            {
                var totalCheckedOut = await _context.ToolMovements
                    .Where(movement =>
                        movement.ToolId == toolId &&
                        movement.Action == ToolMovementActions.Checkout)
                    .SumAsync(movement => (long?)movement.Quantity) ?? 0L;

                var totalReturned = await _context.ToolMovements
                    .Where(movement =>
                        movement.ToolId == toolId &&
                        movement.Action == ToolMovementActions.Return)
                    .SumAsync(movement => (long?)movement.Quantity) ?? 0L;

                var notYetReturnedQuantity =
                    totalCheckedOut - totalReturned;

                if (dto.Quantity > notYetReturnedQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Return quantity cannot exceed the quantity not yet returned."
                    });
                }
            }

            // Validate that a repair does not exceed
            // the quantity damaged and not yet repaired.
            if (action == ToolMovementActions.Repair)
            {
                var totalDamaged = await _context.ToolMovements
                    .Where(movement =>
                        movement.ToolId == toolId &&
                        movement.Action == ToolMovementActions.Damage)
                    .SumAsync(movement => (long?)movement.Quantity) ?? 0L;
                
                var totalRepaired = await _context.ToolMovements
                    .Where(movement =>
                        movement.ToolId == toolId &&
                        movement.Action == ToolMovementActions.Repair)
                    .SumAsync(movement => (long?)movement.Quantity) ?? 0L;

                var notYetRepairedQuantity =
                    totalDamaged - totalRepaired;

                if (dto.Quantity > notYetRepairedQuantity)
                {
                    return BadRequest(new
                    {
                        message = "Repair quantity cannot exceed the quantity damaged and not yet repaired."
                    });
                }
            }

            var decreasesQuantity =
                action == ToolMovementActions.Checkout ||
                action == ToolMovementActions.Damage;

            var increasesQuantity =
                action == ToolMovementActions.Return ||
                action == ToolMovementActions.Repair;

            if (decreasesQuantity && tool.Quantity < dto.Quantity)
            {
                return BadRequest(new
                {
                    message = "Insufficient available quantity."
                });
            }

            if (increasesQuantity &&
                tool.Quantity > int.MaxValue - dto.Quantity)
            {
                return BadRequest(new
                {
                    message = "The resulting quantity exceeds the supported limit."
                });
            }

            if (decreasesQuantity)
            {
                tool.Quantity -= dto.Quantity;
            }
            else if (increasesQuantity)
            {
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
