using System.ComponentModel.DataAnnotations;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Auth.Dtos;
using IssueTracker.Api.Features.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Auth.Endpoints;

public static class SignUpEndpoint
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    public static void Map(RouteGroupBuilder group, bool isPrimary = true)
    {
        // Primary registration / signup route
        var register = group.MapPost("/register", HandleSignUpAsync);
        if (isPrimary)
        {
            register.WithName("RegisterUser")
                .WithSummary("Registers a new user account with linked identity, identifier, and credential");
        }

        // Alias for /signup
        var signup = group.MapPost("/signup", HandleSignUpAsync);
        if (isPrimary)
        {
            signup.WithName("SignUpUser")
                .WithSummary("Alias for registering a new user account")
                .ExcludeFromDescription();
        }
    }

    private static async Task<IResult> HandleSignUpAsync(
        [FromBody] RegisterRequest request,
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var email = request.Email.Trim();

        // 1. Check duplicate email in CredentialIdentifiers
        var exists = await db.CredentialIdentifiers.AnyAsync(
            i => i.Type == "Email" && i.Value.ToLower() == email.ToLower(),
            cancellationToken);

        if (exists)
        {
            return Results.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "A user with this email address already exists."
            });
        }

        var now = DateTime.UtcNow;

        // 2. Resolve CredentialType for Password (Id: 1)
        var passwordCredentialType = await db.CredentialTypes
            .FirstOrDefaultAsync(ct => ct.Code == "Password", cancellationToken);
        var credentialTypeId = passwordCredentialType?.Id ?? 1;

        // 3. Create Identity entity (Table: Identities)
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.DisplayName.Trim(),
            Status = "Active",
            CreatedAtUtc = now
        };

        // 4. Create Credential entity (Table: Credentials)
        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            IdentityId = user.Id,
            CredentialTypeId = credentialTypeId,
            CredentialData = passwordHasher.HashPassword(user, request.Password),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Identity = user
        };

        // 5. Create CredentialIdentifier entity (Table: CredentialIdentifiers)
        var identifier = new CredentialIdentifier
        {
            Id = Guid.NewGuid(),
            IdentityId = user.Id,
            CredentialId = credential.Id,
            Type = "Email",
            Value = email,
            IsPrimary = true,
            CreatedAtUtc = now,
            Identity = user,
            Credential = credential
        };

        user.Credentials.Add(credential);
        user.Identifiers.Add(identifier);

        db.Users.Add(user);
        db.Credentials.Add(credential);
        db.CredentialIdentifiers.Add(identifier);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "A user with this email address already exists."
            });
        }

        var response = new UserResponse(
            user.Id,
            identifier.Value,
            user.FullName,
            user.CreatedAtUtc);

        return Results.Created("/api/v1/auth/me", response);
    }

    private static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["Email"] = ["Email is required."];
        }
        else if (request.Email.Length > 256)
        {
            errors["Email"] = ["Email must not exceed 256 characters."];
        }
        else if (!EmailValidator.IsValid(request.Email.Trim()))
        {
            errors["Email"] = ["Invalid email address format."];
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["DisplayName"] = ["Display name is required."];
        }
        else if (request.DisplayName.Trim().Length > 100)
        {
            errors["DisplayName"] = ["Display name must not exceed 100 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["Password"] = ["Password is required."];
        }
        else if (request.Password.Length < 8)
        {
            errors["Password"] = ["Password must be at least 8 characters long."];
        }
        else if (request.Password.Length > 128)
        {
            errors["Password"] = ["Password must not exceed 128 characters."];
        }

        return errors;
    }
}
