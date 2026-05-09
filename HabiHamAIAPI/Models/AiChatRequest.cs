namespace HabiHamAIAPI.Models;

public sealed class AiChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public Guid? DialogId { get; set; }

    /// <summary>
    /// If set, this active assistant is used for system prompt and extras for this turn.
    /// If omitted, falls back to the user's saved selection.
    /// </summary>
    public Guid? AssistantId { get; set; }
}
