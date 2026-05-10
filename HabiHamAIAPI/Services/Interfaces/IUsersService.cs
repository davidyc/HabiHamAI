using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IUsersService
{
    Task<IActionResult> GetMyProfileAsync(ClaimsPrincipal principal);
    Task<IActionResult> UpdateMyProfileAsync(ClaimsPrincipal principal, UpdateUserProfileRequest request);
}
