using CloudOps.Application.Abstractions;
using CloudOps.Application.Common;
using CloudOps.Domain.Entities;

namespace CloudOps.Application.Projects;

public sealed class ProjectService(IProjectRepository projects, ICurrentUser currentUser) : IProjectService
{
    public async Task<PagedResult<ProjectDto>> GetAllAsync(ProjectListQuery query, CancellationToken cancellationToken = default)
    {
        var page = await projects.GetPageAsync(query.PageNumber, query.PageSize, cancellationToken);
        var items = page.Items.Where(CanViewProject).Select(Map).ToList();
        return new PagedResult<ProjectDto>(items, query.PageNumber, query.PageSize, items.Count);
    }

    public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(id, cancellationToken);
        EnsureCanView(project);
        return Map(project);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.Id ?? throw new ForbiddenException("An authenticated user is required.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Project name is required.", nameof(request.Name));

        if (!string.IsNullOrWhiteSpace(request.ProjectCode))
        {
            var existing = await projects.GetByCodeAsync(request.ProjectCode, cancellationToken);
            if (existing is not null) throw new ConflictException($"A project with code '{request.ProjectCode}' already exists.");
        }

        var project = new Project(
            request.Name,
            request.Description,
            userId,
            request.ProjectCode,
            request.Status,
            request.Priority,
            request.StartDateUtc,
            request.DueDateUtc,
            request.ProjectManagerId ?? userId);

        var member = new ProjectMember(project.Id, userId);
        project.AddMember(member);

        await projects.AddAsync(project, cancellationToken);
        await projects.AddMemberAsync(member, cancellationToken);
        await projects.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(id, cancellationToken);
        EnsureCanManage(project);

        if (!string.IsNullOrWhiteSpace(request.ProjectCode) && request.ProjectCode != project.ProjectCode)
        {
            var existing = await projects.GetByCodeAsync(request.ProjectCode, cancellationToken);
            if (existing is not null && existing.Id != project.Id) throw new ConflictException($"A project with code '{request.ProjectCode}' already exists.");
        }

        project.Update(
            request.Name,
            request.Description,
            request.ProjectCode,
            request.Status,
            request.Priority,
            request.StartDateUtc,
            request.DueDateUtc,
            request.ProjectManagerId ?? project.ProjectManagerId ?? project.CreatedById);

        await projects.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(id, cancellationToken);
        if (!currentUser.IsInRole("Admin")) throw new ForbiddenException("Only administrators can delete projects.");
        projects.Remove(project);
        await projects.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(projectId, cancellationToken);
        EnsureCanView(project);

        var members = await projects.GetMembersAsync(projectId, cancellationToken);
        return members.Select(MapMember).ToList();
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddProjectMemberDto request, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(projectId, cancellationToken);
        EnsureCanManage(project);

        if (string.IsNullOrWhiteSpace(request.UserId)) throw new ArgumentException("User is required.", nameof(request.UserId));
        if (await projects.GetMemberAsync(projectId, request.UserId, cancellationToken) is not null) throw new ConflictException("User is already a member of this project.");

        var member = new ProjectMember(projectId, request.UserId);
        await projects.AddMemberAsync(member, cancellationToken);
        await projects.SaveChangesAsync(cancellationToken);
        return MapMember(member);
    }

    public async Task RemoveMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default)
    {
        var project = await FindAsync(projectId, cancellationToken);
        EnsureCanManage(project);

        var member = await projects.GetMemberAsync(projectId, userId, cancellationToken) ?? throw new NotFoundException("Project member", projectId);
        if (member.UserId == project.ProjectManagerId) throw new ConflictException("Project manager cannot be removed without reassigning ownership.");
        if (member.UserId == project.CreatedById) throw new ConflictException("Project creator cannot be removed.");

        projects.RemoveMember(member);
        await projects.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) => await projects.GetByIdAsync(id, ct) ?? throw new NotFoundException("Project", id);

    private bool CanManageProject(Project project)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrEmpty(userId)) return false;
        if (currentUser.IsInRole("Admin")) return true;
        if (project.ProjectManagerId == userId) return true;
        if (project.CreatedById == userId && currentUser.IsInRole("ProjectManager")) return true;
        return false;
    }

    private bool CanViewProject(Project project)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrEmpty(userId)) return false;
        if (currentUser.IsInRole("Admin")) return true;
        if (project.ProjectManagerId == userId || project.CreatedById == userId) return true;
        return project.Members.Any(member => member.UserId == userId);
    }

    private void EnsureCanManage(Project project)
    {
        if (!CanManageProject(project)) throw new ForbiddenException();
    }

    private void EnsureCanView(Project project)
    {
        if (!CanViewProject(project)) throw new ForbiddenException();
    }

    private static ProjectDto Map(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        ProjectCode = project.ProjectCode,
        Description = project.Description,
        Status = project.Status,
        Priority = project.Priority,
        StartDateUtc = project.StartDateUtc,
        DueDateUtc = project.DueDateUtc,
        CreatedById = project.CreatedById,
        ProjectManagerId = project.ProjectManagerId,
        OwnerId = project.OwnerId,
        CreatedAtUtc = project.CreatedAtUtc,
        UpdatedAtUtc = project.UpdatedAtUtc,
        TaskCount = project.Tasks.Count,
        MemberCount = project.Members.Count
    };

    private static ProjectMemberDto MapMember(ProjectMember member) => new()
    {
        Id = member.Id,
        ProjectId = member.ProjectId,
        UserId = member.UserId,
        JoinedAtUtc = member.JoinedAtUtc
    };
}
