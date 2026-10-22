namespace IssueTracker.Api.Features.Auth.Dtos;

public record RegisterRequest(
    string Email,
    string DisplayName,
    string Password);

public record LoginRequest(
    string Email,
    string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);

public record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAtUtc);
