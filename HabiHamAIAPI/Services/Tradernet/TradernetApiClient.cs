using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HabiHamAIAPI.Options;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Tradernet;

public sealed class TradernetApiClient
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        WriteIndented = false,
    };

    private readonly HttpClient _httpClient;
    private readonly TradernetOptions _options;
    private readonly ILogger<TradernetApiClient> _logger;

    public TradernetApiClient(
        HttpClient httpClient,
        IOptions<TradernetOptions> options,
        ILogger<TradernetApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public string Domain => _options.Domain.Trim();

    public async Task<JsonDocument> GetHlocAsync(
        string symbol,
        DateTime startUtc,
        DateTime endUtc,
        int timeframeSeconds,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Tradernet API keys are not configured.");
        }

        var timeframeMinutes = Math.Max(1, timeframeSeconds / 60);
        var payloadObject = new Dictionary<string, object>
        {
            ["id"] = symbol.Trim(),
            ["count"] = -1,
            ["timeframe"] = timeframeMinutes,
            ["date_from"] = startUtc.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            ["date_to"] = endUtc.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            ["intervalMode"] = "OpenRay",
        };

        return await AuthorizedPostAsync("getHloc", payloadObject, cancellationToken);
    }

    public async Task<JsonDocument> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Tradernet API keys are not configured.");
        }

        return await AuthorizedPostAsync("GetAllUserTexInfo", new Dictionary<string, object>(), cancellationToken);
    }

    public Task<JsonDocument> GetPositionJsonAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Tradernet API keys are not configured.");
        }

        return AuthorizedPostAsync("getPositionJson", new Dictionary<string, object>(), cancellationToken);
    }

    public async Task<JsonDocument> FindSymbolsAsync(
        string query,
        string? exchange,
        CancellationToken cancellationToken)
    {
        var text = string.IsNullOrWhiteSpace(exchange)
            ? query.Trim()
            : $"{query.Trim()}@{exchange.Trim()}";

        var plain = await TryPlainCommandAsync("tickerFinder", new Dictionary<string, object> { ["text"] = text }, cancellationToken);
        if (plain is not null)
        {
            return plain;
        }

        if (!IsConfigured)
        {
            throw new InvalidOperationException("Tradernet API keys are not configured.");
        }

        return await AuthorizedPostAsync(
            "tickerFinder",
            new Dictionary<string, object> { ["text"] = text },
            cancellationToken);
    }

    private async Task<JsonDocument?> TryPlainCommandAsync(
        string command,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var domain = Domain;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var message = new Dictionary<string, object> { ["cmd"] = command, ["params"] = parameters };
        var q = JsonSerializer.Serialize(message, PayloadJsonOptions);
        var url = $"https://{domain.Trim().TrimStart('.')}/api?q={Uri.EscapeDataString(q)}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Tradernet plain {Command} HTTP {Status}", command, (int)response.StatusCode);
                return null;
            }

            return JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Tradernet plain {Command} failed", command);
            return null;
        }
    }

    private async Task<JsonDocument> AuthorizedPostAsync(
        string command,
        Dictionary<string, object> payloadObject,
        CancellationToken cancellationToken)
    {
        var domain = Domain;
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new InvalidOperationException("Tradernet domain is not configured.");
        }

        var payload = JsonSerializer.Serialize(payloadObject, PayloadJsonOptions);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var message = payload + timestamp;
        var signature = Sign(_options.PrivateKey, message);

        var url = $"https://{domain.Trim().TrimStart('.')}/api/{command}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("X-NtApi-PublicKey", _options.PublicKey.Trim());
        request.Headers.TryAddWithoutValidation("X-NtApi-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("X-NtApi-Sig", signature);

        _logger.LogDebug("Tradernet request {Command} to {Url}", command, url);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Tradernet HTTP {Status} for {Command}: {Body}",
                (int)response.StatusCode,
                command,
                Truncate(body, 500));
            throw new InvalidOperationException(
                $"Tradernet HTTP {(int)response.StatusCode}: {Truncate(body, 200)}");
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Tradernet returned invalid JSON.", ex);
        }
    }

    private static string Sign(string privateKey, string message)
    {
        var keyBytes = Encoding.UTF8.GetBytes(privateKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var hash = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
