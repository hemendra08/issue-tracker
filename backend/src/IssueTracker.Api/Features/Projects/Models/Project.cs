namespace IssueTracker.Api.Features.Projects.Models;

public class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsArchived { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
