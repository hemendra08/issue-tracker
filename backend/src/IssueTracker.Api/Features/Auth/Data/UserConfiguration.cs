using IssueTracker.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueTracker.Api.Features.Auth.Data;

/// <summary>
/// EF Core configuration for the Identities table (mapped via User entity).
/// Schema: Id, FullName, Status, CreatedAtUtc
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Identities");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Active");

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(u => u.DisplayName);
        builder.Ignore(u => u.Email);
    }
}
