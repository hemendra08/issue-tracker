namespace IssueTracker.Api.Middleware;

/// <summary>
/// Provides fluent extension methods for configuring application middleware pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Extracts or generates a Request ID (correlation ID) and attaches it to response headers and log scopes.
    /// </summary>
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder app)
        => app.UseMiddleware<RequestIdMiddleware>();

    /// <summary>
    /// Catches unhandled exceptions and returns standardized RFC 7807 problem details responses.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
