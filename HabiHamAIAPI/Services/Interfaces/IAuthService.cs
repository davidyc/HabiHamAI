using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IAuthService
{
    Task<IActionResult> LoginAsync(LoginRequest request);
    Task<IActionResult> RegisterAsync(RegisterRequest request);
}
