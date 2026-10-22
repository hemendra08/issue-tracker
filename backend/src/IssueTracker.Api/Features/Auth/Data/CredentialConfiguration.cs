using IssueTracker.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Api.Features.Auth.Data;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CredentialData)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(c => c.Identity)
            .WithMany(u => u.Credentials)
            .HasForeignKey(c => c.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CredentialType)
            .WithMany(ct => ct.Credentials)
            .HasForeignKey(c => c.CredentialTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
