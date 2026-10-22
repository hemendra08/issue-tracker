using System.Security.Claims;
using System.Text.RegularExpressions;
using IssueTracker.Api.Data;
using IssueTracker.Api.Features.Projects.Dtos;
using IssueTracker.Api.Features.Projects.Models;
using IssueTracker.Api.Features.Projects.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Api.Features.Projects;

public static class ProjectEndpoints
{
    private static readonly Regex ProjectKeyRegex = new("^[A-Z0-9]{2,10}$", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        // 1. Create Project
        group.MapPost("/", async (
            [FromBody] CreateProjectRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var errors = ValidateCreateProjectRequest(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var normalizedKey = request.Key.Trim().ToUpperInvariant();

            // Check duplicate project key
            var keyExists = await db.Projects.AnyAsync(p => p.Key == normalizedKey, cancellationToken);
            if (keyExists)
            {
                return Results.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = $"A project with key '{normalizedKey}' already exists."
                });
            }

            var now = DateTime.UtcNow;
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Key = normalizedKey,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OwnerId = currentUserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsArchived = false
            };

            var ownerMembership = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = currentUserId,
                Role = ProjectRole.Owner,
                JoinedAtUtc = now
            };

            project.Members.Add(ownerMembership);
            db.Projects.Add(project);

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
                    Detail = $"A project with key '{normalizedKey}' already exists."
                });
            }

            var ownerInfo = await userDirectory.FindUserByIdAsync(currentUserId, cancellationToken);

            var response = new ProjectResponse(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.OwnerId,
                ownerInfo?.DisplayName,
                ownerInfo?.Email,
                ProjectRole.Owner.ToString(),
                1,
                project.IsArchived,
                project.CreatedAtUtc,
                project.UpdatedAtUtc);

            return Results.Created($"/api/v1/projects/{project.Id}", response);
        })
        .WithName("CreateProject")
        .WithSummary("Creates a new project and designates the creator as Owner");

        // 2. List Projects for Current User
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] bool? includeArchived,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var actualPage = page.GetValueOrDefault(1);
            if (actualPage < 1) actualPage = 1;
            var actualPageSize = pageSize.GetValueOrDefault(20);
            if (actualPageSize < 1 || actualPageSize > 100) actualPageSize = 20;

            // Only projects where current user is a member
            var query = db.Projects
                .AsNoTracking()
                .Where(p => p.Members.Any(m => m.UserId == currentUserId));

            if (includeArchived.HasValue && !includeArchived.Value)
            {
                query = query.Where(p => !p.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Key.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var projects = await query
                .OrderByDescending(p => p.UpdatedAtUtc)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Key,
                    p.Description,
                    p.OwnerId,
                    CurrentUserRole = p.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).FirstOrDefault(),
                    MemberCount = p.Members.Count(),
                    p.IsArchived,
                    p.CreatedAtUtc,
                    p.UpdatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var ownerIds = projects.Select(p => p.OwnerId).Distinct();
            var owners = await userDirectory.GetUsersByIdsAsync(ownerIds, cancellationToken);

            var items = projects.Select(p =>
            {
                owners.TryGetValue(p.OwnerId, out var owner);
                return new ProjectResponse(
                    p.Id,
                    p.Name,
                    p.Key,
                    p.Description,
                    p.OwnerId,
                    owner?.DisplayName,
                    owner?.Email,
                    p.CurrentUserRole.ToString(),
                    p.MemberCount,
                    p.IsArchived,
                    p.CreatedAtUtc,
                    p.UpdatedAtUtc);
            }).ToList();

            return Results.Ok(new PagedProjectsResponse(items, totalCount, actualPage, actualPageSize));
        })
        .WithName("ListProjects")
        .WithSummary("Lists projects where the authenticated user is an active member");

        // 3. Get Project Detail
        group.MapGet("/{projectId:guid}", async (
            Guid projectId,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .AsNoTracking()
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            // Hide private project existence: return 404 if not found OR user is not a member
            var memberRecord = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || memberRecord is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            var owner = await userDirectory.FindUserByIdAsync(project.OwnerId, cancellationToken);

            return Results.Ok(new ProjectResponse(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.OwnerId,
                owner?.DisplayName,
                owner?.Email,
                memberRecord.Role.ToString(),
                project.Members.Count,
                project.IsArchived,
                project.CreatedAtUtc,
                project.UpdatedAtUtc));
        })
        .WithName("GetProjectById")
        .WithSummary("Retrieves detailed project metadata for authorized members");

        // 4. Update Project Details
        group.MapPatch("/{projectId:guid}", async (
            Guid projectId,
            [FromBody] UpdateProjectRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var memberRecord = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || memberRecord is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            // Only Owner and Admin can edit
            if (memberRecord.Role != ProjectRole.Owner && memberRecord.Role != ProjectRole.Admin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only project owners and admins can update project details.");
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Archived projects cannot be edited. Restore the project first.");
            }

            var errors = ValidateUpdateProjectRequest(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            if (request.Name is not null)
            {
                project.Name = request.Name.Trim();
            }

            if (request.Description is not null)
            {
                project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var owner = await userDirectory.FindUserByIdAsync(project.OwnerId, cancellationToken);

            return Results.Ok(new ProjectResponse(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.OwnerId,
                owner?.DisplayName,
                owner?.Email,
                memberRecord.Role.ToString(),
                project.Members.Count,
                project.IsArchived,
                project.CreatedAtUtc,
                project.UpdatedAtUtc));
        })
        .WithName("UpdateProject")
        .WithSummary("Updates project name or description (Owner or Admin required)");

        // 5. Archive Project
        group.MapPost("/{projectId:guid}/archive", async (
            Guid projectId,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var memberRecord = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || memberRecord is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            // Only Owner can archive
            if (memberRecord.Role != ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only the project owner can archive this project.");
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Project is already archived.");
            }

            project.IsArchived = true;
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var owner = await userDirectory.FindUserByIdAsync(project.OwnerId, cancellationToken);

            return Results.Ok(new ProjectResponse(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.OwnerId,
                owner?.DisplayName,
                owner?.Email,
                memberRecord.Role.ToString(),
                project.Members.Count,
                project.IsArchived,
                project.CreatedAtUtc,
                project.UpdatedAtUtc));
        })
        .WithName("ArchiveProject")
        .WithSummary("Archives a project (Owner only)");

        // 6. Restore Project
        group.MapPost("/{projectId:guid}/restore", async (
            Guid projectId,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var memberRecord = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || memberRecord is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            // Only Owner can restore
            if (memberRecord.Role != ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only the project owner can restore this project.");
            }

            if (!project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Project is not archived.");
            }

            project.IsArchived = false;
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var owner = await userDirectory.FindUserByIdAsync(project.OwnerId, cancellationToken);

            return Results.Ok(new ProjectResponse(
                project.Id,
                project.Name,
                project.Key,
                project.Description,
                project.OwnerId,
                owner?.DisplayName,
                owner?.Email,
                memberRecord.Role.ToString(),
                project.Members.Count,
                project.IsArchived,
                project.CreatedAtUtc,
                project.UpdatedAtUtc));
        })
        .WithName("RestoreProject")
        .WithSummary("Restores an archived project (Owner only)");

        // ==========================================
        // MEMBERSHIP ENDPOINTS
        // ==========================================

        // 7. List Members
        group.MapGet("/{projectId:guid}/members", async (
            Guid projectId,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .AsNoTracking()
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            if (project is null || !project.Members.Any(m => m.UserId == currentUserId))
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            var userIds = project.Members.Select(m => m.UserId);
            var usersMap = await userDirectory.GetUsersByIdsAsync(userIds, cancellationToken);

            var memberResponses = project.Members
                .OrderBy(m => m.Role)
                .ThenBy(m => m.JoinedAtUtc)
                .Select(m =>
                {
                    usersMap.TryGetValue(m.UserId, out var userInfo);
                    return new ProjectMemberResponse(
                        m.ProjectId,
                        m.UserId,
                        userInfo?.Email ?? "unknown@example.com",
                        userInfo?.DisplayName ?? "Unknown User",
                        m.Role.ToString(),
                        m.JoinedAtUtc);
                })
                .ToList();

            return Results.Ok(memberResponses);
        })
        .WithName("ListProjectMembers")
        .WithSummary("Lists all members belonging to a project");

        // 8. Add Member
        group.MapPost("/{projectId:guid}/members", async (
            Guid projectId,
            [FromBody] AddMemberRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var actingMember = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || actingMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Cannot add members to an archived project.");
            }

            // Only Owner and Admin can add members
            if (actingMember.Role != ProjectRole.Owner && actingMember.Role != ProjectRole.Admin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only project owners and admins can add members.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Email"] = ["Member email is required."]
                });
            }

            if (!Enum.TryParse<ProjectRole>(request.Role, true, out var requestedRole) || requestedRole == ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Invalid role. Members can only be added with 'Admin' or 'Member' roles.");
            }

            // Admin cannot add another Admin
            if (actingMember.Role == ProjectRole.Admin && requestedRole == ProjectRole.Admin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Project admins cannot add other admins. Only the owner can assign the Admin role.");
            }

            // Resolve target user from Auth
            var targetUser = await userDirectory.FindUserByEmailAsync(request.Email.Trim(), cancellationToken);
            if (targetUser is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "No registered user found with the provided email address."
                });
            }

            // Check if already member
            if (project.Members.Any(m => m.UserId == targetUser.Id))
            {
                return Results.Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = "User is already a member of this project."
                });
            }

            var newMember = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = targetUser.Id,
                Role = requestedRole,
                JoinedAtUtc = DateTime.UtcNow
            };

            project.Members.Add(newMember);
            project.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            var response = new ProjectMemberResponse(
                project.Id,
                targetUser.Id,
                targetUser.Email,
                targetUser.DisplayName,
                requestedRole.ToString(),
                newMember.JoinedAtUtc);

            return Results.Created($"/api/v1/projects/{project.Id}/members/{targetUser.Id}", response);
        })
        .WithName("AddProjectMember")
        .WithSummary("Adds an existing user to the project as an Admin or Member");

        // 9. Update Member Role
        group.MapPatch("/{projectId:guid}/members/{userId:guid}", async (
            Guid projectId,
            Guid userId,
            [FromBody] UpdateMemberRoleRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            IUserDirectoryService userDirectory,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var actingMember = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || actingMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Cannot update member roles in an archived project.");
            }

            var targetMember = project.Members.FirstOrDefault(m => m.UserId == userId);
            if (targetMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Member was not found in this project."
                });
            }

            // Cannot modify Owner role
            if (targetMember.Role == ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "The project owner role cannot be modified.");
            }

            if (!Enum.TryParse<ProjectRole>(request.Role, true, out var newRole) || newRole == ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Invalid role. Role must be 'Admin' or 'Member'.");
            }

            // Authorization:
            // Members cannot modify roles
            if (actingMember.Role == ProjectRole.Member)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Members are not authorized to modify member roles.");
            }

            // Admin cannot promote to Admin or demote an Admin
            if (actingMember.Role == ProjectRole.Admin)
            {
                if (targetMember.Role == ProjectRole.Admin || newRole == ProjectRole.Admin)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Forbidden",
                        detail: "Only the project owner can promote members to Admin or demote Admins.");
                }
            }

            targetMember.Role = newRole;
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var targetUser = await userDirectory.FindUserByIdAsync(userId, cancellationToken);

            return Results.Ok(new ProjectMemberResponse(
                project.Id,
                userId,
                targetUser?.Email ?? "unknown@example.com",
                targetUser?.DisplayName ?? "Unknown User",
                newRole.ToString(),
                targetMember.JoinedAtUtc));
        })
        .WithName("UpdateProjectMemberRole")
        .WithSummary("Updates the role of a project member (Owner/Admin permissions enforced)");

        // 10. Remove Member
        group.MapDelete("/{projectId:guid}/members/{userId:guid}", async (
            Guid projectId,
            Guid userId,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var actingMember = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || actingMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Cannot remove members from an archived project.");
            }

            var targetMember = project.Members.FirstOrDefault(m => m.UserId == userId);
            if (targetMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Member was not found in this project."
                });
            }

            // Owner cannot be removed
            if (targetMember.Role == ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "The project owner cannot be removed.");
            }

            // Members cannot remove anyone
            if (actingMember.Role == ProjectRole.Member)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Ordinary members are not authorized to remove other members.");
            }

            // Admin cannot remove other Admins
            if (actingMember.Role == ProjectRole.Admin && targetMember.Role == ProjectRole.Admin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Admins cannot remove other admins. Only the project owner can remove an admin.");
            }

            db.ProjectMembers.Remove(targetMember);
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("RemoveProjectMember")
        .WithSummary("Removes a member from the project");

        // 11. Leave Project
        group.MapPost("/{projectId:guid}/leave", async (
            Guid projectId,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var currentUserId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

            var actingMember = project?.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (project is null || actingMember is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Project was not found."
                });
            }

            if (project.IsArchived)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Cannot leave an archived project.");
            }

            // Owner cannot leave
            if (actingMember.Role == ProjectRole.Owner)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "The project owner cannot leave the project.");
            }

            db.ProjectMembers.Remove(actingMember);
            project.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("LeaveProject")
        .WithSummary("Voluntarily leaves a project (Owner cannot leave)");

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? principal.FindFirstValue("sub");

        return Guid.TryParse(idClaim, out userId);
    }

    private static Dictionary<string, string[]> ValidateCreateProjectRequest(CreateProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["Name"] = ["Project name is required."];
        }
        else if (request.Name.Trim().Length > 120)
        {
            errors["Name"] = ["Project name must not exceed 120 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            errors["Key"] = ["Project key is required."];
        }
        else
        {
            var key = request.Key.Trim().ToUpperInvariant();
            if (key.Length < 2 || key.Length > 10)
            {
                errors["Key"] = ["Project key must be between 2 and 10 characters."];
            }
            else if (!ProjectKeyRegex.IsMatch(key))
            {
                errors["Key"] = ["Project key must contain only uppercase letters and numbers."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 2000)
        {
            errors["Description"] = ["Project description must not exceed 2000 characters."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateProjectRequest(UpdateProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors["Name"] = ["Project name cannot be empty."];
            }
            else if (request.Name.Trim().Length > 120)
            {
                errors["Name"] = ["Project name must not exceed 120 characters."];
            }
        }

        if (request.Description is not null && request.Description.Trim().Length > 2000)
        {
            errors["Description"] = ["Project description must not exceed 2000 characters."];
        }

        return errors;
    }
}
