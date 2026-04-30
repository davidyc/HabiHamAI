using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HabiHamAIAPI.Options;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services;

public sealed class KernestalAiService
{
    private readonly HttpClient _httpClient;
    private readonly KernestalOptions _options;

    public KernestalAiService(HttpClient httpClient, IOptions<KernestalOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetCompletionAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0 || messages.All(x => string.IsNullOrWhiteSpace(x.Content)))
        {
            throw new ArgumentException("Messages must not be empty.", nameof(messages));
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Kernestal:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Kernestal:ApiKey is not configured.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var request = new KernestalChatRequest(
            _options.Model,
            messages
                .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                .Select(x => new KernestalMessage(x.Role, x.Content))
                .ToList());

        var response = await _httpClient.PostAsJsonAsync(_options.ChatCompletionsPath, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Kernestal API returned {(int)response.StatusCode}: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<KernestalChatResponse>(cancellationToken: cancellationToken);
        var text = payload?.Choices.FirstOrDefault()?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Kernestal API response does not contain completion text.");
        }

        return text;
    }

    private sealed record KernestalChatRequest(string Model, IReadOnlyList<KernestalMessage> Messages);
    private sealed record KernestalMessage(string Role, string Content);
    public sealed record AiChatMessage(string Role, string Content);

    private sealed class KernestalChatResponse
    {
        [JsonPropertyName("choices")]
        public List<KernestalChoice> Choices { get; set; } = [];
    }

    private sealed class KernestalChoice
    {
        [JsonPropertyName("message")]
        public KernestalChoiceMessage? Message { get; set; }
    }

    private sealed class KernestalChoiceMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
