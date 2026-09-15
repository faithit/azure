using CloudOps.Application.Abstractions;
using CloudOps.Application.AI;
using CloudOps.Application.Common;
using CloudOps.Domain.Entities;
using CloudOps.Domain.Enums;
using Xunit;

namespace CloudOps.Application.Tests;

public sealed class AiServiceTests
{
    [Fact]
    public async Task AskAsync_SendsOnlyProjectsVisibleToTheCurrentUser()
    {
        var repository = new InMemoryProjectRepository();
        var visible = new Project("Visible project", "A project the user belongs to.", "owner-1");
        visible.AddMember(new ProjectMember(visible.Id, "member-1"));
        visible.Tasks.Add(new ProjectTask(visible.Id, "Visible task", null, TaskPriority.High, DateTime.UtcNow.AddDays(-1), "member-1"));

        var privateProject = new Project("Private project", "This must never leave the service.", "owner-2");
        privateProject.Tasks.Add(new ProjectTask(privateProject.Id, "Private task", null, TaskPriority.Critical, DateTime.UtcNow, "owner-2"));
        repository.Projects.AddRange([visible, privateProject]);

        var provider = new RecordingAiProvider();
        var service = new AiService(repository, provider, new TestCurrentUser("member-1"));

        var response = await service.AskAsync(new AiChatRequest { Question = "What is overdue?" });

        Assert.Single(response.Sources);
        Assert.Equal(visible.Id, response.Sources[0].ProjectId);
        Assert.Contains("Visible project", provider.LastPrompt!.UserMessage);
        Assert.DoesNotContain("Private project", provider.LastPrompt.UserMessage);
        Assert.DoesNotContain("Private task", provider.LastPrompt.UserMessage);
    }

    [Fact]
    public async Task AskAsync_RejectsAProjectTheCurrentUserCannotView()
    {
        var repository = new InMemoryProjectRepository();
        var privateProject = new Project("Private project", null, "owner-2");
        repository.Projects.Add(privateProject);
        var service = new AiService(repository, new RecordingAiProvider(), new TestCurrentUser("member-1"));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AskAsync(new AiChatRequest { Question = "Summarize this project", ProjectId = privateProject.Id }));
    }

    private sealed class TestCurrentUser(string? id, params string[] roles) : ICurrentUser
    {
        public string? Id { get; } = id;
        public bool IsInRole(string role) => roles.Contains(role);
    }

    private sealed class RecordingAiProvider : IAiProvider
    {
        public string Name => "Test provider";
        public AiPrompt? LastPrompt { get; private set; }
        public Task<string> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            return Task.FromResult("Test answer");
        }
    }

    private sealed class InMemoryProjectRepository : IProjectRepository
    {
        public List<Project> Projects { get; } = [];
        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Projects.SingleOrDefault(project => project.Id == id));
        public Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default) => Task.FromResult(Projects.SingleOrDefault(project => project.ProjectCode == projectCode));
        public Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<Project>, int)>((Projects.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), Projects.Count));
        public Task<IReadOnlyList<ProjectMember>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProjectMember>>([]);
        public Task<ProjectMember?> GetMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default) => Task.FromResult<ProjectMember?>(null);
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Projects.Count);
        public Task AddAsync(Project project, CancellationToken cancellationToken = default) { Projects.Add(project); return Task.CompletedTask; }
        public Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(Project project) => Projects.Remove(project);
        public void RemoveMember(ProjectMember member) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
