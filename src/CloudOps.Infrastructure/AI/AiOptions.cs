namespace CloudOps.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public string ApiKey { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string Deployment { get; init; } = string.Empty;
    public string ApiVersion { get; init; } = "2024-10-21";
    public string Model { get; init; } = "gpt-4o-mini";
}