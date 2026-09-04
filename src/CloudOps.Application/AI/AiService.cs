using System.Text;
using CloudOps.Application.Abstractions;
using CloudOps.Application.Common;

namespace CloudOps.Application.AI;

public sealed class AiService(IProjectRepository projects, IAiProvider provider) : IAiService
{
    public async Task<AiChatResponse> AskAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var page = await projects.GetPageAsync(1, 100, cancellationToken);
        var selectedProjects = request.ProjectId is null
            ? page.Items
            : page.Items.Where(project => project.Id == request.ProjectId).ToList();

        if (request.ProjectId is not null && selectedProjects.Count == 0)
            throw new NotFoundException("Project", request.ProjectId);

        var context = BuildContext(selectedProjects);
        var answer = await provider.CompleteAsync(new AiPrompt(
            "You are the CloudOps delivery assistant. Answer only from the project and task context provided. " +
            "Be concise, concrete, and transparent when the context does not contain enough information. " +
            "For risks or suggestions, label them as recommendations rather than facts.",
            $"Current UTC time: {DateTime.UtcNow:O}\n\nProject and task context:\n{context}\n\nUser question: {request.Question.Trim()}"), cancellationToken);

        return new AiChatResponse(answer, provider.Name, DateTime.UtcNow, selectedProjects
            .Select(project => new AiSourceDto(project.Id, project.Name, project.Tasks.Count)).ToList());
    }

    private static string BuildContext(IEnumerable<Domain.Entities.Project> projects)
    {
        var context = new StringBuilder();
        foreach (var project in projects)
        {
            context.AppendLine($"Project: {project.Name} ({project.Id})");
            context.AppendLine($"Description: {project.Description ?? "No description"}");
            foreach (var task in project.Tasks)
            {
                context.AppendLine($"- Task: {task.Title}; status={task.Status}; priority={task.Priority}; due={task.DueDateUtc:O}; description={task.Description ?? "None"}");
            }
        }

        return context.Length == 0 ? "No projects or tasks are available." : context.ToString();
    }
}