using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using HabiHamAIAPI.Services.Telegram;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _service;
    private readonly ITelegramUserLinkService _telegramLinkService;

    public UsersController(IUsersService service, ITelegramUserLinkService telegramLinkService)
    {
        _service = service;
        _telegramLinkService = telegramLinkService;
    }

    [HttpGet("me")]
    public Task<IActionResult> GetMyProfile() =>
        _service.GetMyProfileAsync(User);

    [HttpPut("me")]
    public Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request) =>
        _service.UpdateMyProfileAsync(User, request);

    [HttpGet("me/weight-tracker")]
    public Task<IActionResult> GetMyWeightTracker(CancellationToken cancellationToken) =>
        _service.GetMyWeightTrackerAsync(User, cancellationToken);

    [HttpPost("me/weight-tracker")]
    public Task<IActionResult> UpsertMyWeightTrackerEntry([FromBody] UpsertUserWeightEntryRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyWeightTrackerEntryAsync(User, request, cancellationToken);

    [HttpDelete("me/weight-tracker/{entryId:guid}")]
    public Task<IActionResult> DeleteMyWeightTrackerEntry(Guid entryId, CancellationToken cancellationToken) =>
        _service.DeleteMyWeightTrackerEntryAsync(User, entryId, cancellationToken);

    [HttpPost("me/telegram/link")]
    public Task<IActionResult> CreateTelegramLink(CancellationToken cancellationToken) =>
        _telegramLinkService.CreateLinkAsync(User, cancellationToken);

    [HttpDelete("me/telegram")]
    public Task<IActionResult> UnlinkTelegram(CancellationToken cancellationToken) =>
        _telegramLinkService.UnlinkAsync(User, cancellationToken);
}
