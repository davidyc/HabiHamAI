using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/roles")]
[RequirePermission(AppPermissionCatalog.AdminRoles)]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IAdminRolesService _service;

    public AdminRolesController(IAdminRolesService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _service.ListAsync(cancellationToken);

    [HttpGet("permissions/catalog")]
    public Task<IActionResult> ListPermissions(CancellationToken cancellationToken) =>
        _service.ListPermissionsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _service.GetAsync(id, cancellationToken);

    [HttpGet("{id:guid}/permissions")]
    public Task<IActionResult> GetPermissions(Guid id, CancellationToken cancellationToken) =>
        _service.GetRolePermissionsAsync(id, cancellationToken);

    [HttpPut("{id:guid}/permissions")]
    public Task<IActionResult> UpdatePermissions(
        Guid id,
        [FromBody] AdminUpdateRolePermissionsRequest request,
        CancellationToken cancellationToken) =>
        _service.UpdateRolePermissionsAsync(id, request, cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] AdminCreateAppRoleRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, nameof(Get), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateAppRoleRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
