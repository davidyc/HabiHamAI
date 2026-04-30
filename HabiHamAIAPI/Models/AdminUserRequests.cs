namespace HabiHamAIAPI.Models;

public sealed class AdminCreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public sealed class AdminUpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public sealed class AdminUpdateUserPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed class AdminUserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
