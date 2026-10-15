using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Data;

/// <summary>
/// Application database context managing all feature tables in PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Feature entities will be registered here (e.g., Users, Projects, Issues)
    }
}
