using System.ComponentModel.DataAnnotations;
using CloudOps.Application.Common;
using CloudOps.Domain.Enums;

namespace CloudOps.Application.Projects;

public class CreateProjectRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    [StringLength(30)] public string ProjectCode { get; init; } = string.Empty;
    [StringLength(2000)] public string? Description { get; init; }
    public ProjectStatus Status { get; init; } = ProjectStatus.Planning;
    public ProjectPriority Priority { get; init; } = ProjectPriority.Medium;
    public DateTime? StartDateUtc { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public string? ProjectManagerId { get; init; }
}

public sealed class UpdateProjectRequest : CreateProjectRequest;

public sealed class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; }
    public ProjectPriority Priority { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string? ProjectManagerId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int TaskCount { get; set; }
    public int MemberCount { get; set; }
}

public sealed class ProjectMemberDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
}

public sealed class AddProjectMemberDto
{
    [Required] public string UserId { get; set; } = string.Empty;
}

public sealed class ProjectListQuery : PaginationQuery;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> GetAllAsync(ProjectListQuery query, CancellationToken cancellationToken = default);
    Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
}
