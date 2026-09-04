using System.ComponentModel.DataAnnotations;

namespace CloudOps.Application.AI;

public sealed class AiChatRequest
{
    [Required, StringLength(2000, MinimumLength = 3)] public string Question { get; init; } = string.Empty;
    public Guid? ProjectId { get; init; }
}

public sealed record AiSourceDto(Guid ProjectId, string ProjectName, int TaskCount);

public sealed record AiChatResponse(string Answer, string Provider, DateTime GeneratedAtUtc, IReadOnlyList<AiSourceDto> Sources);

public interface IAiService
{
    Task<AiChatResponse> AskAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}