using CloudOps.Domain.Entities;

namespace CloudOps.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken = default);
    void Remove(Project project);
    void RemoveMember(ProjectMember member);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
