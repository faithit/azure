using CloudOps.Application.Abstractions;
using CloudOps.Application.Common;
using CloudOps.Domain.Entities;
using CloudOps.Domain.Enums;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;

namespace CloudOps.Application.Tasks;

public sealed class TaskService(ITaskRepository tasks, IProjectRepository projects, ICurrentUser currentUser) : ITaskService
{
    public async Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Task", id);
        var project = await FindProjectAsync(task.ProjectId, cancellationToken);
        EnsureCanView(project);
        return Map(task);
    }

    public async Task<PagedResult<TaskDto>> GetByProjectIdAsync(Guid projectId, TaskListQuery query, CancellationToken cancellationToken = default)
    {
        var project = await FindProjectAsync(projectId, cancellationToken);
        EnsureCanView(project);
        var page = await tasks.GetPageByProjectIdAsync(projectId, query.PageNumber, query.PageSize, query.Status, query.Priority, cancellationToken);
        return new PagedResult<TaskDto>(page.Items.Select(Map).ToList(), query.PageNumber, query.PageSize, page.TotalCount);
    }

    public async Task<TaskDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var project = await FindProjectAsync(projectId, cancellationToken);
        EnsureCanWriteTask(project);

        if (request.AssignedUserId is not null)
        {
            if (await projects.GetMemberAsync(projectId, request.AssignedUserId, cancellationToken) is null)
                throw new ArgumentException("Assigned user must be a project member.", nameof(request.AssignedUserId));
        }

        var task = new ProjectTask(
            projectId,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            request.AssignedUserId,
            currentUser.Id,
            request.Status);

        await tasks.AddAsync(task, cancellationToken);
        await tasks.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Task", id);
        var project = await FindProjectAsync(task.ProjectId, cancellationToken);

        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("ProjectManager") && task.AssignedUserId != currentUser.Id && !project.Members.Any(member => member.UserId == currentUser.Id))
        {
            throw new ForbiddenException();
        }

        if (request.AssignedUserId is not null && await projects.GetMemberAsync(task.ProjectId, request.AssignedUserId, cancellationToken) is null)
            throw new ArgumentException("Assigned user must be a project member.", nameof(request.AssignedUserId));

        task.Update(request.Title, request.Description, request.Status, request.Priority, request.DueDateUtc, request.AssignedUserId, task.CreatedById);
        await tasks.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Task", id);
        var project = await FindProjectAsync(task.ProjectId, cancellationToken);
        if (!currentUser.IsInRole("Admin") && !currentUser.IsInRole("ProjectManager") && task.AssignedUserId != currentUser.Id && !project.Members.Any(member => member.UserId == currentUser.Id))
            throw new ForbiddenException();
        tasks.Remove(task);
        await tasks.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> FindProjectAsync(Guid id, CancellationToken ct) => await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Project", id);

    private void EnsureCanView(Project project)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrEmpty(userId)) throw new UnauthorizedAccessException();
        if (currentUser.IsInRole("Admin") || project.ProjectManagerId == userId || project.CreatedById == userId || project.Members.Any(member => member.UserId == userId))
            return;
        throw new ForbiddenException();
    }

    private void EnsureCanWriteTask(Project project)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrEmpty(userId)) throw new UnauthorizedAccessException();
        if (currentUser.IsInRole("Admin") || currentUser.IsInRole("ProjectManager") && project.ProjectManagerId == userId || currentUser.IsInRole("Developer") || project.ProjectManagerId == userId || project.CreatedById == userId)
            return;
        throw new ForbiddenException();
    }

    private static TaskDto Map(ProjectTask task) => new()
    {
        Id = task.Id,
        ProjectId = task.ProjectId,
        Title = task.Title,
        Description = task.Description,
        Status = task.Status,
        Priority = task.Priority,
        DueDateUtc = task.DueDateUtc,
        AssignedUserId = task.AssignedUserId,
        CreatedById = task.CreatedById,
        CreatedAtUtc = task.CreatedAtUtc,
        UpdatedAtUtc = task.UpdatedAtUtc,
        CompletedAtUtc = task.CompletedAtUtc
    };
}
