namespace IssueTracker.Api.Middleware;

/// <summary>
/// Extracts or generates a Request ID (correlation ID), propagates it to response headers,
/// and enriches the logging scope.
/// </summary>
public class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-ID";
    public const string AltHeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string requestId;
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            requestId = headerVal.ToString();
        }
        else if (context.Request.Headers.TryGetValue(AltHeaderName, out var altVal) && !string.IsNullOrWhiteSpace(altVal))
        {
            requestId = altVal.ToString();
        }
        else
        {
            requestId = Guid.NewGuid().ToString("D");
        }

        context.Response.Headers[HeaderName] = requestId;
        context.Items["RequestId"] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["Service"] = "IssueTracker.Api"
        }))
        {
            await _next(context);
        }
    }
}
