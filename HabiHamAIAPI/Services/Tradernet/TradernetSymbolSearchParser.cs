using System.Text.Json;
using HabiHamAIAPI.Models;

namespace HabiHamAIAPI.Services.Tradernet;

internal static class TradernetSymbolSearchParser
{
    public static IReadOnlyList<MarketSymbolSearchItemDto> Parse(JsonDocument doc, int maxResults = 30)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<MarketSymbolSearchItemDto>();
        CollectFromElement(doc.RootElement, seen, results, maxResults);
        return results;
    }

    private static void CollectFromElement(
        JsonElement element,
        HashSet<string> seen,
        List<MarketSymbolSearchItemDto> results,
        int maxResults)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                TryAddObject(element, seen, results, maxResults);
                foreach (var prop in element.EnumerateObject())
                {
                    if (results.Count >= maxResults)
                    {
                        return;
                    }

                    CollectFromElement(prop.Value, seen, results, maxResults);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (results.Count >= maxResults)
                    {
                        return;
                    }

                    CollectFromElement(item, seen, results, maxResults);
                }

                break;
        }
    }

    private static void TryAddObject(
        JsonElement item,
        HashSet<string> seen,
        List<MarketSymbolSearchItemDto> results,
        int maxResults)
    {
        if (results.Count >= maxResults)
        {
            return;
        }

        var ticker = GetString(item, "i", "ticker", "symbol", "code", "instr", "nt_ticker", "id", "code_nm");
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return;
        }

        ticker = ticker.Trim().ToUpperInvariant();
        if (!seen.Add(ticker))
        {
            return;
        }

        var name = GetString(
            item,
            "name",
            "short_name",
            "name_short",
            "issue_name",
            "n",
            "title");
        var exchange = GetString(item, "mkt", "exchange", "mkt_short_code", "market", "board");

        results.Add(new MarketSymbolSearchItemDto
        {
            Ticker = ticker,
            Name = name,
            Exchange = exchange,
        });
    }

    private static string? GetString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (!item.TryGetProperty(name, out var prop))
            {
                continue;
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
            else if (prop.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return prop.ToString();
            }
        }

        return null;
    }
}
