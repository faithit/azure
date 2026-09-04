namespace CloudOps.Application.Abstractions;

public sealed record AiPrompt(string SystemMessage, string UserMessage);

public interface IAiProvider
{
    string Name { get; }
    Task<string> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default);
}