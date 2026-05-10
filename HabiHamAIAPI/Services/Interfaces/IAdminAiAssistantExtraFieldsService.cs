using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services.Ai;

public interface IAdminAiAssistantExtraFieldsService
{
    Task<IActionResult> ListAsync(Guid assistantId, CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(Guid assistantId, AdminUpsertAiAssistantExtraFieldRequest request, string listActionName, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(Guid assistantId, Guid fieldId, AdminUpsertAiAssistantExtraFieldRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(Guid assistantId, Guid fieldId, CancellationToken cancellationToken);
}
