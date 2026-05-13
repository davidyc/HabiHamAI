namespace HabiHamAIAPI.Models;

public sealed class TelegramLinkToken
{
    public Guid Id { get; set; }
    public string TokenHashSha256Hex { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
}
