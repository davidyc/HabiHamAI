namespace HabiHamAIAPI.Models;

public sealed class UserWeightEntryResponse
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class UpsertUserWeightEntryRequest
{
    public DateOnly Date { get; set; }
    public decimal WeightKg { get; set; }
}
