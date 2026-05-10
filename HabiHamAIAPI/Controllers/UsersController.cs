using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _service;

    public UsersController(IUsersService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public Task<IActionResult> GetMyProfile() =>
        _service.GetMyProfileAsync(User);

    [HttpPut("me")]
    public Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request) =>
        _service.UpdateMyProfileAsync(User, request);
}
