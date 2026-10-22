using IssueTracker.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Api.Features.Auth.Data;

public class CredentialIdentifierConfiguration : IEntityTypeConfiguration<CredentialIdentifier>
{
    public void Configure(EntityTypeBuilder<CredentialIdentifier> builder)
    {
        builder.ToTable("CredentialIdentifiers");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ci => ci.Value)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(ci => new { ci.Type, ci.Value })
            .IsUnique();

        builder.Property(ci => ci.IsPrimary)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ci => ci.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(ci => ci.Identity)
            .WithMany(u => u.Identifiers)
            .HasForeignKey(ci => ci.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Credential)
            .WithMany(c => c.Identifiers)
            .HasForeignKey(ci => ci.CredentialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
