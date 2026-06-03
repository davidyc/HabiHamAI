using HabiHamAIAPI.Authorization;
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
    [RequirePermission(AppPermissionCatalog.Profile)]
    public Task<IActionResult> GetMyProfile() =>
        _service.GetMyProfileAsync(User);

    [HttpPut("me")]
    [RequirePermission(AppPermissionCatalog.Profile)]
    public Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request) =>
        _service.UpdateMyProfileAsync(User, request);

    [HttpGet("me/weight-tracker")]
    [RequirePermission(AppPermissionCatalog.Progress)]
    public Task<IActionResult> GetMyWeightTracker(CancellationToken cancellationToken) =>
        _service.GetMyWeightTrackerAsync(User, cancellationToken);

    [HttpPost("me/weight-tracker")]
    [RequirePermission(AppPermissionCatalog.Progress)]
    public Task<IActionResult> UpsertMyWeightTrackerEntry([FromBody] UpsertUserWeightEntryRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyWeightTrackerEntryAsync(User, request, cancellationToken);

    [HttpDelete("me/weight-tracker/{entryId:guid}")]
    [RequirePermission(AppPermissionCatalog.Progress)]
    public Task<IActionResult> DeleteMyWeightTrackerEntry(Guid entryId, CancellationToken cancellationToken) =>
        _service.DeleteMyWeightTrackerEntryAsync(User, entryId, cancellationToken);

    [HttpGet("me/categories")]
    public Task<IActionResult> GetMyCategories(CancellationToken cancellationToken) =>
        _service.GetMyCategoriesAsync(User, cancellationToken);

    [HttpGet("me/habits/overview")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> GetMyHabitsOverview([FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken) =>
        _service.GetMyHabitsOverviewAsync(User, asOfDate, cancellationToken);

    [HttpPost("me/habits")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> CreateMyHabit([FromBody] CreateUserHabitRequest request, CancellationToken cancellationToken) =>
        _service.CreateMyHabitAsync(User, request, cancellationToken);

    [HttpPut("me/habits/{habitId:guid}")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> UpdateMyHabit(Guid habitId, [FromBody] UpdateUserHabitRequest request, CancellationToken cancellationToken) =>
        _service.UpdateMyHabitAsync(User, habitId, request, cancellationToken);

    [HttpDelete("me/habits/{habitId:guid}")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> DeleteMyHabit(Guid habitId, CancellationToken cancellationToken) =>
        _service.DeleteMyHabitAsync(User, habitId, cancellationToken);

    [HttpGet("me/habits/{habitId:guid}/checkins")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> GetMyHabitCheckins(Guid habitId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        _service.GetMyHabitCheckinsAsync(User, habitId, from, to, cancellationToken);

    [HttpPost("me/habits/{habitId:guid}/checkins")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> UpsertMyHabitCheckin(Guid habitId, [FromBody] UpsertUserHabitCheckinRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyHabitCheckinAsync(User, habitId, request, cancellationToken);

    [HttpDelete("me/habits/{habitId:guid}/checkins")]
    [RequirePermission(AppPermissionCatalog.Habits)]
    public Task<IActionResult> DeleteMyHabitCheckin(Guid habitId, [FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        _service.DeleteMyHabitCheckinAsync(User, habitId, date, cancellationToken);

    [HttpGet("me/todos")]
    [RequirePermission(AppPermissionCatalog.Todos)]
    public Task<IActionResult> GetMyTodos([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        _service.GetMyTodosAsync(User, from, to, cancellationToken);

    [HttpPost("me/todos")]
    [RequirePermission(AppPermissionCatalog.Todos)]
    public Task<IActionResult> CreateMyTodo([FromBody] CreateUserTodoItemRequest request, CancellationToken cancellationToken) =>
        _service.CreateMyTodoAsync(User, request, cancellationToken);

    [HttpDelete("me/todos/{todoId:guid}")]
    [RequirePermission(AppPermissionCatalog.Todos)]
    public Task<IActionResult> DeleteMyTodo(Guid todoId, CancellationToken cancellationToken) =>
        _service.DeleteMyTodoAsync(User, todoId, cancellationToken);

    [HttpPut("me/todos/{todoId:guid}/done")]
    [RequirePermission(AppPermissionCatalog.Todos)]
    public Task<IActionResult> UpsertMyTodoDone(Guid todoId, [FromBody] UpsertUserTodoDoneRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyTodoDoneAsync(User, todoId, request, cancellationToken);

    [HttpPost("me/telegram/link")]
    [RequirePermission(AppPermissionCatalog.Profile)]
    public Task<IActionResult> CreateTelegramLink(CancellationToken cancellationToken) =>
        _telegramLinkService.CreateLinkAsync(User, cancellationToken);

    [HttpDelete("me/telegram")]
    [RequirePermission(AppPermissionCatalog.Profile)]
    public Task<IActionResult> UnlinkTelegram(CancellationToken cancellationToken) =>
        _telegramLinkService.UnlinkAsync(User, cancellationToken);
}
