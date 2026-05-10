using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IAdminDialogsService
{
    Task<IActionResult> GetDialogsAsync(Guid? userId, CancellationToken cancellationToken);
    Task<IActionResult> GetDialogMessagesAsync(Guid dialogId, CancellationToken cancellationToken);
    Task<IActionResult> CreateDialogAsync(AdminUpsertDialogRequest request, CancellationToken cancellationToken);
    Task<IActionResult> RenameDialogAsync(Guid dialogId, AdminUpsertDialogRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteDialogAsync(Guid dialogId, CancellationToken cancellationToken);
}
