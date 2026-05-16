using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HabiHamAIAPI.Options;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services;

public sealed class KernestalAiService : IKernestalAiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly KernestalOptions _options;

    public KernestalAiService(HttpClient httpClient, IOptions<KernestalOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetCompletionAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken cancellationToken)
    {
        var filtered = messages
            .Where(x => !string.IsNullOrWhiteSpace(x.Content) || x.ToolCalls is { Count: > 0 })
            .ToList();
        if (filtered.Count == 0)
        {
            throw new ArgumentException("Messages must not be empty.", nameof(messages));
        }

        var result = await GetCompletionWithToolsAsync(filtered, [], cancellationToken);
        if (result.HasToolCalls)
        {
            throw new InvalidOperationException("Model requested tools but none were provided.");
        }

        if (string.IsNullOrWhiteSpace(result.Content))
        {
            throw new InvalidOperationException("Kernestal API response does not contain completion text.");
        }

        return result.Content.Trim();
    }

    public async Task<AiCompletionResult> GetCompletionWithToolsAsync(
        IReadOnlyList<AiChatMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            throw new ArgumentException("Messages must not be empty.", nameof(messages));
        }

        var hasContent = messages.Any(x =>
            !string.IsNullOrWhiteSpace(x.Content)
            || x.ToolCalls is { Count: > 0 }
            || string.Equals(x.Role, "tool", StringComparison.OrdinalIgnoreCase));
        if (!hasContent)
        {
            throw new ArgumentException("Messages must not be empty.", nameof(messages));
        }

        EnsureConfigured();

        var request = new KernestalChatRequest(
            _options.Model,
            messages.Select(ToApiMessage).ToList(),
            tools.Count > 0 ? tools.Select(ToApiTool).ToList() : null);

        var response = await PostChatAsync(request, cancellationToken);
        var choice = response?.Choices.FirstOrDefault()?.Message;
        if (choice is null)
        {
            throw new InvalidOperationException("Kernestal API response does not contain a message.");
        }

        var toolCalls = choice.ToolCalls?
            .Select(tc => new AiToolCall(
                tc.Id ?? Guid.NewGuid().ToString("N"),
                tc.Function?.Name ?? string.Empty,
                tc.Function?.Arguments ?? "{}"))
            .Where(tc => !string.IsNullOrWhiteSpace(tc.Name))
            .ToList() ?? [];

        return new AiCompletionResult(choice.Content, toolCalls);
    }

    private async Task<KernestalChatResponse?> PostChatAsync(KernestalChatRequest requestBody, CancellationToken cancellationToken)
    {
        // Do not mutate HttpClient.BaseAddress or DefaultRequestHeaders per call — the trainer agent
        // issues multiple LLM requests per chat turn (tool loop), which breaks after the first request.
        var uri = BuildChatCompletionsUri();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(requestBody, options: SerializerOptions);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Kernestal API returned {(int)response.StatusCode}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<KernestalChatResponse>(SerializerOptions, cancellationToken);
    }

    private Uri BuildChatCompletionsUri()
    {
        var baseUrl = _options.BaseUrl.Trim().TrimEnd('/');
        var path = _options.ChatCompletionsPath.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return new Uri($"{baseUrl}{path}");
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Kernestal:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Kernestal:ApiKey is not configured.");
        }
    }

    private static KernestalApiMessage ToApiMessage(AiChatMessage message)
    {
        if (string.Equals(message.Role, "tool", StringComparison.OrdinalIgnoreCase))
        {
            return new KernestalApiMessage
            {
                Role = "tool",
                Content = message.Content,
                ToolCallId = message.ToolCallId
            };
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            return new KernestalApiMessage
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(message.Content) ? null : message.Content,
                ToolCalls = message.ToolCalls.Select(tc => new KernestalApiToolCall
                {
                    Id = tc.Id,
                    Type = "function",
                    Function = new KernestalApiFunctionCall
                    {
                        Name = tc.Name,
                        Arguments = tc.ArgumentsJson
                    }
                }).ToList()
            };
        }

        return new KernestalApiMessage
        {
            Role = message.Role,
            Content = message.Content
        };
    }

    private static KernestalApiTool ToApiTool(AiToolDefinition tool) => new()
    {
        Type = "function",
        Function = new KernestalApiFunctionDef
        {
            Name = tool.Name,
            Description = tool.Description,
            Parameters = tool.ParametersSchema
        }
    };

    public sealed record AiChatMessage(
        string Role,
        string Content,
        IReadOnlyList<AiToolCall>? ToolCalls = null,
        string? ToolCallId = null);

    public sealed record AiToolCall(string Id, string Name, string ArgumentsJson);

    public sealed record AiToolDefinition(string Name, string Description, object ParametersSchema);

    public sealed class AiCompletionResult
    {
        public AiCompletionResult(string? content, IReadOnlyList<AiToolCall> toolCalls)
        {
            Content = content;
            ToolCalls = toolCalls;
        }

        public string? Content { get; }
        public IReadOnlyList<AiToolCall> ToolCalls { get; }
        public bool HasToolCalls => ToolCalls.Count > 0;
    }

    private sealed class KernestalChatRequest
    {
        public KernestalChatRequest(string model, IReadOnlyList<KernestalApiMessage> messages, IReadOnlyList<KernestalApiTool>? tools)
        {
            Model = model;
            Messages = messages;
            Tools = tools;
        }

        public string Model { get; }
        public IReadOnlyList<KernestalApiMessage> Messages { get; }
        public IReadOnlyList<KernestalApiTool>? Tools { get; }
    }

    private sealed class KernestalApiMessage
    {
        public string Role { get; set; } = string.Empty;
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<KernestalApiToolCall>? ToolCalls { get; set; }

        [JsonPropertyName("tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    private sealed class KernestalApiTool
    {
        public string Type { get; set; } = "function";
        public KernestalApiFunctionDef Function { get; set; } = new();
    }

    private sealed class KernestalApiFunctionDef
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object Parameters { get; set; } = new { type = "object", properties = new { } };
    }

    private sealed class KernestalApiToolCall
    {
        public string? Id { get; set; }
        public string Type { get; set; } = "function";
        public KernestalApiFunctionCall? Function { get; set; }
    }

    private sealed class KernestalApiFunctionCall
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }

    private sealed class KernestalChatResponse
    {
        [JsonPropertyName("choices")]
        public List<KernestalChoice> Choices { get; set; } = [];
    }

    private sealed class KernestalChoice
    {
        [JsonPropertyName("message")]
        public KernestalApiMessage? Message { get; set; }
    }
}
