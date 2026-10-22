using IssueTracker.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Api.Features.Auth.Data;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("RevokedTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TokenId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(r => r.TokenId)
            .IsUnique();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.RevokedAtUtc)
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .IsRequired();
    }
}
