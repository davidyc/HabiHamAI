using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IUsersService
{
    Task<IActionResult> GetMyProfileAsync(ClaimsPrincipal principal);
    Task<IActionResult> UpdateMyProfileAsync(ClaimsPrincipal principal, UpdateUserProfileRequest request);
    Task<IActionResult> GetMyWeightTrackerAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> UpsertMyWeightTrackerEntryAsync(ClaimsPrincipal principal, UpsertUserWeightEntryRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyWeightTrackerEntryAsync(ClaimsPrincipal principal, Guid entryId, CancellationToken cancellationToken);
}
