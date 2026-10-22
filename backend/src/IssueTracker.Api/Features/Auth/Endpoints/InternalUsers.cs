using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Projects.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Auth.Endpoints;

/// <summary>
/// HTTP endpoints designed for inter-service communication across microservices.
/// Other services (e.g., ProjectService, IssueService) call these endpoints via HTTP.
/// </summary>
public static class InternalUsersEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users (Internal Microservice API)")
            .RequireAuthorization();

        // 1. Get user by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var user = await db.Users
                .AsNoTracking()
                .Include(u => u.Identifiers)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = $"User with ID '{id}' was not found."
                });
            }

            return Results.Ok(new AuthUserInfo(user.Id, user.Email, user.FullName));
        })
        .WithName("GetUserById")
        .WithSummary("Fetches user information by ID (used for microservice HTTP calls)");

        // 2. Get user by email
        group.MapGet("/by-email/{email}", async (
            string email,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Detail = "Email cannot be empty."
                });
            }

            var targetEmail = email.Trim().ToLower();
            var identifier = await db.CredentialIdentifiers
                .AsNoTracking()
                .Include(i => i.Identity)
                .FirstOrDefaultAsync(i => i.Type == "Email" && i.Value.ToLower() == targetEmail, cancellationToken);

            if (identifier?.Identity is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = $"User with email '{email}' was not found."
                });
            }

            return Results.Ok(new AuthUserInfo(identifier.Identity.Id, identifier.Value, identifier.Identity.FullName));
        })
        .WithName("GetUserByEmail")
        .WithSummary("Fetches user information by email (used for member invitations over HTTP)");

        // 3. Batch resolve users by ID
        group.MapPost("/batch", async (
            [FromBody] List<Guid> userIds,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var distinctIds = userIds.Distinct().ToList();
            var users = await db.Users
                .AsNoTracking()
                .Include(u => u.Identifiers)
                .Where(u => distinctIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            var result = users.Select(u => new AuthUserInfo(u.Id, u.Email, u.FullName)).ToList();
            return Results.Ok(result);
        })
        .WithName("GetUsersBatch")
        .WithSummary("Batch resolves multiple user profiles by ID over HTTP");
    }
}
