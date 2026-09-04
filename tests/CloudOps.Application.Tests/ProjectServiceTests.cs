using CloudOps.Application.Abstractions;
using CloudOps.Application.Common;
using CloudOps.Application.Projects;
using CloudOps.Domain.Entities;
using Xunit;

namespace CloudOps.Application.Tests;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_AssignsCurrentUserAsOwner()
    {
        var repository = new InMemoryProjectRepository();
        var service = new ProjectService(repository, new TestCurrentUser("owner-1"));

        var result = await service.CreateAsync(new CreateProjectRequest { Name = "Platform modernization", Description = "Migration work" });

        Assert.Equal("owner-1", result.OwnerId);
        Assert.Equal("Platform modernization", result.Name);
        Assert.Single(repository.Projects);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenUserIsNotOwnerOrManager()
    {
        var repository = new InMemoryProjectRepository();
        var project = new Project("Protected", null, "owner-1");
        await repository.AddAsync(project);
        var service = new ProjectService(repository, new TestCurrentUser("other-user"));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(project.Id, new UpdateProjectRequest { Name = "Changed" }));
    }

    private sealed class TestCurrentUser(string? id, params string[] roles) : ICurrentUser
    {
        public string? Id { get; } = id;
        public bool IsInRole(string role) => roles.Contains(role);
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        public List<Project> Projects { get; } = [];
        public List<ProjectMember> Members { get; } = [];
        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Projects.SingleOrDefault(project => project.Id == id));
        public Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default) => Task.FromResult(Projects.SingleOrDefault(project => project.ProjectCode == projectCode));
        public Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Project>, int)>((Projects.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), Projects.Count));
        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProjectMember>>(Members.Where(member => member.ProjectId == projectId).ToList());
        public Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) => Task.FromResult(Members.SingleOrDefault(member => member.ProjectId == projectId && member.UserId == userId));
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Projects.Count);
        public Task AddAsync(Project project, CancellationToken cancellationToken = default) { Projects.Add(project); return Task.CompletedTask; }
        public Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken = default) { Members.Add(member); return Task.CompletedTask; }
        public void Remove(Project project) => Projects.Remove(project);
        public void RemoveMember(ProjectMember member) => Members.Remove(member);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
