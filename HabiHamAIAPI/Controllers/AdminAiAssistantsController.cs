using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/ai-assistants")]
[Authorize(Roles = "Admin")]
public sealed class AdminAiAssistantsController : ControllerBase
{
    private readonly IAdminAiAssistantsService _service;

    public AdminAiAssistantsController(IAdminAiAssistantsService service)
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
    public Task<IActionResult> Create([FromBody] AdminCreateAiAssistantRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(request, nameof(Get), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateAiAssistantRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);
}
