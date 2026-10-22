using IssueTracker.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Api.Features.Auth.Data;

public class CredentialTypeConfiguration : IEntityTypeConfiguration<CredentialType>
{
    public void Configure(EntityTypeBuilder<CredentialType> builder)
    {
        builder.ToTable("CredentialTypes");

        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(ct => ct.Code)
            .IsUnique();

        builder.Property(ct => ct.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ct => ct.Description)
            .HasMaxLength(250);

        builder.Property(ct => ct.CreatedAtUtc)
            .IsRequired();

        // Seed core credential types
        builder.HasData(
            new CredentialType
            {
                Id = 1,
                Code = "Password",
                Name = "Password Authentication",
                Description = "Standard salted password authentication using PBKDF2",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CredentialType
            {
                Id = 2,
                Code = "OAuth_Google",
                Name = "Google OAuth 2.0",
                Description = "Google OpenID Connect identity provider",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CredentialType
            {
                Id = 3,
                Code = "OAuth_GitHub",
                Name = "GitHub OAuth",
                Description = "GitHub Developer OAuth identity provider",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CredentialType
            {
                Id = 4,
                Code = "ApiKey",
                Name = "API Key",
                Description = "Programmatic service-to-service API key authentication",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
