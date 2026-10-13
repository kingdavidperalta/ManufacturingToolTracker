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
        [Fact]
        public async Task TestDatabase_CanConnect_Succeeds()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString =
                configuration.GetConnectionString("TestConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "TestConnection is missing from appsettings.json.");
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);

            await context.Database.MigrateAsync();

            var canConnect = await context.Database.CanConnectAsync();

            Assert.True(
                canConnect,
                "Cannot connect to ManufacturingToolTrackerTestDb.");
        }


        [Fact]
        public async Task Checkout_ValidQuantity_ReducesStockAndRecordsMovement()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString =
                configuration.GetConnectionString("TestConnection");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);

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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString =
                configuration.GetConnectionString("TestConnection");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);

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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString =
                configuration.GetConnectionString("TestConnection");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);

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

    }
}
