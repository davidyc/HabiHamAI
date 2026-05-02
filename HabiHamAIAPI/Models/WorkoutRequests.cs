namespace HabiHamAIAPI.Models;

public sealed class WorkoutSessionResponse
{
    public Guid Id { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Day { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateOnly _date { get; set; }
    public bool IsActive { get; set; }
    public List<WorkoutExerciseResponse> Exercises { get; set; } = [];
}

public sealed class WorkoutExerciseResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Meta { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<WorkoutSetResponse> Sets { get; set; } = [];
}

public sealed class WorkoutSetResponse
{
    public Guid Id { get; set; }
    public string Weight { get; set; } = string.Empty;
    public string Reps { get; set; } = string.Empty;
    public string Rpe { get; set; } = string.Empty;
    public int Order { get; set; }
}

public sealed class UpsertWorkoutSessionRequest
{
    public string SessionCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Day { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
    public List<UpsertWorkoutExerciseRequest> Exercises { get; set; } = [];
}

public sealed class UpsertWorkoutExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Meta { get; set; }
    public List<UpsertWorkoutSetRequest> Sets { get; set; } = [];
}

public sealed class UpsertWorkoutSetRequest
{
    public string? Weight { get; set; }
    public string? Reps { get; set; }
    public string? Rpe { get; set; }
}

public sealed class CreateWorkoutExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Meta { get; set; }
    public int? Order { get; set; }
    public List<UpsertWorkoutSetRequest> Sets { get; set; } = [];
}

public sealed class UpdateWorkoutExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Meta { get; set; }
    public int? Order { get; set; }
    public List<UpsertWorkoutSetRequest> Sets { get; set; } = [];
}
