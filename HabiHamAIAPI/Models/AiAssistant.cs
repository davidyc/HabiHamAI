namespace HabiHamAIAPI.Models;

public sealed class AiAssistant
{
    public Guid Id { get; set; }

    /// <summary>
    /// Optional stable code for lookups (e.g. &quot;trainer&quot; for workout chat). Nullable; unique when set.
    /// </summary>
    public string? AssistantCode { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Встроенный помощник (например &quot;Тренер&quot;) — нельзя удалить через API.
    /// </summary>
    public bool IsSystem { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
