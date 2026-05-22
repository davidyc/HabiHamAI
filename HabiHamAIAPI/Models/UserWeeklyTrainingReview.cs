namespace HabiHamAIAPI.Models;

/// <summary>Сохранённый ИИ-обзор тренировок за фиксированный период.</summary>
public sealed class UserWeeklyTrainingReview
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AiAssistantId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    /// <summary>Отпечаток данных за период; при изменении тренировок обзор пересчитывается.</summary>
    public string DataFingerprint { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
    public AiAssistant? AiAssistant { get; set; }
}
