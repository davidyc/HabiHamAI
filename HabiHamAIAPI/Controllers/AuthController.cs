using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest request) =>
        _service.LoginAsync(request);

    [AllowAnonymous]
    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request) =>
        _service.RegisterAsync(request);
}
