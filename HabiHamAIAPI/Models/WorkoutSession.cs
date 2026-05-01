namespace HabiHamAIAPI.Models;

public sealed class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Day { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public AppUser? User { get; set; }
    public List<WorkoutExercise> Exercises { get; set; } = [];

    public static WorkoutSession Create(
        Guid userId,
        string sessionCode,
        DateOnly date,
        string day,
        string notes,
        DateTime nowUtc)
    {
        return new WorkoutSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionCode = NormalizeRequired(sessionCode),
            Date = date,
            Day = NormalizeRequired(day),
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    public void UpdateDetails(DateOnly date, string day, string notes, DateTime nowUtc)
    {
        Date = date;
        Day = NormalizeRequired(day);
        Notes = NormalizeOptional(notes);
        UpdatedAtUtc = nowUtc;
    }

    public void ReplaceExercises(IEnumerable<WorkoutExercise> exercises)
    {
        Exercises.Clear();
        Exercises.AddRange(exercises);
    }

    private static string NormalizeRequired(string value) => (value ?? string.Empty).Trim();
    private static string NormalizeOptional(string? value) => (value ?? string.Empty).Trim();
}

public sealed class WorkoutExercise
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
    public int Order { get; set; }
    public WorkoutSession? Session { get; set; }
    public List<WorkoutSet> Sets { get; set; } = [];

    public static WorkoutExercise Create(Guid sessionId, string name, string? meta, int order)
    {
        return new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = (name ?? string.Empty).Trim(),
            Meta = (meta ?? string.Empty).Trim(),
            Order = order
        };
    }

    public void SetSets(IEnumerable<WorkoutSet> sets)
    {
        Sets.Clear();
        Sets.AddRange(sets);
    }
}

public sealed class WorkoutSet
{
    public Guid Id { get; set; }
    public Guid ExerciseId { get; set; }
    public string Weight { get; set; } = string.Empty;
    public string Reps { get; set; } = string.Empty;
    public string Rpe { get; set; } = string.Empty;
    public int Order { get; set; }
    public WorkoutExercise? Exercise { get; set; }

    public static WorkoutSet Create(Guid exerciseId, string? weight, string? reps, string? rpe, int order)
    {
        return new WorkoutSet
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            Weight = (weight ?? string.Empty).Trim(),
            Reps = (reps ?? string.Empty).Trim(),
            Rpe = (rpe ?? string.Empty).Trim(),
            Order = order
        };
    }
}
