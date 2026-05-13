using System.Security.Cryptography;
using System.Text;

namespace HabiHamAIAPI.Services.Telegram;

internal static class TelegramLinkTokenHasher
{
    public static string Sha256HexUtf8(string plainText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
