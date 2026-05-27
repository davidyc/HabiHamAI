namespace HabiHamAIAPI.Models;

public sealed class UserTodoItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public DateOnly? DoneDate { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
    public UserCategory? Category { get; set; }
}

