using CloudOps.Domain.Enums;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Application.Dashboard;

public sealed record DashboardDto(int ProjectCount, int TaskCount, int CompletedTaskCount, int OverdueTaskCount, IReadOnlyDictionary<TaskStatus, int> TasksByStatus);

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
