using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/llm-models")]
[RequirePermission(AppPermissionCatalog.AdminAiAssistants)]
public sealed class AdminLlmModelsController : ControllerBase
{
    private readonly IAdminLlmModelsService _service;

    public AdminLlmModelsController(IAdminLlmModelsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _service.ListAsync(cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] AdminCreateLlmModelRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, nameof(List), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateLlmModelRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
