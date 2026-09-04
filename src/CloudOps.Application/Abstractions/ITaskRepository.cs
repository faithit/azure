using CloudOps.Domain.Enums;
using CloudOps.Domain.Entities;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Application.Abstractions;

public interface ITaskRepository
{
    Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectIdAsync(Guid projectId, int pageNumber, int pageSize, TaskStatus? status, TaskPriority? priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTask>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ProjectTask task, CancellationToken cancellationToken = default);
    void Remove(ProjectTask task);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectTask>> GetTasksByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
