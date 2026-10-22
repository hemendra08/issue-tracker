using System.Net;
using System.Text.Json;

namespace IssueTracker.Api.Middleware;

/// <summary>
/// Centralized exception handling middleware that logs errors and returns RFC 7807 problem details.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? context.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception occurred for path {Path}. RequestId: {RequestId}", 
            context.Request.Path, requestId);

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/problem+json";

        var statusCode = exception switch
        {
            BadHttpRequestException badHttpEx => badHttpEx.StatusCode,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new
        {
            type = statusCode == 400 
                ? "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                : "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = statusCode == 400 ? "Bad Request" : "Internal Server Error",
            status = statusCode,
            detail = statusCode == 400
                ? exception.Message
                : "An unexpected error occurred while processing your request. Please reference the request ID when reporting this issue.",
            instance = context.Request.Path.Value,
            requestId = requestId
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
