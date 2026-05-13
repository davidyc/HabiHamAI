namespace HabiHamAIAPI.Services.Telegram;

public enum TelegramLinkConsumeStatus
{
    Linked,
    AlreadyLinkedSameChat,
    InvalidOrExpiredToken,
    ChatBelongsToOtherUser
}

public sealed class TelegramConsumeLinkResult
{
    public TelegramLinkConsumeStatus Status { get; init; }
}
