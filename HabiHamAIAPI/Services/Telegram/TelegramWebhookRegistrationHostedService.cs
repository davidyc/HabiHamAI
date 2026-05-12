using HabiHamAIAPI.Options;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HabiHamAIAPI.Services.Telegram;

/// <summary>
/// On startup: updates the bot command menu (<c>SetMyCommands</c>). When <see cref="TelegramBotOptions.PublicBaseUrl"/> is set, also registers the Telegram webhook to <c>{PublicBaseUrl}/api/telegram/webhook</c>.
/// </summary>
public sealed class TelegramWebhookRegistrationHostedService : IHostedService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IOptions<TelegramBotOptions> _options;
    private readonly ILogger<TelegramWebhookRegistrationHostedService> _logger;

    public TelegramWebhookRegistrationHostedService(
        ITelegramBotClient botClient,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramWebhookRegistrationHostedService> logger)
    {
        _botClient = botClient;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _botClient.SetMyCommands(TelegramBotMenu.BotCommands, cancellationToken: cancellationToken);
        _logger.LogInformation("Telegram bot command menu (SetMyCommands) updated.");

        var opts = _options.Value;
        var baseUrl = opts.PublicBaseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            return;
        }

        var webhookUrl = $"{baseUrl}/api/telegram/webhook";

        await _botClient.SetWebhook(webhookUrl, cancellationToken: cancellationToken);

        _logger.LogInformation("Telegram webhook set to {WebhookUrl}", webhookUrl);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
