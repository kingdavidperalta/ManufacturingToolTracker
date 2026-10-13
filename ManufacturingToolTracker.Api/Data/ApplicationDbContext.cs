using ManufacturingToolTracker.Api.Configurations;
using ManufacturingToolTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufacturingToolTracker.Api.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Tool> Tools => Set<Tool>();
        public DbSet<ToolMovement> ToolMovements => Set<ToolMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
