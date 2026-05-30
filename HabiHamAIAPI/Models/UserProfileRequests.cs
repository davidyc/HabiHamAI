namespace HabiHamAIAPI.Models;

public sealed class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
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
    public bool TelegramLinked { get; set; }
    public List<string> Permissions { get; set; } = [];
}

public sealed class UpdateUserProfileRequest
{
    public DateOnly? BirthDate { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? About { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
