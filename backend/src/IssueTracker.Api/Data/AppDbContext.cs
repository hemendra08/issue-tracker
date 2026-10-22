using IssueTracker.Api.Features.Auth.Models;
using IssueTracker.Api.Features.Projects.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Data;

/// <summary>
/// Application database context managing all feature tables in PostgreSQL.
/// Tables: Identities, CredentialTypes, Credentials, CredentialIdentifiers, Projects, ProjectMembers, RevokedTokens.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Identity & Authentication tables
    public DbSet<User> Users => Set<User>();
    public DbSet<User> Identities => Set<User>();
    public DbSet<CredentialType> CredentialTypes => Set<CredentialType>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<CredentialIdentifier> CredentialIdentifiers => Set<CredentialIdentifier>();

    // Backward-compatible accessors
    public DbSet<CredentialIdentifier> UserIdentifiers => Set<CredentialIdentifier>();
    public DbSet<Credential> UserCredentials => Set<Credential>();

    // Projects & Members tables
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    // Security & Revocation tables
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from all features
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
