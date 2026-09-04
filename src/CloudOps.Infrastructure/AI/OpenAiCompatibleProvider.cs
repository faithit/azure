using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CloudOps.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CloudOps.Infrastructure.AI;

public sealed class OpenAiCompatibleProvider(HttpClient httpClient, IOptions<AiOptions> options) : IAiProvider
{
    private readonly AiOptions _options = options.Value;

    public string Name => string.IsNullOrWhiteSpace(_options.Deployment) ? "OpenAI-compatible" : "Azure OpenAI";

    public async Task<string> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI API key is not configured. Set Ai__ApiKey through an environment variable.");
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("AI endpoint is not configured. Set Ai__Endpoint through an environment variable.");

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint());
        if (string.IsNullOrWhiteSpace(_options.Deployment)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        else request.Headers.Add("api-key", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new[] { new { role = "system", content = prompt.SystemMessage }, new { role = "user", content = prompt.UserMessage } },
            temperature = 0.2
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"AI provider returned {(int)response.StatusCode}: {payload}");

        using var document = JsonDocument.Parse(payload);
        var answer = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(answer) ? "The AI provider returned an empty response." : answer.Trim();
    }

    private string BuildEndpoint()
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return endpoint;
        if (!string.IsNullOrWhiteSpace(_options.Deployment)) return $"{endpoint}/openai/deployments/{Uri.EscapeDataString(_options.Deployment)}/chat/completions?api-version={Uri.EscapeDataString(_options.ApiVersion)}";
        return $"{endpoint}/v1/chat/completions";
    }
}