namespace IssueTracker.Api.Features.Auth.Models;

/// <summary>
/// Domain entity representing a secret/authentication credential belonging to an identity.
/// </summary>
public class Credential
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IdentityId { get; set; }

    public int CredentialTypeId { get; set; }

    public string CredentialData { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Identity { get; set; } = null!;

    public CredentialType CredentialType { get; set; } = null!;

    public List<CredentialIdentifier> Identifiers { get; set; } = new();

    // Backward compatibility helpers
    public Guid UserId => IdentityId;
    public string Type => CredentialType?.Code ?? "Password";
}
