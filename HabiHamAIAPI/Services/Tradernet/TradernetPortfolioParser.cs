using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Models;

namespace HabiHamAIAPI.Services.Tradernet;

internal static class TradernetPortfolioParser
{
    private static readonly string[] PositionArrayKeys =
    [
        "pos", "positions", "position", "portfolio", "data", "result", "list",
    ];

    public static BrokerPortfolioResponse Parse(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (TryGetError(root, out var err))
        {
            throw new InvalidOperationException(err);
        }

        var positions = new List<BrokerPortfolioPositionDto>();
        foreach (var array in FindPositionArrays(root))
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var parsed = TryParsePosition(item);
                if (parsed is not null)
                {
                    positions.Add(parsed);
                }
            }
        }

        if (positions.Count == 0 && root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var parsed = TryParsePosition(item);
                if (parsed is not null)
                {
                    positions.Add(parsed);
                }
            }
        }

        var totalMarket = positions.Sum(p => p.MarketValue ?? 0m);
        var totalPl = positions.Sum(p => p.ProfitLoss ?? 0m);
        var totalInvested = positions.Sum(p =>
        {
            if (p.AveragePrice is { } avg && avg > 0)
            {
                return avg * p.Quantity;
            }

            if (p.MarketValue is { } mv && p.ProfitLoss is { } pl)
            {
                return mv - pl;
            }

            return 0m;
        });

        var currency = positions
            .Select(p => p.Currency)
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

        decimal? plPercent = null;
        if (totalInvested > 0)
        {
            plPercent = totalPl / totalInvested * 100m;
        }

        return new BrokerPortfolioResponse
        {
            Source = "tradernet",
            Currency = currency,
            TotalInvested = totalInvested > 0 ? totalInvested : null,
            TotalMarketValue = totalMarket,
            TotalProfitLoss = totalPl,
            TotalProfitLossPercent = plPercent,
            PositionsCount = positions.Count,
            IsStub = positions.Count == 0,
            Positions = positions,
        };
    }

    private static IEnumerable<JsonElement> FindPositionArrays(JsonElement root)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<JsonElement>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var prop in current.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array &&
                    PositionArrayKeys.Contains(prop.Name, StringComparer.OrdinalIgnoreCase) &&
                    seen.Add(prop.Name))
                {
                    yield return prop.Value;
                }

                if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                {
                    queue.Enqueue(prop.Value);
                }
            }
        }
    }

    private static BrokerPortfolioPositionDto? TryParsePosition(JsonElement item)
    {
        var ticker = GetString(item, "i", "ticker", "symbol", "code", "instr", "id");
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return null;
        }

        var quantity = GetDecimal(item, "q", "qty", "quantity", "amount", "vol");
        if (quantity is null or 0)
        {
            return null;
        }

        var name = GetString(item, "name", "name2", "name_short", "short_name", "issue_name");
        var avg = GetDecimal(item, "bal_price_a", "open_bal", "average_price", "avg_price", "price_a");
        var current = GetDecimal(item, "mkt_price", "price", "current_price", "last_price", "close_price");
        var marketValue = GetDecimal(item, "market_value", "mkt_value", "value", "sum", "amount_money");
        var profit = GetDecimal(item, "profit_close", "profit", "pl", "gain", "unrealized_pl");
        var currency = GetString(item, "curr", "curr_a", "currency", "curr_pos");

        if (marketValue is null && current is not null)
        {
            marketValue = current * quantity;
        }

        return new BrokerPortfolioPositionDto
        {
            Ticker = ticker.Trim().ToUpperInvariant(),
            Name = name,
            Quantity = quantity.Value,
            AveragePrice = avg,
            CurrentPrice = current,
            MarketValue = marketValue,
            ProfitLoss = profit,
            Currency = currency,
        };
    }

    private static bool TryGetError(JsonElement root, out string message)
    {
        message = string.Empty;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("errMsg", out var errProp))
        {
            var err = errProp.GetString();
            if (!string.IsNullOrWhiteSpace(err))
            {
                message = err;
                return true;
            }
        }

        return false;
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
                    return value;
                }
            }
            else if (prop.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return prop.ToString();
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (!item.TryGetProperty(name, out var prop))
            {
                continue;
            }

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var num))
            {
                return num;
            }

            if (prop.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    prop.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
