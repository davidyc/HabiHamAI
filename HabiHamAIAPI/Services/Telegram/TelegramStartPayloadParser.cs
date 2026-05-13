namespace HabiHamAIAPI.Services.Telegram;

internal static class TelegramStartPayloadParser
{
    /// <summary>
    /// Returns true if the message is a /start command (optionally with @botname). When true, <paramref name="payload"/> is non-null if a deep-link argument is present.
    /// </summary>
    public static bool TryParse(string text, out string? payload)
    {
        payload = null;
        var trimmed = text.Trim();
        if (trimmed.Length < 6 || trimmed[0] != '/')
        {
            return false;
        }

        ReadOnlySpan<char> rest = trimmed.AsSpan(1);
        if (!rest.StartsWith("start", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        rest = rest[5..];
        if (rest.StartsWith("@"))
        {
            var spaceIdx = rest.IndexOf(' ');
            if (spaceIdx < 0)
            {
                return true;
            }

            rest = rest[(spaceIdx + 1)..].TrimStart();
        }
        else if (rest.StartsWith(" "))
        {
            rest = rest.TrimStart();
        }
        else if (rest.Length > 0)
        {
            return false;
        }

        if (rest.Length == 0)
        {
            return true;
        }

        payload = rest.ToString();
        return true;
    }
}
