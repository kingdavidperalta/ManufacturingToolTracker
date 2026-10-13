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
    }
}
