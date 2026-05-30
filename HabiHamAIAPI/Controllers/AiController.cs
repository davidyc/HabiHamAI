using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("ai")]
[RequirePermission(AppPermissionCatalog.AiAssistant)]
public sealed class AiController : ControllerBase
{
    private readonly IAiUserService _service;

    public AiController(IAiUserService service)
    {
        _service = service;
    }

    [HttpPost("chat")]
    public Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken) =>
        _service.ChatAsync(User, request, cancellationToken);

    [HttpPost("trainer/weekly-review")]
    public Task<IActionResult> WeeklyReview([FromBody] WeeklyTrainingReviewRequest request, CancellationToken cancellationToken) =>
        _service.WeeklyReviewAsync(User, request, cancellationToken);

    [HttpGet("trainer/weekly-reviews")]
    public Task<IActionResult> GetWeeklyReviews(CancellationToken cancellationToken) =>
        _service.GetWeeklyReviewsAsync(User, cancellationToken);

    [HttpGet("trainer/weekly-reviews/{reviewId:guid}")]
    public Task<IActionResult> GetWeeklyReview(Guid reviewId, CancellationToken cancellationToken) =>
        _service.GetWeeklyReviewAsync(User, reviewId, cancellationToken);

    [HttpDelete("trainer/weekly-reviews/{reviewId:guid}")]
    public Task<IActionResult> DeleteWeeklyReview(Guid reviewId, CancellationToken cancellationToken) =>
        _service.DeleteWeeklyReviewAsync(User, reviewId, cancellationToken);

    [HttpPost("trainer/weekly-reviews/import")]
    public Task<IActionResult> ImportWeeklyReview([FromBody] ImportWeeklyTrainingReviewRequest request, CancellationToken cancellationToken) =>
        _service.ImportWeeklyReviewAsync(User, request, cancellationToken);

    [HttpGet("dialogs")]
    public Task<IActionResult> GetDialogs(CancellationToken cancellationToken) =>
        _service.GetDialogsAsync(User, cancellationToken);

    [HttpGet("dialogs/{dialogId:guid}/messages")]
    public Task<IActionResult> GetDialogMessages(Guid dialogId, CancellationToken cancellationToken) =>
        _service.GetDialogMessagesAsync(User, dialogId, cancellationToken);

    [HttpPost("dialogs")]
    public Task<IActionResult> CreateDialog([FromBody] CreateDialogRequest request, CancellationToken cancellationToken) =>
        _service.CreateDialogAsync(User, request, cancellationToken);

    [HttpPut("dialogs/{dialogId:guid}")]
    public Task<IActionResult> RenameDialog(Guid dialogId, [FromBody] RenameDialogRequest request, CancellationToken cancellationToken) =>
        _service.RenameDialogAsync(User, dialogId, request, cancellationToken);

    [HttpDelete("dialogs/{dialogId:guid}")]
    public Task<IActionResult> DeleteDialog(Guid dialogId, CancellationToken cancellationToken) =>
        _service.DeleteDialogAsync(User, dialogId, cancellationToken);

    [HttpGet("assistants")]
    public Task<IActionResult> GetAssistants(CancellationToken cancellationToken) =>
        _service.GetAssistantsAsync(User, cancellationToken);

    [HttpPut("assistants/selection")]
    public Task<IActionResult> SetAssistantSelection([FromBody] AiAssistantSelectionRequest request, CancellationToken cancellationToken) =>
        _service.SetAssistantSelectionAsync(User, request, cancellationToken);

    [HttpGet("assistant-extra-fields")]
    public Task<IActionResult> GetAssistantExtraFields([FromQuery] Guid assistantId, CancellationToken cancellationToken) =>
        _service.GetAssistantExtraFieldsAsync(User, assistantId, cancellationToken);

    [HttpPut("assistant-extra-fields")]
    public Task<IActionResult> PutAssistantExtraFields([FromBody] UserAiAssistantExtrasPutRequest request, CancellationToken cancellationToken) =>
        _service.PutAssistantExtraFieldsAsync(User, request, cancellationToken);
}
