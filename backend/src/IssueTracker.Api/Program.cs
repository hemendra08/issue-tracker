using System.Text.Json;
using IssueTracker.Api.Common.Middleware;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Identity;
using IssueTracker.Api.Features.Issues;
using IssueTracker.Api.Features.Projects;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Structured Logging
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

// 2. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required but missing.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "dit_db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

// 4. OpenAPI & Scalar Interactive Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// 5. Middlewares
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// 6. Interactive API Documentation (OpenAPI + Scalar)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Distributed Issue Tracker API")
               .WithTheme(ScalarTheme.Moon);
    });
}

// 7. Liveness & Readiness Probes
app.MapGet("/health/live", () => Results.Ok(new
{
    status = "Healthy",
    service = "IssueTracker.Api",
    timestamp = DateTime.UtcNow
}))
.WithTags("Health");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            service = "IssueTracker.Api",
            timestamp = DateTime.UtcNow,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        };
        var statusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        }));
    }
})
.WithTags("Health");

// 8. Register Feature Endpoints
app.MapIdentityEndpoints();
app.MapProjectEndpoints();
app.MapIssueEndpoints();

// 9. Startup Database Migration
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program { }
