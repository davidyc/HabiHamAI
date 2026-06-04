namespace HabiHamAIAPI.Models;

public sealed class MarketCandleDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long? Volume { get; set; }
}

public sealed class MarketCandlesResponse
{
    public string Ticker { get; set; } = string.Empty;
    public string Source { get; set; } = "tradernet";
    public string Interval { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public IReadOnlyList<MarketCandleDto> Candles { get; set; } = [];
    public string? Message { get; set; }
}

public sealed class MarketStatusResponse
{
    public bool Configured { get; set; }
    public bool Connected { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class BrokerPortfolioPositionDto
{
    public string Ticker { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? ProfitLoss { get; set; }
    public string? Currency { get; set; }
}

public sealed class MarketSymbolSearchItemDto
{
    public string Ticker { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Exchange { get; set; }
}

public sealed class MarketSymbolSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public IReadOnlyList<MarketSymbolSearchItemDto> Results { get; set; } = [];
}

public sealed class BrokerPortfolioResponse
{
    public string Source { get; set; } = "tradernet";
    public string? Currency { get; set; }
    public decimal? TotalInvested { get; set; }
    public decimal TotalMarketValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal? TotalProfitLossPercent { get; set; }
    public int PositionsCount { get; set; }
    public bool IsStub { get; set; }
    public IReadOnlyList<BrokerPortfolioPositionDto> Positions { get; set; } = [];
}
