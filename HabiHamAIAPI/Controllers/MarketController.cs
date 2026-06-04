using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.Tradernet;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users/me/market")]
[RequirePermission(AppPermissionCatalog.Investments)]
public sealed class MarketController : ControllerBase
{
    private readonly IMarketDataService _marketData;

    public MarketController(IMarketDataService marketData)
    {
        _marketData = marketData;
    }

    [HttpGet("status")]
    public Task<IActionResult> GetStatus(CancellationToken cancellationToken) =>
        _marketData.GetStatusAsync(cancellationToken);

    [HttpGet("candles")]
    public Task<IActionResult> GetCandles(
        [FromQuery] string ticker,
        [FromQuery] string interval = "1d",
        [FromQuery] string period = "3m",
        CancellationToken cancellationToken = default) =>
        _marketData.GetCandlesAsync(ticker, interval, period, cancellationToken);

    [HttpGet("portfolio")]
    public Task<IActionResult> GetPortfolio(CancellationToken cancellationToken) =>
        _marketData.GetPortfolioAsync(cancellationToken);

    [HttpGet("search")]
    public Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? exchange = null,
        CancellationToken cancellationToken = default) =>
        _marketData.SearchSymbolsAsync(q, exchange, cancellationToken);

    [HttpGet("refbook/{market}")]
    public Task<IActionResult> GetRefbook(
        string market,
        [FromQuery] bool tradeableOnly = false,
        CancellationToken cancellationToken = default) =>
        _marketData.GetRefbookAsync(market, tradeableOnly, cancellationToken);
}
