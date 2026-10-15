namespace IssueTracker.Api.Features.Issues;

public static class IssueEndpoints
{
    public static IEndpointRouteBuilder MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/issues")
            .WithTags("Issues");

        group.MapGet("/health/ping", () => Results.Ok(new
        {
            feature = "Issues",
            status = "pong",
            timestamp = DateTime.UtcNow
        }))
        .WithName("IssuesPing")
        .WithSummary("Ping endpoint for Issues feature");

        // Milestone 4 endpoints (Create Issue, Update Status, Assign, Comments) will be added here!

        return app;
    }
}
