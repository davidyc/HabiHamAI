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
}
