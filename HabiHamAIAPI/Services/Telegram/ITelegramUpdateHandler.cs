using Telegram.Bot.Types;

namespace HabiHamAIAPI.Services.Telegram;

public interface ITelegramUpdateHandler
{
    Task HandleAsync(Update update, CancellationToken cancellationToken);
}
