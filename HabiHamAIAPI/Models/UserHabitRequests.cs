namespace HabiHamAIAPI.Models;

public sealed class CreateUserHabitRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }

    /// <summary>Дней подряд done для освоения; по умолчанию 21. 0 — только вручную (пока без UI).</summary>
    public int? DaysToMaster { get; set; }
}

public sealed class UpdateUserHabitRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public int DaysToMaster { get; set; } = HabitMastery.DefaultDaysToMaster;
}

public sealed class UserHabitResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsMastered { get; set; }
    public int DaysToMaster { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UserHabitOverviewResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsMastered { get; set; }
    public int DaysToMaster { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public int CurrentStreakDays { get; set; }
    public bool IsDoneToday { get; set; }
    /// <summary>null — нет отметки сегодня; partial | done | failed.</summary>
    public string? TodayStatus { get; set; }
    public DateOnly? LastDoneDate { get; set; }
}

public sealed class UpsertUserHabitCheckinRequest
{
    public DateOnly Date { get; set; }
    public string Status { get; set; } = UserHabitCheckinStatus.Done;
}

public sealed class UserHabitCheckinResponse
{
    public DateOnly Date { get; set; }
    public string Status { get; set; } = UserHabitCheckinStatus.Done;
    public Guid? Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

