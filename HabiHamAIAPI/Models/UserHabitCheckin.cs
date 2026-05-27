namespace HabiHamAIAPI.Models;

public sealed class UserHabitCheckin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid HabitId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>partial = жёлтый, done = зелёный, failed = красный (провал).</summary>
    public string Status { get; set; } = UserHabitCheckinStatus.Done;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public UserHabit? Habit { get; set; }
    public AppUser? User { get; set; }
}

