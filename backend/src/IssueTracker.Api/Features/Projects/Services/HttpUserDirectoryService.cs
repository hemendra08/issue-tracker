using System.Net;
using System.Net.Http.Json;

namespace IssueTracker.Api.Features.Projects.Services;

/// <summary>
/// HTTP client implementation of IUserDirectoryService that communicates with the Auth microservice.
/// Demonstrates pure HTTP request based inter-service communication.
/// </summary>
public class HttpUserDirectoryService : IUserDirectoryService
{
    private readonly HttpClient _http;

    public HttpUserDirectoryService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthUserInfo?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var encodedEmail = Uri.EscapeDataString(email.Trim().ToLower());
        var response = await _http.GetAsync($"/api/v1/users/by-email/{encodedEmail}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthUserInfo>(cancellationToken: cancellationToken);
    }

    public async Task<AuthUserInfo?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/v1/users/{userId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthUserInfo>(cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<Guid, AuthUserInfo>> GetUsersByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = userIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<Guid, AuthUserInfo>();
        }

        var response = await _http.PostAsJsonAsync("/api/v1/users/batch", distinctIds, cancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<List<AuthUserInfo>>(cancellationToken: cancellationToken);
        return list?.ToDictionary(u => u.Id, u => u) ?? new Dictionary<Guid, AuthUserInfo>();
    }
}
