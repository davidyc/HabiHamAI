namespace HabiHamAIAPI.Models;

public sealed class UserAiAssistantExtras
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AiAssistantId { get; set; }
    public string ValuesJson { get; set; } = "{}";

    public AppUser? User { get; set; }
    public AiAssistant? AiAssistant { get; set; }
}
