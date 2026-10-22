using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Auth.Models;
using Microsoft.AspNetCore.Mvc;

namespace IssueTracker.Api.Features.Auth.Endpoints;

public static class LogoutEndpoint
{
    public static void Map(RouteGroupBuilder group, bool isPrimary = true)
    {
        var logout = group.MapPost("/logout", HandleLogoutAsync)
            .RequireAuthorization();

        if (isPrimary)
        {
            logout.WithName("LogoutUser")
                .WithSummary("Revokes the caller's JWT access token so it cannot be reused");
        }
    }

    private static async Task<IResult> HandleLogoutAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                       ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        // If a jti is present, revoke it in the database
        if (!string.IsNullOrWhiteSpace(jti))
        {
            var expClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);
            var expiresAtUtc = DateTime.UtcNow.AddHours(1);

            if (long.TryParse(expClaim, out var expSeconds))
            {
                expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            }

            var revokedToken = new RevokedToken
            {
                TokenId = jti,
                UserId = userId,
                RevokedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAtUtc
            };

            db.RevokedTokens.Add(revokedToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(new { message = "Successfully logged out." });
    }
}
