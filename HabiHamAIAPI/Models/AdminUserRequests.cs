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
    public DateOnly? BirthDate { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? About { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AiSummary { get; set; }
}
