using System.Text.RegularExpressions;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed partial class AdminRolesService : IAdminRolesService
{
    private static readonly Regex RoleNamePattern = RoleNameRegex();

    private readonly AppDbContext _dbContext;

    public AdminRolesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AppRoles
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.AppRoles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapToResponseExpression())
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? new NotFoundObjectResult(new { message = "Role not found." })
            : new OkObjectResult(row);
    }

    public async Task<IActionResult> CreateAsync(
        AdminCreateAppRoleRequest request,
        string getActionName,
        CancellationToken cancellationToken)
    {
        var name = NormalizeRoleName(request.Name);
        if (name is null)
        {
            return new BadRequestObjectResult(new
            {
                message = "Role code is invalid. Use 2–30 Latin letters/digits, start with a letter (e.g. Moderator).",
            });
        }

        var label = NormalizeLabel(request.Label);
        if (label is null)
        {
            return new BadRequestObjectResult(new { message = "Role label is required." });
        }

        var exists = await _dbContext.AppRoles
            .AsNoTracking()
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return new ConflictObjectResult(new { message = "Role with this code already exists." });
        }

        var now = DateTime.UtcNow;
        var maxSortOrder = await _dbContext.AppRoles
            .AsNoTracking()
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var row = new AppRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Label = label,
            Description = NormalizeDescription(request.Description),
            IsSystem = false,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder ?? (maxSortOrder + 1),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _dbContext.AppRoles.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedAtActionResult(getActionName, "AdminRoles", new { id = row.Id }, MapToResponse(row));
    }

    public async Task<IActionResult> UpdateAsync(Guid id, AdminUpdateAppRoleRequest request, CancellationToken cancellationToken)
    {
        var label = NormalizeLabel(request.Label);
        if (label is null)
        {
            return new BadRequestObjectResult(new { message = "Role label is required." });
        }

        var row = await _dbContext.AppRoles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Role not found." });
        }

        if (row.IsSystem && !request.IsActive)
        {
            return new BadRequestObjectResult(new { message = "System roles cannot be deactivated." });
        }

        row.Label = label;
        row.Description = NormalizeDescription(request.Description);
        row.IsActive = request.IsActive;
        row.SortOrder = request.SortOrder;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(MapToResponse(row));
    }

    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.AppRoles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Role not found." });
        }

        if (row.IsSystem)
        {
            return new BadRequestObjectResult(new { message = "System roles cannot be deleted." });
        }

        var inUse = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(x => x.RoleName == row.Name, cancellationToken);
        if (inUse)
        {
            return new ConflictObjectResult(new { message = "Role is assigned to users and cannot be deleted." });
        }

        _dbContext.AppRoles.Remove(row);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Role deleted." });
    }

    public async Task<IActionResult> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AppPermissions
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new AppPermissionResponse
            {
                Id = x.Id,
                Code = x.Code,
                Label = x.Label,
                Description = x.Description,
                Category = x.Category,
                SortOrder = x.SortOrder,
                IsSystem = x.IsSystem,
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> GetRolePermissionsAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.AppRoles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return new NotFoundObjectResult(new { message = "Role not found." });
        }

        var permissionCodes = await _dbContext.AppRolePermissions
            .AsNoTracking()
            .Where(x => x.RoleName == role.Name)
            .Select(x => x.PermissionCode)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new OkObjectResult(new AdminRolePermissionsResponse
        {
            RoleId = role.Id,
            RoleName = role.Name,
            PermissionCodes = permissionCodes,
        });
    }

    public async Task<IActionResult> UpdateRolePermissionsAsync(
        Guid id,
        AdminUpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.AppRoles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
        {
            return new NotFoundObjectResult(new { message = "Role not found." });
        }

        var validCodes = await _dbContext.AppPermissions
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);
        var validSet = new HashSet<string>(validCodes, StringComparer.OrdinalIgnoreCase);

        var requested = (request.PermissionCodes ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requested.Any(code => !validSet.Contains(code)))
        {
            return new BadRequestObjectResult(new { message = "One or more permissions are invalid." });
        }

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var minimum in AppPermissionCatalog.AdminRoleMinimumPermissions)
            {
                if (!requested.Contains(minimum, StringComparer.OrdinalIgnoreCase))
                {
                    requested.Add(minimum);
                }
            }
        }

        var existing = await _dbContext.AppRolePermissions
            .Where(x => x.RoleName == role.Name)
            .ToListAsync(cancellationToken);
        _dbContext.AppRolePermissions.RemoveRange(existing);

        foreach (var code in requested)
        {
            _dbContext.AppRolePermissions.Add(new AppRolePermission
            {
                RoleName = role.Name,
                PermissionCode = code,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new AdminRolePermissionsResponse
        {
            RoleId = role.Id,
            RoleName = role.Name,
            PermissionCodes = requested.OrderBy(x => x).ToList(),
        });
    }

    private static System.Linq.Expressions.Expression<Func<AppRole, AppRoleResponse>> MapToResponseExpression()
    {
        return x => new AppRoleResponse
        {
            Id = x.Id,
            Name = x.Name,
            Label = x.Label,
            Description = x.Description,
            IsSystem = x.IsSystem,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };
    }

    private static AppRoleResponse MapToResponse(AppRole x)
    {
        return new AppRoleResponse
        {
            Id = x.Id,
            Name = x.Name,
            Label = x.Label,
            Description = x.Description,
            IsSystem = x.IsSystem,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };
    }

    private static string? NormalizeRoleName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 30)
        {
            return null;
        }

        return RoleNamePattern.IsMatch(value) ? value : null;
    }

    private static string? NormalizeLabel(string? label)
    {
        var value = (label ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 100 ? null : value;
    }

    private static string? NormalizeDescription(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= 300 ? normalized : normalized[..300];
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]{1,29}$")]
    private static partial Regex RoleNameRegex();
}
