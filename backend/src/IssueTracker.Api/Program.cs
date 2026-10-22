using System.Text;
using IssueTracker.Api.Middleware;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Auth;
using IssueTracker.Api.Features.Auth.Models;
using IssueTracker.Api.Features.Auth.Services;
using IssueTracker.Api.Features.Projects;
using IssueTracker.Api.Features.Projects.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// 3. Identity & JWT Authentication Services
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? "IssueTracker.Api";
var audience = jwtSection["Audience"] ?? "IssueTracker.Client";
var signingKey = builder.Configuration["Jwt:SigningKey"] 
              ?? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");

if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
{
    throw new InvalidOperationException(
        "Configuration error: Jwt:SigningKey is required and must be at least 32 characters long. " +
        "Set it using 'dotnet user-secrets set \"Jwt:SigningKey\" \"<your-secret-key>\"' or the JWT_SIGNING_KEY environment variable.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Local HTTP development supported
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti))
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var isRevoked = await db.RevokedTokens.AnyAsync(r => r.TokenId == jti);
                    if (isRevoked)
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

// 5. CORS for Frontend
const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
            builder.Configuration["Frontend:Url"] ?? "http://localhost:5173",
            "http://localhost:5173",
            "http://127.0.0.1:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// 6. OpenAPI & Scalar Interactive Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Middlewares
app.UseRequestId();
app.UseGlobalExceptionHandler();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// 7. Interactive API Documentation (OpenAPI + Scalar)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Distributed Issue Tracker API")
               .WithTheme(ScalarTheme.Moon);
    });
}

// 8. Register Feature Endpoints
app.MapAuthEndpoints();
app.MapProjectEndpoints();

// 10. Startup Database Migration
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();

public partial class Program { }
