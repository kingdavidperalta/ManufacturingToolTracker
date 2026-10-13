using ManufacturingToolTracker.Api.Data;
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

            var canConnect = await context.Database.CanConnectAsync();

            Assert.True(
                canConnect,
                "Cannot connect to ManufacturingToolTrackerTestDb.");
        }
    }
}
