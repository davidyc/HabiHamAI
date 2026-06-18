using HabiHamAIAPI.Services.Ai;

namespace HabiHamAIAPI.Services.Telegram;

public sealed record TelegramTrainerChatResult(bool Success, string? Response, string? ErrorMessage);

public interface ITelegramTrainerChatService
{
    Task<TelegramTrainerChatResult> SendMessageAsync(
        long chatId,
        Guid userId,
        string prompt,
        CancellationToken cancellationToken);

    void ResetDialog(long chatId);
}

public sealed class TelegramTrainerChatService : ITelegramTrainerChatService
{
    private readonly IUserAiChatService _userAiChatService;
    private readonly TelegramChatStateStore _state;

    public TelegramTrainerChatService(IUserAiChatService userAiChatService, TelegramChatStateStore state)
    {
        _userAiChatService = userAiChatService;
        _state = state;
    }

    public async Task<TelegramTrainerChatResult> SendMessageAsync(
        long chatId,
        Guid userId,
        string prompt,
        CancellationToken cancellationToken)
    {
        var trainerId = await _userAiChatService.ResolveTrainerAssistantIdAsync(cancellationToken);
        if (trainerId is null)
        {
            return new TelegramTrainerChatResult(false, null, "AI-тренер временно недоступен. Попробуйте позже.");
        }

        var dialogId = _state.GetTrainerDialogId(chatId);

        try
        {
            var result = await _userAiChatService.SendMessageAsync(
                userId,
                dialogId,
                prompt,
                trainerId,
                cancellationToken);

            _state.SetTrainerDialogId(chatId, result.DialogId);
            return new TelegramTrainerChatResult(true, result.Response, null);
        }
        catch (InvalidOperationException ex)
        {
            return new TelegramTrainerChatResult(false, null, ex.Message);
        }
    }

    public void ResetDialog(long chatId) => _state.ClearTrainerDialogId(chatId);
}
