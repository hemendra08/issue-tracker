namespace IssueTracker.Api.Features.Auth.Models;

/// <summary>
/// Tracks revoked JWT access tokens by their unique token ID (jti).
/// </summary>
public class RevokedToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
