using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IAdminUsersService
{
    Task<IActionResult> GetUsersAsync();
    Task<IActionResult> GetUserAsync(Guid id);
    Task<IActionResult> CreateUserAsync(AdminCreateUserRequest request, string getUserActionName);
    Task<IActionResult> UpdateUserAsync(Guid id, AdminUpdateUserRequest request);
    Task<IActionResult> UpdateUserPasswordAsync(Guid id, AdminUpdateUserPasswordRequest request);
    Task<IActionResult> DeleteUserAsync(Guid id);
}
