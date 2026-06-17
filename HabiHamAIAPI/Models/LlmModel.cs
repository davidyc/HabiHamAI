namespace HabiHamAIAPI.Models;

public sealed class LlmModel
{
    public Guid Id { get; set; }

    /// <summary>Идентификатор модели для API провайдера (например gpt-4o).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Отображаемое название в UI. Если пусто — используется <see cref="Name"/>.</summary>
    public string? Label { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
