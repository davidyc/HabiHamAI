using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IAdminRolesService
{
    Task<IActionResult> ListAsync(CancellationToken cancellationToken);
    Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IActionResult> CreateAsync(AdminCreateAppRoleRequest request, string getActionName, CancellationToken cancellationToken);
    Task<IActionResult> UpdateAsync(Guid id, AdminUpdateAppRoleRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IActionResult> ListPermissionsAsync(CancellationToken cancellationToken);
    Task<IActionResult> GetRolePermissionsAsync(Guid id, CancellationToken cancellationToken);
    Task<IActionResult> UpdateRolePermissionsAsync(Guid id, AdminUpdateRolePermissionsRequest request, CancellationToken cancellationToken);
}
