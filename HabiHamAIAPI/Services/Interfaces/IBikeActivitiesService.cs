using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IBikeActivitiesService
{
    Task<IActionResult> ImportTcxAsync(ClaimsPrincipal principal, IFormFile? file, CancellationToken cancellationToken);
    Task<IActionResult> ListAsync(ClaimsPrincipal principal, DateOnly? from, DateOnly? to, string? sport, CancellationToken cancellationToken);
    Task<IActionResult> GetByIdAsync(ClaimsPrincipal principal, Guid id, int? trackpointLimit, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken);
}
