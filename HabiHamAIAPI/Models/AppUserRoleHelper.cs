namespace HabiHamAIAPI.Models;

public static class AppUserRoleHelper
{
    public const string DefaultRoleName = "User";

    public static IReadOnlyList<string> ParseRoleNames(IEnumerable<string>? roleNames)
    {
        if (roleNames is null)
        {
            return [DefaultRoleName];
        }

        var list = new List<string>();
        foreach (var name in roleNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var normalized = name.Trim();
            if (!list.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(normalized);
            }
        }

        return list.Count > 0 ? list : [DefaultRoleName];
    }

    public static IEnumerable<string> ResolveRoleNamesFromRequest(
        IEnumerable<string>? roles,
        string? singleRole)
    {
        if (roles is not null)
        {
            var fromList = roles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            if (fromList.Count > 0)
            {
                return fromList;
            }
        }

        if (!string.IsNullOrWhiteSpace(singleRole))
        {
            return [singleRole.Trim()];
        }

        return [DefaultRoleName];
    }

    public static IReadOnlyList<string> GetRoles(AppUser user) =>
        user.RoleAssignments
            .Select(x => x.RoleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> GetRoleNames(AppUser user) => GetRoles(user);

    public static bool HasRole(AppUser user, string roleName) =>
        user.RoleAssignments.Any(x =>
            string.Equals(x.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

    public static void SetRoles(AppUser user, IEnumerable<string> roleNames)
    {
        var distinct = roleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinct.Count == 0)
        {
            distinct = [DefaultRoleName];
        }

        user.RoleAssignments.Clear();
        foreach (var roleName in distinct)
        {
            user.RoleAssignments.Add(new AppUserRoleAssignment
            {
                UserId = user.Id,
                RoleName = roleName,
            });
        }
    }

    public static void EnsureRole(AppUser user, string roleName)
    {
        if (HasRole(user, roleName))
        {
            return;
        }

        user.RoleAssignments.Add(new AppUserRoleAssignment
        {
            UserId = user.Id,
            RoleName = roleName.Trim(),
        });
    }
}
