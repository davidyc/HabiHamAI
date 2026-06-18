namespace HabiHamAIAPI.Services.Ai;

public sealed record UserAiChatSendResult(Guid DialogId, string? DialogTitle, string Response);

public interface IUserAiChatService
{
    Task<UserAiChatSendResult> SendMessageAsync(
        Guid userId,
        Guid? dialogId,
        string prompt,
        Guid? assistantId,
        CancellationToken cancellationToken,
        string? model = null);

    Task<string> CompleteTrainerPromptAsync(
        Guid userId,
        IReadOnlyList<KernestalAiService.AiChatMessage> messages,
        CancellationToken cancellationToken,
        string? model = null);

    Task<Guid?> ResolveTrainerAssistantIdAsync(CancellationToken cancellationToken);
}
