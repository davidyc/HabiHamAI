namespace HabiHamAIAPI.Services.Telegram;

/// <summary>Разбивает длинный ответ на части для лимита Telegram (4096 символов).</summary>
internal static class TelegramMessageSplitter
{
    private const int MaxChunkLength = 4096;

    internal static IReadOnlyList<string> Split(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [""];
        }

        if (text.Length <= MaxChunkLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var remaining = text;
        while (remaining.Length > MaxChunkLength)
        {
            var splitAt = remaining.LastIndexOf('\n', MaxChunkLength - 1);
            if (splitAt < MaxChunkLength / 2)
            {
                splitAt = MaxChunkLength;
            }

            chunks.Add(remaining[..splitAt].TrimEnd());
            remaining = remaining[splitAt..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            chunks.Add(remaining);
        }

        return chunks;
    }
}
