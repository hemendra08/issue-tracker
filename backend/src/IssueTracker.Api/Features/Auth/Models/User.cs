namespace IssueTracker.Api.Features.Auth.Models;

/// <summary>
/// Domain entity representing an authenticated user identity root.
/// Table: Identities
/// Schema: Id, FullName, Status, CreatedAtUtc
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Backward-compatible accessors for callers, API responses, and JWT generation
    public string DisplayName => FullName;
    public string Email => Identifiers.FirstOrDefault(i => i.Type == "Email")?.Value ?? string.Empty;

    // Navigation properties
    public List<CredentialIdentifier> Identifiers { get; set; } = new();

    public List<Credential> Credentials { get; set; } = new();
}
