using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Projects.Services;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Auth.Services;

public class UserDirectoryService : IUserDirectoryService
{
    private readonly AppDbContext _db;

    public UserDirectoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthUserInfo?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var targetEmail = email.Trim().ToLower();
        var identifier = await _db.CredentialIdentifiers
            .AsNoTracking()
            .Include(i => i.Identity)
            .FirstOrDefaultAsync(i => i.Type == "Email" && i.Value.ToLower() == targetEmail, cancellationToken);

        return identifier?.Identity is null ? null : new AuthUserInfo(identifier.Identity.Id, identifier.Value, identifier.Identity.FullName);
    }

    public async Task<AuthUserInfo?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Identifiers)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : new AuthUserInfo(user.Id, user.Email, user.FullName);
    }

    public async Task<Dictionary<Guid, AuthUserInfo>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.Identifiers)
            .Where(u => distinctIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => new AuthUserInfo(u.Id, u.Email, u.FullName));
    }
}
