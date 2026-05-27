using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IAdminCategoriesService
{
    Task<IActionResult> ListAsync(CancellationToken cancellationToken);
    Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(AdminCreateUserCategoryRequest request, string getActionName, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(Guid id, AdminUpdateUserCategoryRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
