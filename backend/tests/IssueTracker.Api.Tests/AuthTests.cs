using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IssueTracker.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IssueTracker.Api.Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string JwtSecret = "SuperSecretDevelopmentKeyForIssueTrackerJwtSigning_2026!";

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Scenario01_Register_WithValidData_Returns201CreatedAndUserWithoutPasswordHash()
    {
        var uniqueEmail = $"user-{Guid.NewGuid():N}@example.com";
        var payload = new
        {
            email = uniqueEmail,
            displayName = "Valid User",
            password = "SecurePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("id", out var idProp));
        Assert.NotEqual(Guid.Empty, idProp.GetGuid());
        Assert.Equal(uniqueEmail, root.GetProperty("email").GetString());
        Assert.Equal("Valid User", root.GetProperty("displayName").GetString());
        Assert.True(root.TryGetProperty("createdAtUtc", out _));

        // Password hash must never be returned
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("password", out _));
    }

    [Theory]
    [InlineData("", "Valid Name", "SecurePassword123!", "Email")]
    [InlineData("not-an-email", "Valid Name", "SecurePassword123!", "Email")]
    [InlineData("valid@example.com", "", "SecurePassword123!", "DisplayName")]
    [InlineData("valid@example.com", "Valid Name", "short", "Password")]
    public async Task Scenario02_Register_WithInvalidData_Returns400BadRequestWithValidationErrors(
        string email, string displayName, string password, string expectedErrorKey)
    {
        var payload = new
        {
            email,
            displayName,
            password
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errorsProp));
        Assert.True(errorsProp.TryGetProperty(expectedErrorKey, out _));
    }

    [Fact]
    public async Task Scenario03_Register_WithDuplicateEmail_Returns409Conflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var payload1 = new
        {
            email = email.ToLowerInvariant(),
            displayName = "First User",
            password = "SecurePassword123!"
        };
        var payload2 = new
        {
            email = email.ToUpperInvariant(), // Test case insensitivity
            displayName = "Duplicate User",
            password = "AnotherPassword123!"
        };

        var firstResp = await _client.PostAsJsonAsync("/api/v1/auth/register", payload1);
        Assert.Equal(HttpStatusCode.Created, firstResp.StatusCode);

        var secondResp = await _client.PostAsJsonAsync("/api/v1/auth/register", payload2);
        Assert.Equal(HttpStatusCode.Conflict, secondResp.StatusCode);

        var content = await secondResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal(409, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Scenario04_Login_WithValidCredentials_Returns200WithSignedJwt()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        var password = "CorrectPassword123!";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName = "Login Test User",
            password
        });

        var loginPayload = new { email, password };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("accessToken", out var tokenProp));
        var token = tokenProp.GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        Assert.Equal("Bearer", root.GetProperty("tokenType").GetString());
        Assert.True(root.GetProperty("expiresIn").GetInt32() > 0);

        // Verify token can be parsed
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("IssueTracker.Api", jwt.Issuer);
    }

    [Fact]
    public async Task Scenario05_Login_WithIncorrectPassword_Returns401UnauthorizedWithGenericMessage()
    {
        var email = $"wrongpass-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName = "User",
            password = "ActualPassword123!"
        });

        var loginPayload = new { email, password = "WrongPassword123!" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Invalid email or password.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Scenario06_Login_WithUnknownEmail_Returns401UnauthorizedWithGenericMessage()
    {
        var loginPayload = new
        {
            email = $"nonexistent-{Guid.NewGuid():N}@example.com",
            password = "SomePassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Invalid email or password.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Scenario07_GetMe_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Scenario08_GetMe_WithValidToken_Returns200WithCurrentUser()
    {
        var email = $"me-{Guid.NewGuid():N}@example.com";
        var displayName = "Me User";
        var password = "SecurePassword123!";

        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName,
            password
        });
        var regContent = await regResp.Content.ReadAsStringAsync();
        using var regDoc = JsonDocument.Parse(regContent);
        var expectedUserId = regDoc.RootElement.GetProperty("id").GetGuid();

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var loginContent = await loginResp.Content.ReadAsStringAsync();
        using var loginDoc = JsonDocument.Parse(loginContent);
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var meResp = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

        var meContent = await meResp.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(meContent);
        var meRoot = meDoc.RootElement;

        Assert.Equal(expectedUserId, meRoot.GetProperty("id").GetGuid());
        Assert.Equal(email, meRoot.GetProperty("email").GetString());
        Assert.Equal(displayName, meRoot.GetProperty("displayName").GetString());
        Assert.True(meRoot.TryGetProperty("createdAtUtc", out _));

        // Password hash must not appear
        Assert.False(meRoot.TryGetProperty("passwordHash", out _));
    }

    [Fact]
    public async Task Scenario09_GetMe_WithInvalidOrExpiredToken_Returns401Unauthorized()
    {
        // 9a. Tampered token
        var tamperedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        tamperedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
        var tamperedResp = await _client.SendAsync(tamperedRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, tamperedResp.StatusCode);

        // 9b. Token signed with wrong key
        var wrongKeyToken = GenerateTokenWithWrongKey(Guid.NewGuid());
        var wrongKeyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        wrongKeyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wrongKeyToken);
        var wrongKeyResp = await _client.SendAsync(wrongKeyRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongKeyResp.StatusCode);

        // 9c. Expired token
        var expiredToken = GenerateExpiredToken(Guid.NewGuid());
        var expiredRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        expiredRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var expiredResp = await _client.SendAsync(expiredRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResp.StatusCode);
    }

    [Fact]
    public async Task Scenario10_PasswordHashes_NeverAppearInAnyResponse()
    {
        var email = $"leakcheck-{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";

        // Check Register
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName = "Leak Check",
            password
        });
        var regJson = await regResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", regJson, StringComparison.OrdinalIgnoreCase);

        // Check Login
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var loginJson = await loginResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", loginJson, StringComparison.OrdinalIgnoreCase);

        using var loginDoc = JsonDocument.Parse(loginJson);
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();

        // Check Me
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResp = await _client.SendAsync(meReq);
        var meJson = await meResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", meJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scenario11_Register_CreatesConnectedIdentityIdentifiersAndCredentialsInDatabase()
    {
        var email = $"connected-{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";
        var displayName = "Connected Identity Test";

        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName,
            password
        });
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        var regJson = await regResp.Content.ReadAsStringAsync();
        using var regDoc = JsonDocument.Parse(regJson);
        var userId = regDoc.RootElement.GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Verify Identity (Users: Id, FullName, Status) exists
        var user = await db.Users
            .Include(u => u.Identifiers)
            .Include(u => u.Credentials)
            .FirstOrDefaultAsync(u => u.Id == userId);

        Assert.NotNull(user);
        Assert.Equal(displayName, user.FullName);
        Assert.Equal("Active", user.Status);
        Assert.Equal(email, user.Email);

        // 2. Verify Identifier (UserIdentifiers: UserId, Type, Value)
        Assert.Single(user.Identifiers);
        var identifier = user.Identifiers.First();
        Assert.Equal(user.Id, identifier.UserId);
        Assert.Equal("Email", identifier.Type);
        Assert.Equal(email, identifier.Value);

        // 3. Verify Credential (UserCredentials: UserId, Type, CredentialData)
        Assert.Single(user.Credentials);
        var credential = user.Credentials.First();
        Assert.Equal(user.Id, credential.UserId);
        Assert.Equal("Password", credential.Type);
        Assert.False(string.IsNullOrWhiteSpace(credential.CredentialData));

        // 4. Verify password hashing on the linked credential
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<IssueTracker.Api.Features.Auth.Models.User>>();
        var verifyResult = hasher.VerifyHashedPassword(user, credential.CredentialData, password);
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);

        // 5. Verify direct table queries for systematic relational integrity across all 4 tables
        var dbIdentity = await db.Identities.FirstOrDefaultAsync(i => i.Id == userId);
        Assert.NotNull(dbIdentity);

        var dbCredential = await db.Credentials
            .Include(c => c.CredentialType)
            .FirstOrDefaultAsync(c => c.IdentityId == userId);
        Assert.NotNull(dbCredential);
        Assert.Equal(1, dbCredential.CredentialTypeId);
        Assert.Equal("Password", dbCredential.CredentialType.Code);

        var dbIdentifier = await db.CredentialIdentifiers
            .FirstOrDefaultAsync(ci => ci.IdentityId == userId && ci.Type == "Email");
        Assert.NotNull(dbIdentifier);
        Assert.Equal(dbCredential.Id, dbIdentifier.CredentialId);
        Assert.Equal(email, dbIdentifier.Value);
        Assert.True(dbIdentifier.IsPrimary);
    }

    private static string GenerateExpiredToken(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(JwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }),
            IssuedAt = DateTime.UtcNow.AddMinutes(-30),
            NotBefore = DateTime.UtcNow.AddMinutes(-30),
            Expires = DateTime.UtcNow.AddMinutes(-10),
            Issuer = "IssueTracker.Api",
            Audience = "IssueTracker.Client",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateTokenWithWrongKey(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = Encoding.UTF8.GetBytes("WrongKeyThatShouldBeRejectedByTheServer1234567890!");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            Issuer = "IssueTracker.Api",
            Audience = "IssueTracker.Client",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(wrongKey),
                SecurityAlgorithms.HmacSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task Scenario12_BackwardCompatibility_IdentityEndpointAlias_StillFunctions()
    {
        var email = $"alias-{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";

        var regResp = await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email,
            displayName = "Alias Test User",
            password
        });
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        var loginResp = await _client.PostAsJsonAsync("/api/v1/identity/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var loginJson = await loginResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginJson);
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meResp = await _client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
    }

    [Fact]
    public async Task Scenario13_Logout_RevokesToken_SubsequentRequestsReturn401Unauthorized()
    {
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";

        // 1. Register & Login
        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName = "Logout Test User",
            password
        });
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var loginJson = await loginResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(loginJson);
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        // 2. Call /me before logout -> 200 OK
        var beforeLogoutReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        beforeLogoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var beforeResp = await _client.SendAsync(beforeLogoutReq);
        Assert.Equal(HttpStatusCode.OK, beforeResp.StatusCode);

        // 3. Call /logout -> 200 OK
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var logoutResp = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);

        // 4. Call /me after logout -> 401 Unauthorized (token revoked!)
        var afterLogoutReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        afterLogoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var afterResp = await _client.SendAsync(afterLogoutReq);
        Assert.Equal(HttpStatusCode.Unauthorized, afterResp.StatusCode);
    }

    [Fact]
    public async Task Scenario14_InternalUsers_HttpEndpoints_EnableInterServiceCalls()
    {
        var email = $"internal-{Guid.NewGuid():N}@example.com";
        var password = "SecurePassword123!";
        var displayName = "Internal Service User";

        var regResp = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            displayName,
            password
        });
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);
        var regJson = await regResp.Content.ReadFromJsonAsync<JsonElement>();
        var userId = regJson.GetProperty("id").GetGuid();

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var loginJson = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("accessToken").GetString();

        // 1. GET /api/v1/users/{id}
        var getByIdReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{userId}");
        getByIdReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getByIdResp = await _client.SendAsync(getByIdReq);
        Assert.Equal(HttpStatusCode.OK, getByIdResp.StatusCode);

        var userInfo = await getByIdResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(userId, userInfo.GetProperty("id").GetGuid());
        Assert.Equal(email, userInfo.GetProperty("email").GetString());
        Assert.Equal(displayName, userInfo.GetProperty("displayName").GetString());

        // 2. GET /api/v1/users/by-email/{email}
        var getByEmailReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/by-email/{email}");
        getByEmailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getByEmailResp = await _client.SendAsync(getByEmailReq);
        Assert.Equal(HttpStatusCode.OK, getByEmailResp.StatusCode);

        // 3. POST /api/v1/users/batch
        var batchReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/batch")
        {
            Content = JsonContent.Create(new List<Guid> { userId })
        };
        batchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var batchResp = await _client.SendAsync(batchReq);
        Assert.Equal(HttpStatusCode.OK, batchResp.StatusCode);
        var batchList = await batchResp.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(batchList);
        Assert.Single(batchList);
        Assert.Equal(userId, batchList[0].GetProperty("id").GetGuid());
    }
}
