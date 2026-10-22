namespace IssueTracker.Api.Features.Auth.Models;

/// <summary>
/// Domain entity representing a login handle or identifier (e.g. Email, Username, Phone)
/// pointing to an identity and its associated credential.
/// </summary>
public class CredentialIdentifier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IdentityId { get; set; }

    public Guid? CredentialId { get; set; }

    public string Type { get; set; } = "Email";

    public string Value { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Identity { get; set; } = null!;

    public Credential? Credential { get; set; }

    // Backward compatibility helpers
    public Guid UserId => IdentityId;
}
