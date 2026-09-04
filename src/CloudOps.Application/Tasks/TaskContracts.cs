using System.ComponentModel.DataAnnotations;
using CloudOps.Application.Common;
using CloudOps.Domain.Enums;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Application.Tasks;

public class CreateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 2)] public string Title { get; init; } = string.Empty;
    [StringLength(5000)] public string? Description { get; init; }
    public TaskStatus Status { get; init; } = TaskStatus.Todo;
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
    public DateTime? DueDateUtc { get; init; }
    public string? AssignedUserId { get; init; }
    public string? CreatedById { get; init; }
}

public sealed class UpdateTaskRequest : CreateTaskRequest
{
}

public sealed class TaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string? AssignedUserId { get; set; }
    public string? CreatedById { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class TaskListQuery : PaginationQuery
{
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
}

public interface ITaskService
{
    Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskDto>> GetByProjectIdAsync(Guid projectId, TaskListQuery query, CancellationToken cancellationToken = default);
    Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
