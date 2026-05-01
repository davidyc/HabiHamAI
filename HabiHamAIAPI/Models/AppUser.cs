namespace HabiHamAIAPI.Models;

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppUserRole Role { get; set; } = AppUserRole.User;
    public DateTime CreatedAtUtc { get; set; }
    public DateOnly? BirthDate { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? About { get; set; }
    public List<ChatDialog> Dialogs { get; set; } = [];
    public List<WorkoutSession> WorkoutSessions { get; set; } = [];
}
