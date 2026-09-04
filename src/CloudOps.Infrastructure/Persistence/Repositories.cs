using CloudOps.Application.Abstractions;
using CloudOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatus = CloudOps.Domain.Enums.TaskStatus;
using TaskPriority = CloudOps.Domain.Enums.TaskPriority;

namespace CloudOps.Infrastructure.Persistence;

public sealed class ProjectRepository(ApplicationDbContext context) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => context.Projects.Include(project => project.Tasks).Include(project => project.Members).SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
    public Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default) => context.Projects.Include(project => project.Tasks).Include(project => project.Members).SingleOrDefaultAsync(project => project.ProjectCode == projectCode, cancellationToken);
    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Projects.Include(project => project.Tasks).Include(project => project.Members).OrderByDescending(project => project.CreatedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
    public async Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) => await context.ProjectMembers.Where(member => member.ProjectId == projectId).OrderBy(member => member.JoinedAtUtc).ToListAsync(cancellationToken);
    public Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) => context.ProjectMembers.SingleOrDefaultAsync(member => member.ProjectId == projectId && member.UserId == userId, cancellationToken);
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => context.Projects.CountAsync(cancellationToken);
    public Task AddAsync(Project project, CancellationToken cancellationToken = default) => context.Projects.AddAsync(project, cancellationToken).AsTask();
    public Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken = default) => context.ProjectMembers.AddAsync(member, cancellationToken).AsTask();
    public void Remove(Project project) => context.Projects.Remove(project);
    public void RemoveMember(ProjectMember member) => context.ProjectMembers.Remove(member);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}

public sealed class TaskRepository(ApplicationDbContext context) : ITaskRepository
{
    public Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => context.Tasks.SingleOrDefaultAsync(task => task.Id == id, cancellationToken);
    public async Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectIdAsync(Guid projectId, int pageNumber, int pageSize, TaskStatus? status, TaskPriority? priority, CancellationToken cancellationToken = default)
    {
        var query = context.Tasks.Where(task => task.ProjectId == projectId);
        if (status is not null) query = query.Where(task => task.Status == status);
        if (priority is not null) query = query.Where(task => task.Priority == priority);
        query = query.OrderBy(task => task.Status).ThenBy(task => task.DueDateUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }
    public async Task<IReadOnlyList<ProjectTask>> GetAllAsync(CancellationToken cancellationToken = default) => await context.Tasks.AsNoTracking().ToListAsync(cancellationToken);
    public Task AddAsync(ProjectTask task, CancellationToken cancellationToken = default) => context.Tasks.AddAsync(task, cancellationToken).AsTask();
    public void Remove(ProjectTask task) => context.Tasks.Remove(task);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
    public async Task<IReadOnlyList<ProjectTask>> GetTasksByProjectAsync(Guid projectId, CancellationToken cancellationToken = default) => await context.Tasks.Where(task => task.ProjectId == projectId).OrderBy(task => task.DueDateUtc).ToListAsync(cancellationToken);
}
