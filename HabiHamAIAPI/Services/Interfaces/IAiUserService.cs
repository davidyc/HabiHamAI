using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services.Ai;

public interface IAiUserService
{
    Task<IActionResult> ChatAsync(ClaimsPrincipal principal, AiChatRequest request, CancellationToken cancellationToken);
    Task<IActionResult> WeeklyReviewAsync(ClaimsPrincipal principal, WeeklyTrainingReviewRequest request, CancellationToken cancellationToken);
    Task<IActionResult> GetWeeklyReviewsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> GetWeeklyReviewAsync(ClaimsPrincipal principal, Guid reviewId, CancellationToken cancellationToken);
    Task<IActionResult> ImportWeeklyReviewAsync(ClaimsPrincipal principal, ImportWeeklyTrainingReviewRequest request, CancellationToken cancellationToken);
    Task<IActionResult> GetDialogsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> GetDialogMessagesAsync(ClaimsPrincipal principal, Guid dialogId, CancellationToken cancellationToken);
    Task<IActionResult> CreateDialogAsync(ClaimsPrincipal principal, CreateDialogRequest request, CancellationToken cancellationToken);
    Task<IActionResult> RenameDialogAsync(ClaimsPrincipal principal, Guid dialogId, RenameDialogRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteDialogAsync(ClaimsPrincipal principal, Guid dialogId, CancellationToken cancellationToken);
    Task<IActionResult> GetAssistantsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> SetAssistantSelectionAsync(ClaimsPrincipal principal, AiAssistantSelectionRequest request, CancellationToken cancellationToken);
    Task<IActionResult> GetAssistantExtraFieldsAsync(ClaimsPrincipal principal, Guid assistantId, CancellationToken cancellationToken);
    Task<IActionResult> PutAssistantExtraFieldsAsync(ClaimsPrincipal principal, UserAiAssistantExtrasPutRequest request, CancellationToken cancellationToken);
}
