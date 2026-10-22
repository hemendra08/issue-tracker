namespace IssueTracker.Api.Features.Projects.Dtos;

public record AddMemberRequest(
    string Email,
    string Role);

public record UpdateMemberRoleRequest(
    string Role);

public record ProjectMemberResponse(
    Guid ProjectId,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    DateTime JoinedAtUtc);
