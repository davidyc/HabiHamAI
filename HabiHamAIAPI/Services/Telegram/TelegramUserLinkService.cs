using System.Security.Claims;
using System.Security.Cryptography;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Telegram;

public sealed class TelegramUserLinkService : ITelegramUserLinkService
{
    private const int TokenTtlMinutes = 15;
    private const int TokenBytes = 32;
    private const int MaxStartPayloadChars = 64;

    private readonly AppDbContext _dbContext;
    private readonly IOptions<TelegramBotOptions> _options;

    public TelegramUserLinkService(AppDbContext dbContext, IOptions<TelegramBotOptions> options)
    {
        _dbContext = dbContext;
        _options = options;
    }

    public async Task<IActionResult> CreateLinkAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var botUsername = _options.Value.BotUsername.Trim().TrimStart('@');
        if (string.IsNullOrEmpty(botUsername))
        {
            return new BadRequestObjectResult(new
            {
                message = "Telegram bot username is not configured. Set Telegram:BotUsername or TELEGRAM_BOT_USERNAME (without @)."
            });
        }

        var plainToken = GenerateUrlSafeToken();
        if (plainToken.Length > MaxStartPayloadChars)
        {
            return new BadRequestObjectResult(new { message = "Generated link token is too long for Telegram; contact support." });
        }

        var hash = TelegramLinkTokenHasher.Sha256HexUtf8(plainToken);
        var now = DateTime.UtcNow;

        await _dbContext.TelegramLinkTokens
            .Where(t => t.UserId == user.Id && t.ConsumedAtUtc == null)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.TelegramLinkTokens.Add(new TelegramLinkToken
        {
            Id = Guid.NewGuid(),
            TokenHashSha256Hex = hash,
            UserId = user.Id,
            ExpiresAtUtc = now.AddMinutes(TokenTtlMinutes)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var deepLinkUrl = $"https://t.me/{Uri.EscapeDataString(botUsername)}?start={Uri.EscapeDataString(plainToken)}";
        return new OkObjectResult(new TelegramLinkStartResponse
        {
            DeepLinkUrl = deepLinkUrl,
            ExpiresAtUtc = now.AddMinutes(TokenTtlMinutes)
        });
    }

    public async Task<IActionResult> UnlinkAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        user.TelegramChatId = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Telegram unlinked." });
    }

    public async Task<TelegramConsumeLinkResult> TryConsumeStartPayloadAsync(string payload, long chatId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > MaxStartPayloadChars)
        {
            return new TelegramConsumeLinkResult { Status = TelegramLinkConsumeStatus.InvalidOrExpiredToken };
        }

        var hash = TelegramLinkTokenHasher.Sha256HexUtf8(payload.Trim());
        var now = DateTime.UtcNow;

        var row = await _dbContext.TelegramLinkTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHashSha256Hex == hash && t.ConsumedAtUtc == null && t.ExpiresAtUtc > now,
                cancellationToken);
        if (row?.User is null)
        {
            return new TelegramConsumeLinkResult { Status = TelegramLinkConsumeStatus.InvalidOrExpiredToken };
        }

        var user = row.User;

        var otherWithChat = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TelegramChatId == chatId && u.Id != user.Id, cancellationToken);
        if (otherWithChat is not null)
        {
            return new TelegramConsumeLinkResult { Status = TelegramLinkConsumeStatus.ChatBelongsToOtherUser };
        }

        if (user.TelegramChatId == chatId)
        {
            row.ConsumedAtUtc = now;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new TelegramConsumeLinkResult { Status = TelegramLinkConsumeStatus.AlreadyLinkedSameChat };
        }

        user.TelegramChatId = chatId;
        row.ConsumedAtUtc = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TelegramConsumeLinkResult { Status = TelegramLinkConsumeStatus.Linked };
    }

    private static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private async Task<AppUser?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);
    }
}
