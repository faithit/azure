using CloudOps.Domain.Common;
using CloudOps.Domain.Enums;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Domain.Entities;

public sealed class ProjectTask : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public string? AssignedUserId { get; private set; }
    public string CreatedById { get; private set; } = null!;
    public DateTime? CompletedAtUtc { get; private set; }
    public Project Project { get; private set; } = null!;

    private ProjectTask() { }

    public ProjectTask(Guid projectId, string title, string? description, TaskPriority priority, DateTime? dueDateUtc, string? assignedUserId, string? createdById = null, TaskStatus status = TaskStatus.Todo)
    {
        ProjectId = projectId;
        CreatedById = createdById ?? string.Empty;
        Update(title, description, status, priority, dueDateUtc, assignedUserId, createdById);
    }

    public void Update(string title, string? description, TaskStatus status, TaskPriority priority, DateTime? dueDateUtc, string? assignedUserId, string? createdById = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Task title is required.", nameof(title));
        Title = title.Trim();
        Description = description?.Trim();
        Status = status;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        AssignedUserId = assignedUserId;
        if (!string.IsNullOrWhiteSpace(createdById)) CreatedById = createdById.Trim();
        if (status == TaskStatus.Completed || status == TaskStatus.Done)
        {
            CompletedAtUtc ??= DateTime.UtcNow;
        }
        else
        {
            CompletedAtUtc = null;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

