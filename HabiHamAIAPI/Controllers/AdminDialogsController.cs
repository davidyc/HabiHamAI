using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/dialogs")]
[Authorize(Roles = "Admin")]
public sealed class AdminDialogsController : ControllerBase
{
    private readonly IAdminDialogsService _service;

    public AdminDialogsController(IAdminDialogsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> GetDialogs([FromQuery] Guid? userId, CancellationToken cancellationToken) =>
        _service.GetDialogsAsync(userId, cancellationToken);

    [HttpGet("{dialogId:guid}/messages")]
    public Task<IActionResult> GetDialogMessages(Guid dialogId, CancellationToken cancellationToken) =>
        _service.GetDialogMessagesAsync(dialogId, cancellationToken);

    [HttpPost]
    public Task<IActionResult> CreateDialog([FromBody] AdminUpsertDialogRequest request, CancellationToken cancellationToken) =>
        _service.CreateDialogAsync(request, cancellationToken);

    [HttpPut("{dialogId:guid}")]
    public Task<IActionResult> RenameDialog(Guid dialogId, [FromBody] AdminUpsertDialogRequest request, CancellationToken cancellationToken) =>
        _service.RenameDialogAsync(dialogId, request, cancellationToken);

    [HttpDelete("{dialogId:guid}")]
    public Task<IActionResult> DeleteDialog(Guid dialogId, CancellationToken cancellationToken) =>
        _service.DeleteDialogAsync(dialogId, cancellationToken);
}
