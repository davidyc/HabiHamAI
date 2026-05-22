namespace HabiHamAIAPI.Models;

public sealed class ImportWeeklyTrainingReviewRequest
{
    public string Content { get; set; } = string.Empty;
    public int? Days { get; set; }
    public string? EndingOn { get; set; }
}
