using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IssueTracker.Api.Features.Projects.Dtos;
using IssueTracker.Api.Features.Projects.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IssueTracker.Api.Tests;

public class ProjectTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProjectTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string Token, Guid UserId, string Email)> CreateAndLoginUserAsync(string prefix = "user")
    {
        var email = $"{prefix}_{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";
        var displayName = $"{prefix} Name";

        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName,
            password
        });
        regResp.EnsureSuccessStatusCode();
        var regJson = await regResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = regJson.GetProperty("id").GetGuid();

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });
        loginResp.EnsureSuccessStatusCode();
        var loginJson = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("accessToken").GetString()!;

        return (token, userId, email);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url, string token, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            req.Content = JsonContent.Create(body);
        }
        return req;
    }

    [Fact]
    public async Task Scenario01_CreateProject_Authenticated_Returns201AndOwnerMembership()
    {
        var (token, userId, _) = await CreateAndLoginUserAsync("proj_creator");
        var key = $"P{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var payload = new CreateProjectRequest("Core Engine", key, "Core project description");

        var req = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token, payload);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var proj = await res.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.NotNull(proj);
        Assert.Equal("Core Engine", proj.Name);
        Assert.Equal(key, proj.Key);
        Assert.Equal(userId, proj.OwnerId);
        Assert.Equal("Owner", proj.CurrentUserRole);
        Assert.Equal(1, proj.MemberCount);
        Assert.False(proj.IsArchived);
    }

    [Fact]
    public async Task Scenario02_CreateProject_Unauthenticated_Returns401Unauthorized()
    {
        var payload = new CreateProjectRequest("Unauth Proj", "UNAUTH", "Desc");
        var res = await _client.PostAsJsonAsync("/api/v1/projects", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Scenario03_CreateProject_DuplicateKey_Returns409Conflict()
    {
        var (token, _, _) = await CreateAndLoginUserAsync("dup_key");
        var key = $"D{Guid.NewGuid():N}"[..6].ToUpperInvariant();

        var req1 = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token, new CreateProjectRequest("P1", key, "Desc"));
        var res1 = await _client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        var req2 = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token, new CreateProjectRequest("P2", key, "Desc2"));
        var res2 = await _client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Scenario04_ListProjects_ReturnsOnlyProjectsUserBelongsTo()
    {
        var (token1, _, _) = await CreateAndLoginUserAsync("user_one");
        var (token2, _, _) = await CreateAndLoginUserAsync("user_two");

        var key1 = $"K{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var key2 = $"K{Guid.NewGuid():N}"[..6].ToUpperInvariant();

        // User 1 creates Project 1
        var req1 = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token1, new CreateProjectRequest("User1 Project", key1, null));
        await _client.SendAsync(req1);

        // User 2 creates Project 2
        var req2 = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", token2, new CreateProjectRequest("User2 Project", key2, null));
        await _client.SendAsync(req2);

        // User 1 lists projects: should only see Project 1
        var listReq = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/projects", token1);
        var listRes = await _client.SendAsync(listReq);
        var paged = await listRes.Content.ReadFromJsonAsync<PagedProjectsResponse>(JsonOptions);

        Assert.NotNull(paged);
        Assert.Contains(paged.Items, p => p.Key == key1);
        Assert.DoesNotContain(paged.Items, p => p.Key == key2);
    }

    [Fact]
    public async Task Scenario05_GetProject_NonMember_Returns404NotFound()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner");
        var (otherToken, _, _) = await CreateAndLoginUserAsync("outsider");

        var key = $"S{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Secret Project", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Outsider attempts to get project by ID
        var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project!.Id}", otherToken);
        var getRes = await _client.SendAsync(getReq);

        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Scenario06_Members_AddExistingUser_SucceedsWith201()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("proj_owner");
        var (_, memberId, memberEmail) = await CreateAndLoginUserAsync("new_member");

        var key = $"M{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Collab Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Add member
        var addReq = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/projects/{project!.Id}/members",
            ownerToken,
            new AddMemberRequest(memberEmail, "Member"));
        var addRes = await _client.SendAsync(addReq);

        Assert.Equal(HttpStatusCode.Created, addRes.StatusCode);
        var member = await addRes.Content.ReadFromJsonAsync<ProjectMemberResponse>(JsonOptions);
        Assert.NotNull(member);
        Assert.Equal(memberId, member.UserId);
        Assert.Equal(memberEmail, member.Email);
        Assert.Equal("Member", member.Role);
    }

    [Fact]
    public async Task Scenario07_Members_AddUnknownEmail_Returns404NotFound()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_unknown");
        var key = $"U{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        var addReq = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/projects/{project!.Id}/members",
            ownerToken,
            new AddMemberRequest("nobody_exists_12345@example.com", "Member"));
        var addRes = await _client.SendAsync(addReq);

        Assert.Equal(HttpStatusCode.NotFound, addRes.StatusCode);
    }

    [Fact]
    public async Task Scenario08_Members_DuplicateMembership_Returns409Conflict()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_dup");
        var (_, _, memberEmail) = await CreateAndLoginUserAsync("target_member");

        var key = $"D{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Add first time
        var addReq1 = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(memberEmail, "Member"));
        var addRes1 = await _client.SendAsync(addReq1);
        Assert.Equal(HttpStatusCode.Created, addRes1.StatusCode);

        // Add second time
        var addReq2 = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(memberEmail, "Member"));
        var addRes2 = await _client.SendAsync(addReq2);
        Assert.Equal(HttpStatusCode.Conflict, addRes2.StatusCode);
    }

    [Fact]
    public async Task Scenario09_Authorization_MemberCannotEditProject_Returns403Forbidden()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_edit");
        var (memberToken, _, memberEmail) = await CreateAndLoginUserAsync("member_edit");

        var key = $"E{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Add user as Member
        var addReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(memberEmail, "Member"));
        await _client.SendAsync(addReq);

        // Member attempts to update project
        var updateReq = CreateAuthenticatedRequest(HttpMethod.Patch, $"/api/v1/projects/{project.Id}", memberToken, new UpdateProjectRequest("Hacked Name", null));
        var updateRes = await _client.SendAsync(updateReq);

        Assert.Equal(HttpStatusCode.Forbidden, updateRes.StatusCode);
    }

    [Fact]
    public async Task Scenario10_Authorization_AdminCannotPromoteToAdmin_Returns403Forbidden()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_admin_test");
        var (adminToken, _, adminEmail) = await CreateAndLoginUserAsync("admin_user");
        var (_, _, user3Email) = await CreateAndLoginUserAsync("user3");

        var key = $"A{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Owner adds admin_user as Admin
        var addAdminReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(adminEmail, "Admin"));
        await _client.SendAsync(addAdminReq);

        // Admin attempts to add user3 as Admin
        var adminAddAdminReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/members", adminToken, new AddMemberRequest(user3Email, "Admin"));
        var adminAddAdminRes = await _client.SendAsync(adminAddAdminReq);

        Assert.Equal(HttpStatusCode.Forbidden, adminAddAdminRes.StatusCode);
    }

    [Fact]
    public async Task Scenario11_Authorization_OwnerCannotBeRemovedOrLeave()
    {
        var (ownerToken, ownerId, _) = await CreateAndLoginUserAsync("owner_immutability");

        var key = $"O{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Owner Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Owner attempts to delete themselves via members API
        var delReq = CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/v1/projects/{project!.Id}/members/{ownerId}", ownerToken);
        var delRes = await _client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.BadRequest, delRes.StatusCode);

        // Owner attempts to leave project
        var leaveReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/leave", ownerToken);
        var leaveRes = await _client.SendAsync(leaveReq);
        Assert.Equal(HttpStatusCode.BadRequest, leaveRes.StatusCode);
    }

    [Fact]
    public async Task Scenario12_ArchiveAndRestore_OwnerSucceeds_AdminForbidden()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_arc");
        var (adminToken, _, adminEmail) = await CreateAndLoginUserAsync("admin_arc");

        var key = $"R{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Archivable", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Add admin
        var addAdminReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(adminEmail, "Admin"));
        await _client.SendAsync(addAdminReq);

        // Admin attempts to archive: 403 Forbidden
        var adminArcReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/archive", adminToken);
        var adminArcRes = await _client.SendAsync(adminArcReq);
        Assert.Equal(HttpStatusCode.Forbidden, adminArcRes.StatusCode);

        // Owner archives: 200 OK
        var ownerArcReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/archive", ownerToken);
        var ownerArcRes = await _client.SendAsync(ownerArcReq);
        Assert.Equal(HttpStatusCode.OK, ownerArcRes.StatusCode);
        var archivedProj = await ownerArcRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.True(archivedProj!.IsArchived);

        // When archived, modifications are rejected
        var editReq = CreateAuthenticatedRequest(HttpMethod.Patch, $"/api/v1/projects/{project.Id}", ownerToken, new UpdateProjectRequest("New Name", null));
        var editRes = await _client.SendAsync(editReq);
        Assert.Equal(HttpStatusCode.BadRequest, editRes.StatusCode);

        // Owner restores: 200 OK
        var ownerRestoreReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/restore", ownerToken);
        var ownerRestoreRes = await _client.SendAsync(ownerRestoreReq);
        Assert.Equal(HttpStatusCode.OK, ownerRestoreRes.StatusCode);
        var restoredProj = await ownerRestoreRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.False(restoredProj!.IsArchived);
    }

    [Fact]
    public async Task Scenario13_MemberLeavesProject_SucceedsWith204()
    {
        var (ownerToken, _, _) = await CreateAndLoginUserAsync("owner_leave");
        var (memberToken, _, memberEmail) = await CreateAndLoginUserAsync("member_leave");

        var key = $"L{Guid.NewGuid():N}"[..6].ToUpperInvariant();
        var createReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/projects", ownerToken, new CreateProjectRequest("Leave Proj", key, null));
        var createRes = await _client.SendAsync(createReq);
        var project = await createRes.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);

        // Add member
        var addReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project!.Id}/members", ownerToken, new AddMemberRequest(memberEmail, "Member"));
        await _client.SendAsync(addReq);

        // Member leaves
        var leaveReq = CreateAuthenticatedRequest(HttpMethod.Post, $"/api/v1/projects/{project.Id}/leave", memberToken);
        var leaveRes = await _client.SendAsync(leaveReq);
        Assert.Equal(HttpStatusCode.NoContent, leaveRes.StatusCode);

        // Now member should get 404 when trying to view project
        var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/projects/{project.Id}", memberToken);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Scenario14_HttpUserDirectoryService_ResolvesUsersOverHttpRequests()
    {
        var (token, userId, email) = await CreateAndLoginUserAsync("httpuser");

        var httpClient = _factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var httpUserDirectory = new HttpUserDirectoryService(httpClient);

        // 1. Find by ID over HTTP
        var userById = await httpUserDirectory.FindUserByIdAsync(userId);
        Assert.NotNull(userById);
        Assert.Equal(userId, userById.Id);
        Assert.Equal(email, userById.Email);

        // 2. Find by Email over HTTP
        var userByEmail = await httpUserDirectory.FindUserByEmailAsync(email);
        Assert.NotNull(userByEmail);
        Assert.Equal(userId, userByEmail.Id);

        // 3. Batch over HTTP
        var batchResult = await httpUserDirectory.GetUsersByIdsAsync(new[] { userId });
        Assert.True(batchResult.ContainsKey(userId));
        Assert.Equal(email, batchResult[userId].Email);
    }
}
