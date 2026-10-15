namespace IssueTracker.Api.Features.Projects;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/projects")
            .WithTags("Projects");

        group.MapGet("/health/ping", () => Results.Ok(new
        {
            feature = "Projects",
            status = "pong",
            timestamp = DateTime.UtcNow
        }))
        .WithName("ProjectsPing")
        .WithSummary("Ping endpoint for Projects feature");

        // Milestone 3 endpoints (Create Project, List Projects, etc.) will be added here!

        return app;
    }
}
