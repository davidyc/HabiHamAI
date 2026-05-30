using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class InvestmentsService : IInvestmentsService
{
    private readonly AppDbContext _dbContext;

    public InvestmentsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return new OkObjectResult(Array.Empty<UserInvestmentResponse>());
    }

    public async Task<IActionResult> GetSummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return new OkObjectResult(new UserInvestmentSummaryResponse
        {
            TotalInvested = 0,
            TotalCurrentValue = 0,
            TotalProfitLoss = 0,
            TotalProfitLossPercent = 0,
            PositionsCount = 0,
            IsStub = true,
        });
    }

    public async Task<IActionResult> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return new NotFoundObjectResult(new { message = "Позиция не найдена." });
    }

    public async Task<IActionResult> CreateAsync(
        ClaimsPrincipal principal,
        CreateUserInvestmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return NotImplemented("Создание позиций пока не реализовано.");
    }

    public async Task<IActionResult> UpdateAsync(
        ClaimsPrincipal principal,
        Guid id,
        UpdateUserInvestmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return NotImplemented("Обновление позиций пока не реализовано.");
    }

    public async Task<IActionResult> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedUserAsync(principal, cancellationToken))
        {
            return Unauthorized();
        }

        return NotImplemented("Удаление позиций пока не реализовано.");
    }

    private async Task<bool> IsAuthorizedUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
        await GetCurrentUserAsync(principal, cancellationToken) is not null;

    private static ObjectResult Unauthorized() =>
        new(new { message = "User is not authorized." }) { StatusCode = StatusCodes.Status401Unauthorized };

    private static ObjectResult NotImplemented(string message) =>
        new(new { message }) { StatusCode = StatusCodes.Status501NotImplemented };

    private async Task<AppUser?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);
    }
}
