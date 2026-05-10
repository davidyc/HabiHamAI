using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUsersService _service;

    public AdminUsersController(IAdminUsersService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> GetUsers() =>
        _service.GetUsersAsync();

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetUser(Guid id) =>
        _service.GetUserAsync(id);

    [HttpPost]
    public Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request) =>
        _service.CreateUserAsync(request, nameof(GetUser));

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserRequest request) =>
        _service.UpdateUserAsync(id, request);

    [HttpPut("{id:guid}/password")]
    public Task<IActionResult> UpdateUserPassword(Guid id, [FromBody] AdminUpdateUserPasswordRequest request) =>
        _service.UpdateUserPasswordAsync(id, request);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> DeleteUser(Guid id) =>
        _service.DeleteUserAsync(id);
}
