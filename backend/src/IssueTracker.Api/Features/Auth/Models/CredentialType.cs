namespace IssueTracker.Api.Features.Auth.Models;

/// <summary>
/// Lookup entity defining supported authentication credential types (e.g. Password, OAuth_Google).
/// </summary>
public class CredentialType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<Credential> Credentials { get; set; } = new();
}
