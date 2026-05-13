using System.Text.Json;
using HabiHamAIAPI.Options;
using HabiHamAIAPI.Services.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("api/telegram")]
public sealed class TelegramWebhookController : ControllerBase
{
    private readonly IOptions<TelegramBotOptions> _options;

    public TelegramWebhookController(IOptions<TelegramBotOptions> options)
    {
        _options = options;
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(
        CancellationToken cancellationToken,
        [FromServices] ILogger<TelegramWebhookController> logger,
        [FromServices] ITelegramUpdateHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.BotToken))
        {
            logger.LogWarning("Telegram webhook: BotToken пустой в конфигурации.");
            return NotFound();
        }

        if (handler is null)
        {
            logger.LogWarning(
                "Telegram webhook: ITelegramUpdateHandler не зарегистрирован (при старте не было токена бота). Перезапустите API с TELEGRAM_BOT_TOKEN / Telegram:BotToken.");
            return NotFound();
        }

        Update? update;
        try
        {
            update = await JsonSerializer.DeserializeAsync(
                Request.Body,
                JsonBotSerializerContext.Default.Update,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Telegram webhook: не удалось разобрать JSON апдейта.");
            return BadRequest();
        }

        if (update is null)
        {
            logger.LogWarning("Telegram webhook: тело запроса пустое или не Update.");
            return BadRequest();
        }

        logger.LogInformation("Telegram webhook: update {UpdateId}, тип {UpdateType}", update.Id, update.Type);
        await handler.HandleAsync(update, cancellationToken);
        return Ok();
    }
}
