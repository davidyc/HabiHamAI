namespace HabiHamAIAPI.Models;

public sealed class ChatDialog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AiAssistantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public AppUser? User { get; set; }
    public AiAssistant? AiAssistant { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}
