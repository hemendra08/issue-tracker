using IssueTracker.Api.Features.Auth.Endpoints;

namespace IssueTracker.Api.Features.Auth;

/// <summary>
/// Entry point that maps all decoupled Auth endpoints.
/// Each action (SignUp, Login, Logout, Profile, Users) is isolated in its own file.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Primary Auth group (/api/v1/auth)
        var primaryGroup = app.MapGroup("/api/v1/auth").WithTags("Auth");
        SignUpEndpoint.Map(primaryGroup, isPrimary: true);
        LoginEndpoint.Map(primaryGroup, isPrimary: true);
        LogoutEndpoint.Map(primaryGroup, isPrimary: true);
        UserProfileEndpoint.Map(primaryGroup, isPrimary: true);

        // 2. Backward-compatible alias group (/api/v1/identity)
        var aliasGroup = app.MapGroup("/api/v1/identity").WithTags("Auth").ExcludeFromDescription();
        SignUpEndpoint.Map(aliasGroup, isPrimary: false);
        LoginEndpoint.Map(aliasGroup, isPrimary: false);
        LogoutEndpoint.Map(aliasGroup, isPrimary: false);
        UserProfileEndpoint.Map(aliasGroup, isPrimary: false);

        // 3. Inter-service HTTP endpoints for other microservices (/api/v1/users)
        InternalUsersEndpoint.Map(app);

        return app;
    }

    [Obsolete("Use MapAuthEndpoints instead.")]
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app) => app.MapAuthEndpoints();
}
