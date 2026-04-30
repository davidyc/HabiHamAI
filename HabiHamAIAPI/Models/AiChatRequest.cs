namespace HabiHamAIAPI.Models;

public sealed class AiChatRequest
{
    public string Prompt { get; set; } = string.Empty;
    public Guid? DialogId { get; set; }
}
