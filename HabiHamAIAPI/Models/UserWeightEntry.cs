namespace HabiHamAIAPI.Models;

public sealed class UserWeightEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
}
