namespace HabiHamAIAPI.Models;

public sealed class UserInvestmentResponse
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal? CurrentPrice { get; set; }
    public string Currency { get; set; } = "RUB";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UserInvestmentSummaryResponse
{
    public decimal TotalInvested { get; set; }
    public decimal TotalCurrentValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercent { get; set; }
    public string Currency { get; set; } = "RUB";
    public int PositionsCount { get; set; }
    public bool IsStub { get; set; } = true;
}

public sealed class CreateUserInvestmentRequest
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public string Currency { get; set; } = "RUB";
    public string? Notes { get; set; }
}

public sealed class UpdateUserInvestmentRequest
{
    public string? Ticker { get; set; }
    public string? Name { get; set; }
    public string? AssetType { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? AveragePrice { get; set; }
    public string? Currency { get; set; }
    public string? Notes { get; set; }
}

public static class InvestmentAssetTypes
{
    public const string Stock = "stock";
    public const string Bond = "bond";
    public const string Etf = "etf";
    public const string Crypto = "crypto";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [Stock, Bond, Etf, Crypto, Other];
}
