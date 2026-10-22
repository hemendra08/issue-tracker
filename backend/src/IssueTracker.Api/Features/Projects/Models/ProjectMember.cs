namespace IssueTracker.Api.Features.Projects.Models;

public class ProjectMember
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public ProjectRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public Project? Project { get; set; }
}
