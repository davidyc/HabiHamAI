namespace HabiHamAIAPI.Models;

public sealed class UserHabit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
    public UserCategory? Category { get; set; }
    public List<UserHabitCheckin> Checkins { get; set; } = [];
}

