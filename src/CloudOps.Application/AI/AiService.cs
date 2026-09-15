using System.Text;
using CloudOps.Application.Abstractions;
using CloudOps.Application.Common;
using CloudOps.Domain.Entities;

namespace CloudOps.Application.AI;

public sealed class AiService(IProjectRepository projects, IAiProvider provider, ICurrentUser currentUser) : IAiService
{
    private const int ProjectPageSize = 100;

    public async Task<AiChatResponse> AskAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var selectedProjects = request.ProjectId is { } projectId
            ? await GetAuthorizedProjectAsync(projectId, cancellationToken)
            : await GetAuthorizedProjectsAsync(cancellationToken);

        var context = BuildContext(selectedProjects);
        var answer = await provider.CompleteAsync(new AiPrompt(
            "You are the CloudOps delivery assistant. Answer only from the project and task context provided. " +
            "Be concise, concrete, and transparent when the context does not contain enough information. " +
            "For risks or suggestions, label them as recommendations rather than facts.",
            $"Current UTC time: {DateTime.UtcNow:O}\n\nProject and task context:\n{context}\n\nUser question: {request.Question.Trim()}"), cancellationToken);

        return new AiChatResponse(answer, provider.Name, DateTime.UtcNow, selectedProjects
            .Select(project => new AiSourceDto(project.Id, project.Name, project.Tasks.Count)).ToList());
    }

    private async Task<IReadOnlyList<Project>> GetAuthorizedProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(projectId, cancellationToken) ?? throw new NotFoundException("Project", projectId);
        EnsureCanView(project);
        return [project];
    }

    private async Task<IReadOnlyList<Project>> GetAuthorizedProjectsAsync(CancellationToken cancellationToken)
    {
        var visibleProjects = new List<Project>();
        var pageNumber = 1;
        var totalCount = 0;

        do
        {
            var page = await projects.GetPageAsync(pageNumber, ProjectPageSize, cancellationToken);
            totalCount = page.TotalCount;
            visibleProjects.AddRange(page.Items.Where(CanViewProject));
            pageNumber++;
        } while ((pageNumber - 1) * ProjectPageSize < totalCount);

        return visibleProjects;
    }

    private bool CanViewProject(Project project)
    {
        var userId = currentUser.Id;
        return !string.IsNullOrEmpty(userId) &&
               (currentUser.IsInRole("Admin") || project.ProjectManagerId == userId || project.CreatedById == userId || project.Members.Any(member => member.UserId == userId));
    }

    private void EnsureCanView(Project project)
    {
        if (!CanViewProject(project)) throw new ForbiddenException();
    }

    private static string BuildContext(IEnumerable<Project> projects)
    {
        var context = new StringBuilder();
        foreach (var project in projects)
        {
            context.AppendLine($"Project: {project.Name} ({project.Id})");
            context.AppendLine($"Status: {project.Status}; priority: {project.Priority}; start: {FormatDate(project.StartDateUtc)}; due: {FormatDate(project.DueDateUtc)}");
            context.AppendLine($"Description: {project.Description ?? "No description"}");
            foreach (var task in project.Tasks)
            {
                context.AppendLine($"- Task: {task.Title}; status={task.Status}; priority={task.Priority}; assigned={task.AssignedUserId ?? "Unassigned"}; due={FormatDate(task.DueDateUtc)}; description={task.Description ?? "None"}");
            }
        }

        return context.Length == 0 ? "No projects or tasks are available." : context.ToString();
    }

    private static string FormatDate(DateTime? date) => date?.ToString("O") ?? "Not set";
}
