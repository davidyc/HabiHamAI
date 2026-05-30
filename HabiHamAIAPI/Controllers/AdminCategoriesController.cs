using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/categories")]
[RequirePermission(AppPermissionCatalog.AdminCategories)]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly IAdminCategoriesService _service;

    public AdminCategoriesController(IAdminCategoriesService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _service.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _service.GetAsync(id, cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] AdminCreateUserCategoryRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, nameof(Get), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateUserCategoryRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
