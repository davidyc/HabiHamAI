namespace HabiHamAIAPI.Models;

public sealed class ChatMessage
{
    public Guid Id { get; set; }
    public Guid DialogId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ChatDialog? Dialog { get; set; }
}
