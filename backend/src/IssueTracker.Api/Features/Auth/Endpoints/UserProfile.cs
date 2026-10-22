using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Auth.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Auth.Endpoints;

public static class UserProfileEndpoint
{
    public static void Map(RouteGroupBuilder group, bool isPrimary = true)
    {
        var me = group.MapGet("/me", HandleGetProfileAsync)
            .RequireAuthorization();

        if (isPrimary)
        {
            me.WithName("GetCurrentUser")
                .WithSummary("Retrieves profile information for the authenticated user");
        }
    }

    private static async Task<IResult> HandleGetProfileAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                       ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .Include(u => u.Identifiers)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "User account no longer exists."
            });
        }

        return Results.Ok(new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAtUtc));
    }
}
