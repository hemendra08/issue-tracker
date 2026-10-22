namespace IssueTracker.Api.Features.Projects.Services;

public record AuthUserInfo(Guid Id, string Email, string DisplayName);

// Backward-compatible alias
public record IdentityUserInfo(Guid Id, string Email, string DisplayName);

/// <summary>
/// Narrow abstraction allowing the Projects feature to resolve user details
/// without directly depending on Auth internal entities.
/// When extracted to microservices, this can be substituted by an HTTP client.
/// </summary>
public interface IUserDirectoryService
{
    Task<AuthUserInfo?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AuthUserInfo?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, AuthUserInfo>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
