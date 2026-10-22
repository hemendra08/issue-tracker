using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Auth.Dtos;
using IssueTracker.Api.Features.Auth.Models;
using IssueTracker.Api.Features.Auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Auth.Endpoints;

public static class LoginEndpoint
{
    public static void Map(RouteGroupBuilder group, bool isPrimary = true)
    {
        var login = group.MapPost("/login", HandleLoginAsync);
        if (isPrimary)
        {
            login.WithName("LoginUser")
                .WithSummary("Authenticates credentials and returns a signed JWT access token");
        }
    }

    private static async Task<IResult> HandleLoginAsync(
        [FromBody] LoginRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid email or password.");
        }

        var email = request.Email.Trim().ToLower();

        var identifier = await db.CredentialIdentifiers
            .Include(i => i.Identity)
                .ThenInclude(u => u.Credentials)
            .Include(i => i.Credential)
            .SingleOrDefaultAsync(
                i => i.Type == "Email" && i.Value.ToLower() == email,
                cancellationToken);

        if (identifier is null || identifier.Identity is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid email or password.");
        }

        var credential = identifier.Credential 
                      ?? identifier.Identity.Credentials.FirstOrDefault(c => c.CredentialTypeId == 1);
        if (credential is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid email or password.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            identifier.Identity,
            credential.CredentialData,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid email or password.");
        }

        var (accessToken, expiresIn) = tokenService.GenerateToken(identifier.Identity);
        return Results.Ok(new LoginResponse(accessToken, "Bearer", expiresIn));
    }
}
