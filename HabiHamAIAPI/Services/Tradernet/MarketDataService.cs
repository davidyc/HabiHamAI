using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Tradernet;

public interface IMarketDataService
{
    Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken);
    Task<IActionResult> GetCandlesAsync(
        string ticker,
        string interval,
        string period,
        CancellationToken cancellationToken);

    Task<IActionResult> GetPortfolioAsync(CancellationToken cancellationToken);

    Task<IActionResult> SearchSymbolsAsync(
        string query,
        string? exchange,
        CancellationToken cancellationToken);

    Task<IActionResult> GetRefbookAsync(
        string market,
        bool tradeableOnly,
        CancellationToken cancellationToken);
}

public sealed class MarketDataService : IMarketDataService
{
    private readonly TradernetApiClient _client;
    private readonly TradernetOptions _options;
    private readonly RefbookCatalogService _refbooks;

    public MarketDataService(
        TradernetApiClient client,
        IOptions<TradernetOptions> options,
        RefbookCatalogService refbooks)
    {
        _client = client;
        _options = options.Value;
        _refbooks = refbooks;
    }

    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var response = new MarketStatusResponse
        {
            Configured = _options.IsConfigured,
            Domain = _options.Domain,
        };

        if (!_options.IsConfigured)
        {
            response.Message = "Добавьте TRADERNET_PUBLIC_KEY и TRADERNET_PRIVATE_KEY в .env API.";
            return new OkObjectResult(response);
        }

        try
        {
            using var doc = await _client.GetUserInfoAsync(cancellationToken);
            if (TryGetError(doc, out var err))
            {
                response.Connected = false;
                response.Message = err;
                return new OkObjectResult(response);
            }

            response.Connected = true;
            response.Message = "Подключение к Tradernet успешно.";
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            response.Connected = false;
            response.Message = ex.Message;
            return new OkObjectResult(response);
        }
    }

    public async Task<IActionResult> GetCandlesAsync(
        string ticker,
        string interval,
        string period,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new BadRequestObjectResult(new { message = "Укажите тикер." });
        }

        if (!_options.IsConfigured)
        {
            return new ObjectResult(new { message = "Tradernet API не настроен на сервере." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }

        var normalizedTicker = ticker.Trim().ToUpperInvariant();
        var timeframeSeconds = interval == "1h" ? 3600 : 86400;
        var days = period switch
        {
            "1m" => 30,
            "6m" => 180,
            _ => 90,
        };
        var end = DateTime.UtcNow;
        var start = end.AddDays(-days);

        try
        {
            using var doc = await _client.GetHlocAsync(
                normalizedTicker,
                start,
                end,
                timeframeSeconds,
                cancellationToken);

            if (TryGetError(doc, out var err))
            {
                return new BadRequestObjectResult(new { message = err });
            }

            var candles = ParseCandles(doc, normalizedTicker);
            if (candles.Count == 0)
            {
                return new NotFoundObjectResult(new
                {
                    message = $"Нет свечей для {normalizedTicker}. Проверьте тикер (например SBER.RU, FRHC.KZ).",
                });
            }

            return new OkObjectResult(new MarketCandlesResponse
            {
                Ticker = normalizedTicker,
                Source = "tradernet",
                Interval = interval,
                Period = period,
                Candles = candles,
            });
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = StatusCodes.Status502BadGateway,
            };
        }
    }

    public async Task<IActionResult> SearchSymbolsAsync(
        string query,
        string? exchange,
        CancellationToken cancellationToken)
    {
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return new BadRequestObjectResult(new { message = "Введите минимум 2 символа для поиска." });
        }

        var normalizedExchange = string.IsNullOrWhiteSpace(exchange)
            ? null
            : exchange.Trim().ToUpperInvariant();

        var localResults = _refbooks.Search(trimmed, normalizedExchange);
        if (localResults.Count > 0)
        {
            return new OkObjectResult(new MarketSymbolSearchResponse
            {
                Query = trimmed,
                Exchange = normalizedExchange,
                Results = localResults,
            });
        }

        try
        {
            using var doc = await _client.FindSymbolsAsync(trimmed, normalizedExchange, cancellationToken);
            if (TryGetError(doc, out var err))
            {
                return new BadRequestObjectResult(new { message = err });
            }

            var items = TradernetSymbolSearchParser.Parse(doc);
            if (items.Count == 0 && localResults.Count == 0)
            {
                localResults = _refbooks.Search(trimmed, normalizedExchange);
            }

            return new OkObjectResult(new MarketSymbolSearchResponse
            {
                Query = trimmed,
                Exchange = normalizedExchange,
                Results = items.Count > 0 ? items : localResults,
            });
        }
        catch (Exception ex)
        {
            if (localResults.Count > 0)
            {
                return new OkObjectResult(new MarketSymbolSearchResponse
                {
                    Query = trimmed,
                    Exchange = normalizedExchange,
                    Results = localResults,
                });
            }

            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = StatusCodes.Status502BadGateway,
            };
        }
    }

    public Task<IActionResult> GetRefbookAsync(
        string market,
        bool tradeableOnly,
        CancellationToken cancellationToken)
    {
        var key = market.Trim().ToUpperInvariant();
        var instruments = _refbooks.GetInstruments(key, tradeableOnly);
        if (instruments.Count == 0)
        {
            var available = _refbooks.AvailableMarkets;
            return Task.FromResult<IActionResult>(new NotFoundObjectResult(new
            {
                message = $"Справочник «{key}» не найден. Доступно: {string.Join(", ", available)}.",
            }));
        }

        return Task.FromResult<IActionResult>(new OkObjectResult(new
        {
            market = key,
            tradeableOnly,
            count = instruments.Count,
            instruments = instruments.Select(x => new
            {
                ticker = x.Ticker,
                name = x.Name,
                exchange = x.Exchange,
                istrade = x.IsTradeable ? 1 : 0,
            }),
        }));
    }

    public async Task<IActionResult> GetPortfolioAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return new ObjectResult(new { message = "Tradernet API не настроен на сервере." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
        }

        try
        {
            using var doc = await _client.GetPositionJsonAsync(cancellationToken);
            var portfolio = TradernetPortfolioParser.Parse(doc);
            return new OkObjectResult(portfolio);
        }
        catch (Exception ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = StatusCodes.Status502BadGateway,
            };
        }
    }

    private static bool TryGetError(JsonDocument doc, out string message)
    {
        message = string.Empty;
        if (!doc.RootElement.TryGetProperty("errMsg", out var errProp))
        {
            return false;
        }

        var err = errProp.GetString();
        if (string.IsNullOrWhiteSpace(err))
        {
            return false;
        }

        message = err;
        return true;
    }

    private static List<MarketCandleDto> ParseCandles(JsonDocument doc, string ticker)
    {
        var root = doc.RootElement;
        if (!root.TryGetProperty("hloc", out var hlocRoot) ||
            !hlocRoot.TryGetProperty(ticker, out var hlocArr))
        {
            return [];
        }

        JsonElement? xSeriesArr = null;
        if (root.TryGetProperty("xSeries", out var xRoot) &&
            xRoot.TryGetProperty(ticker, out var xs))
        {
            xSeriesArr = xs;
        }

        JsonElement? volumeArr = null;
        if (root.TryGetProperty("vl", out var vlRoot) &&
            vlRoot.TryGetProperty(ticker, out var vl))
        {
            volumeArr = vl;
        }

        var result = new List<MarketCandleDto>();
        var count = hlocArr.GetArrayLength();
        for (var i = 0; i < count; i++)
        {
            var bar = hlocArr[i];
            if (bar.ValueKind != JsonValueKind.Array || bar.GetArrayLength() < 4)
            {
                continue;
            }

            var high = bar[0].GetDecimal();
            var low = bar[1].GetDecimal();
            var open = bar[2].GetDecimal();
            var close = bar[3].GetDecimal();

            var date = FormatBarDate(xSeriesArr, i);
            long? volume = null;
            if (volumeArr is { } volumes &&
                volumes.ValueKind == JsonValueKind.Array &&
                i < volumes.GetArrayLength())
            {
                volume = volumes[i].TryGetInt64(out var v) ? v : null;
            }

            result.Add(new MarketCandleDto
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
            });
        }

        return result;
    }

    private static string FormatBarDate(JsonElement? xSeriesArr, int index)
    {
        if (xSeriesArr is not { } series ||
            series.ValueKind != JsonValueKind.Array ||
            index >= series.GetArrayLength())
        {
            return string.Empty;
        }

        var raw = series[index];
        if (raw.ValueKind == JsonValueKind.Number && raw.TryGetInt64(out var unix))
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unix + 3 * 3600);
            return dt.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return raw.ToString();
    }
}
