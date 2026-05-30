namespace HabiHamAIAPI.Models;

public sealed class AppPermission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class AppRolePermission
{
    public string RoleName { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public AppRole? Role { get; set; }
    public AppPermission? Permission { get; set; }
}

public sealed class AppPermissionResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class AdminRolePermissionsResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
}

public sealed class AdminUpdateRolePermissionsRequest
{
    public IReadOnlyList<string> PermissionCodes { get; set; } = [];
}
