using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services.Telegram;

public interface ITelegramUserLinkService
{
    Task<IActionResult> CreateLinkAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<IActionResult> UnlinkAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<TelegramConsumeLinkResult> TryConsumeStartPayloadAsync(string payload, long chatId, CancellationToken cancellationToken);
}
