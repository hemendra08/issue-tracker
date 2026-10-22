namespace IssueTracker.Api.Features.Projects.Dtos;

public record CreateProjectRequest(
    string Name,
    string Key,
    string? Description);

public record UpdateProjectRequest(
    string? Name,
    string? Description);

public record ProjectResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    Guid OwnerId,
    string? OwnerName,
    string? OwnerEmail,
    string CurrentUserRole,
    int MemberCount,
    bool IsArchived,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record PagedProjectsResponse(
    IReadOnlyList<ProjectResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
