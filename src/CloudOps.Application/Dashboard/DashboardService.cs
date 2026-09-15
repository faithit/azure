using CloudOps.Application.Abstractions;
using CloudOps.Domain.Enums;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Application.Dashboard;

public sealed class DashboardService(IProjectRepository projects, ITaskRepository tasks) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var allTasks = await tasks.GetAllAsync(cancellationToken);
        var statuses = Enum.GetValues<TaskStatus>()
            .DistinctBy(status => (int)status)
            .ToDictionary(status => status, status => allTasks.Count(task => task.Status == status));
        var now = DateTime.UtcNow;
        return new DashboardDto(await projects.CountAsync(cancellationToken), allTasks.Count, statuses[TaskStatus.Done], allTasks.Count(task => task.DueDateUtc < now && task.Status != TaskStatus.Done), statuses);
    }
}
