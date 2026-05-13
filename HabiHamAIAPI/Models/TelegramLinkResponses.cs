namespace HabiHamAIAPI.Models;

public sealed class TelegramLinkStartResponse
{
    public string DeepLinkUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
