using Microsoft.EntityFrameworkCore;

namespace Nerve_Practice.Models
{
    public class AppDbContext:DbContext
    {
        public DbSet<Node> Node { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    }
}
