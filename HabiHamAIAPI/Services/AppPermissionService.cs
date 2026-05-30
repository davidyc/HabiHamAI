using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public interface IAppPermissionService
{
    Task EnsureCatalogAsync(CancellationToken cancellationToken = default);
    Task EnsureDefaultRolePermissionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ResolvePermissionsForRoleNamesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ResolvePermissionsForUserAsync(
        AppUser user,
        CancellationToken cancellationToken = default);
}

public sealed class AppPermissionService : IAppPermissionService
{
    private readonly AppDbContext _dbContext;

    public AppPermissionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureCatalogAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seed in AppPermissionCatalog.All)
        {
            var existing = await _dbContext.AppPermissions
                .FirstOrDefaultAsync(x => x.Code == seed.Code, cancellationToken);
            if (existing is null)
            {
                _dbContext.AppPermissions.Add(new AppPermission
                {
                    Id = Guid.NewGuid(),
                    Code = seed.Code,
                    Label = seed.Label,
                    Description = seed.Description,
                    Category = seed.Category,
                    SortOrder = seed.SortOrder,
                    IsSystem = true,
                });
            }
            else
            {
                existing.Label = seed.Label;
                existing.Description = seed.Description;
                existing.Category = seed.Category;
                existing.SortOrder = seed.SortOrder;
                existing.IsSystem = true;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureDefaultRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (roleName, permissionCodes) in AppPermissionCatalog.DefaultRolePermissions)
        {
            var roleExists = await _dbContext.AppRoles
                .AsNoTracking()
                .AnyAsync(x => x.Name == roleName, cancellationToken);
            if (!roleExists)
            {
                continue;
            }

            var hasAny = await _dbContext.AppRolePermissions
                .AsNoTracking()
                .AnyAsync(x => x.RoleName == roleName, cancellationToken);
            if (hasAny)
            {
                continue;
            }

            foreach (var code in permissionCodes)
            {
                _dbContext.AppRolePermissions.Add(new AppRolePermission
                {
                    RoleName = roleName,
                    PermissionCode = code,
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ResolvePermissionsForRoleNamesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoles = roleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            return [];
        }

        var activeRoleNames = await _dbContext.AppRoles
            .AsNoTracking()
            .Where(x => normalizedRoles.Contains(x.Name) && x.IsActive)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (activeRoleNames.Count == 0)
        {
            return [];
        }

        return await _dbContext.AppRolePermissions
            .AsNoTracking()
            .Where(x => activeRoleNames.Contains(x.RoleName))
            .Join(
                _dbContext.AppPermissions.AsNoTracking(),
                rp => rp.PermissionCode,
                p => p.Code,
                (_, p) => p.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<string>> ResolvePermissionsForUserAsync(
        AppUser user,
        CancellationToken cancellationToken = default) =>
        ResolvePermissionsForRoleNamesAsync(AppUserRoleHelper.GetRoles(user), cancellationToken);
}
