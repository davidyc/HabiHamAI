using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services.Ai;

public interface IAdminAiAssistantsService
{
    Task<IActionResult> ListAsync(CancellationToken cancellationToken);
    Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(AdminCreateAiAssistantRequest request, string getActionName, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(Guid id, AdminUpdateAiAssistantRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
