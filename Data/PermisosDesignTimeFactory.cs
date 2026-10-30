using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UniConnect.Data;

// Allows migration scaffolding without executing Program.cs (which migrates
// and seeds the runtime database). This factory never opens a connection.
public sealed class PermisosDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
}
