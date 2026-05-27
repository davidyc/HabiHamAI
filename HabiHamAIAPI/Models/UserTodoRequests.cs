namespace HabiHamAIAPI.Models;

public sealed class CreateUserTodoItemRequest
{
    public string Title { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public Guid? CategoryId { get; set; }
}

public sealed class UserTodoItemResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? DoneDate { get; set; }
    public bool IsDone => DoneDate.HasValue;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UpsertUserTodoDoneRequest
{
    public bool IsDone { get; set; }
    public DateOnly? Date { get; set; }
}

