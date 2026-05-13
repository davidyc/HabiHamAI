namespace HabiHamAIAPI.Options;

public sealed class TelegramBotOptions
{
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Bot username without @, e.g. MyHabiBot. Required for deep links (t.me/...?start=).
    /// </summary>
    public string BotUsername { get; set; } = string.Empty;

    /// <summary>
    /// Public HTTPS origin of this API (no trailing slash), e.g. https://api.example.com.
    /// When set, the app calls setWebhook on startup to <c>{PublicBaseUrl}/api/telegram/webhook</c>.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
