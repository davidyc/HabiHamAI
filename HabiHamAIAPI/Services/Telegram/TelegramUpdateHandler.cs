using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HabiHamAIAPI.Services.Telegram;

public sealed class TelegramUpdateHandler : ITelegramUpdateHandler
{
    private readonly ITelegramBotClient _botClient;

    public TelegramUpdateHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is not { } message)
        {
            return;
        }

        var text = message.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        await _botClient.SendMessage(
            message.Chat.Id,
            $"Вы написали: {text}",
            cancellationToken: cancellationToken);
    }
}
