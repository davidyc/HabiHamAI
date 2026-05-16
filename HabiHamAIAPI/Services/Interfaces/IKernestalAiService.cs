namespace HabiHamAIAPI.Services;

public interface IKernestalAiService
{
    Task<string> GetCompletionAsync(IReadOnlyList<KernestalAiService.AiChatMessage> messages, CancellationToken cancellationToken);

    Task<KernestalAiService.AiCompletionResult> GetCompletionWithToolsAsync(
        IReadOnlyList<KernestalAiService.AiChatMessage> messages,
        IReadOnlyList<KernestalAiService.AiToolDefinition> tools,
        CancellationToken cancellationToken);
}
