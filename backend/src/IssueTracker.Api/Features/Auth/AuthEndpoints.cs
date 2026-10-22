namespace IssueTracker.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/identity")
            .WithTags("Identity & Auth");

        group.MapGet("/health/ping", () => Results.Ok(new
        {
            feature = "Identity",
            status = "pong",
            timestamp = DateTime.UtcNow
        }))
        .WithName("IdentityPing")
        .WithSummary("Ping endpoint for Identity feature");

        // Milestone 2 endpoints (Register, Login, Refresh) will be added here!

        return app;
    }
}
