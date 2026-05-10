using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/ai-assistants/{assistantId:guid}/extra-fields")]
[Authorize(Roles = "Admin")]
public sealed class AdminAiAssistantExtraFieldsController : ControllerBase
{
    private readonly IAdminAiAssistantExtraFieldsService _service;

    public AdminAiAssistantExtraFieldsController(IAdminAiAssistantExtraFieldsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> List(Guid assistantId, CancellationToken cancellationToken) =>
        _service.ListAsync(assistantId, cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create(Guid assistantId, [FromBody] AdminUpsertAiAssistantExtraFieldRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(assistantId, request, nameof(List), cancellationToken);

    [HttpPut("{fieldId:guid}")]
    public Task<IActionResult> Update(Guid assistantId, Guid fieldId, [FromBody] AdminUpsertAiAssistantExtraFieldRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(assistantId, fieldId, request, cancellationToken);

    [HttpDelete("{fieldId:guid}")]
    public Task<IActionResult> Delete(Guid assistantId, Guid fieldId, CancellationToken cancellationToken) =>
        _service.DeleteAsync(assistantId, fieldId, cancellationToken);
}
