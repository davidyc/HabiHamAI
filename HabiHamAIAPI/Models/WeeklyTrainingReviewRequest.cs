namespace HabiHamAIAPI.Models;

public sealed class WeeklyTrainingReviewRequest
{
    public Guid? DialogId { get; set; }
    public Guid? AssistantId { get; set; }
    public int? Days { get; set; }
    public string? EndingOn { get; set; }
    /// <summary>Дублировать обзор в диалог чата. По умолчанию — только сохранение в журнал обзоров.</summary>
    public bool WriteToDialog { get; set; }
}
