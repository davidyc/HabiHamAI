using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services.Ai;

public interface IAdminLlmModelsService
{
    Task<IActionResult> ListAsync(CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(AdminCreateLlmModelRequest request, string getActionName, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(Guid id, AdminUpdateLlmModelRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
