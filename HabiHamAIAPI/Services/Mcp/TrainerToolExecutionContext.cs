namespace HabiHamAIAPI.Services.Mcp;

/// <summary>
/// Scoped user id for trainer MCP tools (chat agent loop or authenticated MCP HTTP).
/// </summary>
public sealed class TrainerToolExecutionContext
{
    public Guid UserId { get; set; }

    public Guid? TrainerAssistantId { get; set; }
}
