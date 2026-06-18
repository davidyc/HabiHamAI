using System.Collections.Concurrent;

namespace HabiHamAIAPI.Services.Telegram;

public enum TelegramChatDialogState
{
    Idle,
    AwaitingWeightKg,
}

/// <summary>Память диалога по chat_id (один инстанс API; при масштабировании нужен общий стор).</summary>
public sealed class TelegramChatStateStore
{
    private readonly ConcurrentDictionary<long, TelegramChatDialogState> _byChat = new();
    private readonly ConcurrentDictionary<long, Guid> _trainerDialogByChat = new();

    public TelegramChatDialogState Get(long chatId) =>
        _byChat.GetValueOrDefault(chatId, TelegramChatDialogState.Idle);

    public void Set(long chatId, TelegramChatDialogState state)
    {
        if (state == TelegramChatDialogState.Idle)
        {
            _byChat.TryRemove(chatId, out _);
        }
        else
        {
            _byChat[chatId] = state;
        }
    }

    public Guid? GetTrainerDialogId(long chatId) =>
        _trainerDialogByChat.TryGetValue(chatId, out var id) ? id : null;

    public void SetTrainerDialogId(long chatId, Guid dialogId) => _trainerDialogByChat[chatId] = dialogId;

    public void ClearTrainerDialogId(long chatId) => _trainerDialogByChat.TryRemove(chatId, out _);
}
