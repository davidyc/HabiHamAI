using Microsoft.AspNetCore.Authorization;

namespace HabiHamAIAPI.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
        : base(PermissionPolicyNames.For(permissionCode))
    {
        PermissionCode = permissionCode;
    }

    public string PermissionCode { get; }
}

public static class PermissionPolicyNames
{
    public static string For(string permissionCode) => $"perm:{permissionCode}";
}
