using ManufacturingToolTracker.Api.Controllers;
using ManufacturingToolTracker.Api.Data;
using ManufacturingToolTracker.Api.DTOs.ToolMovements;
using ManufacturingToolTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ManufacturingToolTracker.Tests
{
    public class ToolMovementTests
    {
        private static readonly object MigrationLock = new();
        private static volatile bool _databaseMigrated;

        [Fact]
        public async Task TestDatabase_CanConnect_Succeeds()
        {
            await using var context = CreateDbContext();

            await context.Database.MigrateAsync();

            var canConnect = await context.Database.CanConnectAsync();

            Assert.True(
                canConnect,
                "Cannot connect to ManufacturingToolTrackerTestDb.");
        }

        private static ApplicationDbContext CreateDbContext()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString("TestConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "TestConnection connection string is not configured.");
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            EnsureDatabaseMigrated(options);

            return new ApplicationDbContext(options);
        }

        private static void EnsureDatabaseMigrated(
            DbContextOptions<ApplicationDbContext> options)
        {
            if (_databaseMigrated)
            {
                return;
            }

            lock (MigrationLock)
            {
                if (_databaseMigrated)
                {
                    return;
                }

                using var migrationContext = new ApplicationDbContext(options);
                migrationContext.Database.Migrate();

                _databaseMigrated = true;
            }
        }

        [Fact]
        public async Task Checkout_ValidQuantity_ReducesStockAndRecordsMovement()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 10,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Checkout,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                };

                var result = await controller.CreateMovement(tool.Id, dto);

                Assert.IsType<CreatedAtActionResult>(result.Result);

                context.ChangeTracker.Clear();

                var updatedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                var movement = await context.ToolMovements
                    .AsNoTracking()
                    .SingleAsync(item => item.ToolId == tool.Id);

                Assert.Equal(7, updatedTool.Quantity);
                Assert.Equal(ToolMovementActions.Checkout, movement.Action);
                Assert.Equal(3, movement.Quantity);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools
                    .FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Checkout_InsufficientQuantity_RejectsRequestWithoutChangingStock()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Checkout,
                    Quantity = 6,
                    PerformedBy = "Automated Test"
                };

                var result = await controller.CreateMovement(tool.Id, dto);

                var badRequest = Assert.IsType<BadRequestObjectResult>(
                    result.Result);

                Assert.NotNull(badRequest.Value);

                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                var movementCount = await context.ToolMovements
                    .CountAsync(item => item.ToolId == tool.Id);

                Assert.Equal(5, savedTool.Quantity);
                Assert.Equal(0, movementCount);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools.FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Return_ExceedsNotYetReturnedQuantity_RejectsRequest()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 8,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                // Arrange: record a checkout of 3 units.
                context.ToolMovements.Add(new ToolMovement
                {
                    ToolId = tool.Id,
                    Action = ToolMovementActions.Checkout,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                });

                // Arrange: record a return of 1 unit.
                context.ToolMovements.Add(new ToolMovement
                {
                    ToolId = tool.Id,
                    Action = ToolMovementActions.Return,
                    Quantity = 1,
                    PerformedBy = "Automated Test"
                });

                await context.SaveChangesAsync();

                var controller = new ToolMovementsController(context);

                // Act: only 2 units remain eligible for return.
                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Return,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                };

                var result = await controller.CreateMovement(tool.Id, dto);

                // Assert: the return is rejected.
                Assert.IsType<BadRequestObjectResult>(result.Result);

                context.ChangeTracker.Clear();

                var movementCount = await context.ToolMovements
                    .CountAsync(movement => movement.ToolId == tool.Id);

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(2, movementCount);
                Assert.Equal(8, savedTool.Quantity);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools.FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }



      
        [Fact]
        public async Task Return_ValidQuantity_IncreasesStockAndRecordsMovement()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                // Arrange: check out 3 tools through the controller.
                var checkoutDto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Checkout,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                };

                var checkoutResult =
                    await controller.CreateMovement(tool.Id, checkoutDto);

                Assert.IsType<CreatedAtActionResult>(checkoutResult.Result);

                // Verify that only 2 tools remain available.
                context.ChangeTracker.Clear();

                var toolAfterCheckout = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(2, toolAfterCheckout.Quantity);

                // Act: return 2 tools through the controller.
                var returnDto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Return,
                    Quantity = 2,
                    PerformedBy = "Automated Test"
                };

                var returnResult =
                    await controller.CreateMovement(tool.Id, returnDto);

                // Assert: the return succeeds.
                Assert.IsType<CreatedAtActionResult>(returnResult.Result);

                context.ChangeTracker.Clear();

                var updatedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                var returnMovement = await context.ToolMovements
                    .AsNoTracking()
                    .SingleAsync(item =>
                        item.ToolId == tool.Id &&
                        item.Action == ToolMovementActions.Return);

                var totalCheckedOut = await context.ToolMovements
                    .Where(item =>
                        item.ToolId == tool.Id &&
                        item.Action == ToolMovementActions.Checkout)
                    .SumAsync(item => item.Quantity);

                var totalReturned = await context.ToolMovements
                    .Where(item =>
                        item.ToolId == tool.Id &&
                        item.Action == ToolMovementActions.Return)
                    .SumAsync(item => item.Quantity);

                Assert.Equal(4, updatedTool.Quantity);
                Assert.Equal(2, returnMovement.Quantity);
                Assert.Equal(ToolMovementActions.Return, returnMovement.Action);
                Assert.Equal(1, totalCheckedOut - totalReturned);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools.FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }



        [Fact]
        public async Task Damage_ExceedsAvailableQuantity_RejectsRequest()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Damage,
                    Quantity = 6,
                    PerformedBy = "Automated Test"
                };

                // Act
                var result = await controller.CreateMovement(tool.Id, dto);

                // Assert: reject damage exceeding available quantity.
                Assert.IsType<BadRequestObjectResult>(result.Result);

                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                var movementCount = await context.ToolMovements
                    .CountAsync(item => item.ToolId == tool.Id);

                Assert.Equal(5, savedTool.Quantity);
                Assert.Equal(0, movementCount);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools.FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Damage_ValidQuantity_ReducesStockAndRecordsMovement()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 10,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Damage,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                };

                // Act
                var result = await controller.CreateMovement(tool.Id, dto);

                // Assert
                Assert.IsType<CreatedAtActionResult>(result.Result);

                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                var movement = await context.ToolMovements
                    .AsNoTracking()
                    .SingleAsync(item => item.ToolId == tool.Id);

                Assert.Equal(7, savedTool.Quantity);
                Assert.Equal(ToolMovementActions.Damage, movement.Action);
                Assert.Equal(3, movement.Quantity);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools.FindAsync(tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Repair_ValidQuantity_IncreasesStockAndRecordsMovement()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                // Act 1: Damage 3 tools through the controller.
                var damageDto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Damage,
                    Quantity = 3,
                    PerformedBy = "Automated Test"
                };

                var damageResult =
                    await controller.CreateMovement(tool.Id, damageDto);

                Assert.IsType<CreatedAtActionResult>(damageResult.Result);

                // Verify available stock decreases from 5 to 2.
                context.ChangeTracker.Clear();

                var toolAfterDamage = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(2, toolAfterDamage.Quantity);

                // Act 2: Repair 2 of the damaged tools.
                var repairDto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Repair,
                    Quantity = 2,
                    PerformedBy = "Automated Test"
                };

                var repairResult =
                    await controller.CreateMovement(tool.Id, repairDto);

                Assert.IsType<CreatedAtActionResult>(repairResult.Result);

                // Verify available stock increases from 2 to 4.
                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(4, savedTool.Quantity);

                // Verify both movement records exist.
                var movements = await context.ToolMovements
                    .AsNoTracking()
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                Assert.Equal(2, movements.Count);

                var damageMovement = Assert.Single(
                    movements.Where(item =>
                        item.Action == ToolMovementActions.Damage));

                Assert.Equal(3, damageMovement.Quantity);

                var repairMovement = Assert.Single(
                    movements.Where(item =>
                        item.Action == ToolMovementActions.Repair));

                Assert.Equal(2, repairMovement.Quantity);

                // Verify 1 damaged tool is not yet repaired.
                var notYetRepaired =
                    damageMovement.Quantity - repairMovement.Quantity;

                Assert.Equal(1, notYetRepaired);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools
                    .SingleOrDefaultAsync(item => item.Id == tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Repair_ExceedsNotYetRepairedQuantity_RejectsRequest()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                // Damage 3 tools through the controller.
                var damageResult = await controller.CreateMovement(
                    tool.Id,
                    new CreateToolMovementDto
                    {
                        Action = ToolMovementActions.Damage,
                        Quantity = 3,
                        PerformedBy = "Automated Test"
                    });

                Assert.IsType<CreatedAtActionResult>(damageResult.Result);

                // Repair 1 tool through the controller.
                var repairResult = await controller.CreateMovement(
                    tool.Id,
                    new CreateToolMovementDto
                    {
                        Action = ToolMovementActions.Repair,
                        Quantity = 1,
                        PerformedBy = "Automated Test"
                    });

                Assert.IsType<CreatedAtActionResult>(repairResult.Result);

                // Only 2 damaged tools remain unrepaired.
                // Attempt to repair 3, which must be rejected.
                var invalidRepairResult = await controller.CreateMovement(
                    tool.Id,
                    new CreateToolMovementDto
                    {
                        Action = ToolMovementActions.Repair,
                        Quantity = 3,
                        PerformedBy = "Automated Test"
                    });

                Assert.IsType<BadRequestObjectResult>(
                    invalidRepairResult.Result);

                // Verify stock remains unchanged after the rejected request.
                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(3, savedTool.Quantity);

                // Verify the rejected repair was not recorded.
                var movements = await context.ToolMovements
                    .AsNoTracking()
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                Assert.Equal(2, movements.Count);

                Assert.Equal(
                    3,
                    movements
                        .Where(item => item.Action == ToolMovementActions.Damage)
                        .Sum(item => item.Quantity));

                Assert.Equal(
                    1,
                    movements
                        .Where(item => item.Action == ToolMovementActions.Repair)
                        .Sum(item => item.Quantity));
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools
                    .SingleOrDefaultAsync(item => item.Id == tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Checkout_ZeroQuantity_RejectsRequestWithoutChangingStock()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Checkout,
                    Quantity = 0,
                    PerformedBy = "Automated Test"
                };

                var result = await controller.CreateMovement(tool.Id, dto);

                Assert.IsType<BadRequestObjectResult>(result.Result);

                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(5, savedTool.Quantity);

                var movementCount = await context.ToolMovements
                    .AsNoTracking()
                    .CountAsync(item => item.ToolId == tool.Id);

                Assert.Equal(0, movementCount);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools
                    .SingleOrDefaultAsync(item => item.Id == tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }


        [Fact]
        public async Task Checkout_NegativeQuantity_RejectsRequestWithoutChangingStock()
        {
            await using var context = CreateDbContext();

            var tool = new Tool
            {
                ToolCode = $"T-{Guid.NewGuid():N}"[..20],
                Name = "Test Tool",
                Quantity = 5,
                Status = "Available"
            };

            context.Tools.Add(tool);
            await context.SaveChangesAsync();

            try
            {
                var controller = new ToolMovementsController(context);

                var dto = new CreateToolMovementDto
                {
                    Action = ToolMovementActions.Checkout,
                    Quantity = -3,
                    PerformedBy = "Automated Test"
                };

                var result = await controller.CreateMovement(tool.Id, dto);

                Assert.IsType<BadRequestObjectResult>(result.Result);

                context.ChangeTracker.Clear();

                var savedTool = await context.Tools
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == tool.Id);

                Assert.Equal(5, savedTool.Quantity);

                var movementCount = await context.ToolMovements
                    .AsNoTracking()
                    .CountAsync(item => item.ToolId == tool.Id);

                Assert.Equal(0, movementCount);
            }
            finally
            {
                var movements = await context.ToolMovements
                    .Where(item => item.ToolId == tool.Id)
                    .ToListAsync();

                context.ToolMovements.RemoveRange(movements);

                var savedTool = await context.Tools
                    .SingleOrDefaultAsync(item => item.Id == tool.Id);

                if (savedTool is not null)
                {
                    context.Tools.Remove(savedTool);
                }

                await context.SaveChangesAsync();
            }
        }

    }
}
