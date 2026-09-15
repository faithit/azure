using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CloudOps.Infrastructure.Persistence;

/// <summary>Supplies EF tooling with a context without requiring runtime secrets.</summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(DatabaseConnectionString.ResolveFromEnvironment("Host=localhost;Database=cloudops"))
            .Options);
    }
}
