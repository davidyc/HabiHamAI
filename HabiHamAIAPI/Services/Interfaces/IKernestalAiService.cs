namespace HabiHamAIAPI.Services;

public interface IKernestalAiService
{
    Task<string> GetCompletionAsync(IReadOnlyList<KernestalAiService.AiChatMessage> messages, CancellationToken cancellationToken);
}
