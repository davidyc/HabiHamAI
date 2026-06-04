using System.Text.Json;
using HabiHamAIAPI.Models;

namespace HabiHamAIAPI.Services.Tradernet;

public sealed class RefbookCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyDictionary<string, IReadOnlyList<RefbookInstrument>> _catalogs;
    private readonly ILogger<RefbookCatalogService> _logger;

    public RefbookCatalogService(IHostEnvironment environment, ILogger<RefbookCatalogService> logger)
    {
        _logger = logger;
        _catalogs = LoadCatalogs(environment);
    }

    public IReadOnlyList<string> AvailableMarkets => _catalogs.Keys.OrderBy(x => x).ToList();

    public IReadOnlyList<RefbookInstrument> GetInstruments(string market, bool tradeableOnly = false)
    {
        var key = market.Trim().ToUpperInvariant();
        if (!_catalogs.TryGetValue(key, out var list))
        {
            return [];
        }

        return tradeableOnly
            ? list.Where(x => x.IsTradeable).ToList()
            : list;
    }

    public IReadOnlyList<MarketSymbolSearchItemDto> Search(string query, string? market, int maxResults = 30)
    {
        var q = query.Trim();
        if (q.Length < 1)
        {
            return [];
        }

        IEnumerable<RefbookInstrument> source = market is { Length: > 0 } m
            ? GetInstruments(m, tradeableOnly: false)
            : _catalogs.Values.SelectMany(x => x);

        var comparison = StringComparison.OrdinalIgnoreCase;
        return source
            .Where(x =>
                x.Ticker.Contains(q, comparison) ||
                (x.Name?.Contains(q, comparison) ?? false))
            .Take(maxResults)
            .Select(x => new MarketSymbolSearchItemDto
            {
                Ticker = x.Ticker,
                Name = x.Name,
                Exchange = x.Exchange,
            })
            .ToList();
    }

    private IReadOnlyDictionary<string, IReadOnlyList<RefbookInstrument>> LoadCatalogs(IHostEnvironment environment)
    {
        var result = new Dictionary<string, IReadOnlyList<RefbookInstrument>>(StringComparer.OrdinalIgnoreCase);
        var baseDir = Path.Combine(environment.ContentRootPath, "Data", "refbooks");
        if (!Directory.Exists(baseDir))
        {
            return result;
        }

        foreach (var path in Directory.EnumerateFiles(baseDir, "*-instruments.json"))
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var market = fileName.Replace("-instruments", "", StringComparison.OrdinalIgnoreCase)
                    .ToUpperInvariant();
                var json = File.ReadAllText(path);
                var rows = JsonSerializer.Deserialize<List<RefbookInstrumentRow>>(json, JsonOptions) ?? [];
                var instruments = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Ticker))
                    .Select(r => new RefbookInstrument(
                        r.Ticker!.Trim().ToUpperInvariant(),
                        r.Name,
                        r.Exchange?.ToUpperInvariant() ?? market,
                        r.Istrade == 1))
                    .ToList();

                result[market] = instruments;
                _logger.LogInformation("Refbook {Market}: {Count} instruments loaded", market, instruments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load refbook {Path}", path);
            }
        }

        return result;
    }

    private sealed class RefbookInstrumentRow
    {
        public string? Ticker { get; set; }
        public string? Name { get; set; }
        public string? Exchange { get; set; }
        public int Istrade { get; set; }
    }
}

public sealed record RefbookInstrument(string Ticker, string? Name, string Exchange, bool IsTradeable);
