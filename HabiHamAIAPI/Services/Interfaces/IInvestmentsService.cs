using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IInvestmentsService
{
    Task<IActionResult> ListAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> GetSummaryAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> GetAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(ClaimsPrincipal principal, CreateUserInvestmentRequest request, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(ClaimsPrincipal principal, Guid id, UpdateUserInvestmentRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
