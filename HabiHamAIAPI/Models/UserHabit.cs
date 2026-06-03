namespace HabiHamAIAPI.Models;

public sealed class UserHabit
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Привычка освоена (достижение сохраняется, даже если серия прервётся).</summary>
    public bool IsMastered { get; set; }

    /// <summary>Дней подряд со статусом done для автоматической отметки «освоена»; 0 — не считать автоматически.</summary>
    public int DaysToMaster { get; set; } = 21;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
    public UserCategory? Category { get; set; }
    public List<UserHabitCheckin> Checkins { get; set; } = [];
}

