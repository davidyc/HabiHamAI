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

    [HttpGet("me/categories")]
    public Task<IActionResult> GetMyCategories(CancellationToken cancellationToken) =>
        _service.GetMyCategoriesAsync(User, cancellationToken);

    // Habits
    [HttpGet("me/habits/overview")]
    public Task<IActionResult> GetMyHabitsOverview([FromQuery] DateOnly? asOfDate, CancellationToken cancellationToken) =>
        _service.GetMyHabitsOverviewAsync(User, asOfDate, cancellationToken);

    [HttpPost("me/habits")]
    public Task<IActionResult> CreateMyHabit([FromBody] CreateUserHabitRequest request, CancellationToken cancellationToken) =>
        _service.CreateMyHabitAsync(User, request, cancellationToken);

    [HttpDelete("me/habits/{habitId:guid}")]
    public Task<IActionResult> DeleteMyHabit(Guid habitId, CancellationToken cancellationToken) =>
        _service.DeleteMyHabitAsync(User, habitId, cancellationToken);

    [HttpGet("me/habits/{habitId:guid}/checkins")]
    public Task<IActionResult> GetMyHabitCheckins(Guid habitId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        _service.GetMyHabitCheckinsAsync(User, habitId, from, to, cancellationToken);

    [HttpPost("me/habits/{habitId:guid}/checkins")]
    public Task<IActionResult> UpsertMyHabitCheckin(Guid habitId, [FromBody] UpsertUserHabitCheckinRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyHabitCheckinAsync(User, habitId, request, cancellationToken);

    [HttpDelete("me/habits/{habitId:guid}/checkins")]
    public Task<IActionResult> DeleteMyHabitCheckin(Guid habitId, [FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        _service.DeleteMyHabitCheckinAsync(User, habitId, date, cancellationToken);

    // Todos
    [HttpGet("me/todos")]
    public Task<IActionResult> GetMyTodos([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        _service.GetMyTodosAsync(User, from, to, cancellationToken);

    [HttpPost("me/todos")]
    public Task<IActionResult> CreateMyTodo([FromBody] CreateUserTodoItemRequest request, CancellationToken cancellationToken) =>
        _service.CreateMyTodoAsync(User, request, cancellationToken);

    [HttpDelete("me/todos/{todoId:guid}")]
    public Task<IActionResult> DeleteMyTodo(Guid todoId, CancellationToken cancellationToken) =>
        _service.DeleteMyTodoAsync(User, todoId, cancellationToken);

    [HttpPut("me/todos/{todoId:guid}/done")]
    public Task<IActionResult> UpsertMyTodoDone(Guid todoId, [FromBody] UpsertUserTodoDoneRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyTodoDoneAsync(User, todoId, request, cancellationToken);

    [HttpPost("me/telegram/link")]
    public Task<IActionResult> CreateTelegramLink(CancellationToken cancellationToken) =>
        _telegramLinkService.CreateLinkAsync(User, cancellationToken);

    [HttpDelete("me/telegram")]
    public Task<IActionResult> UnlinkTelegram(CancellationToken cancellationToken) =>
        _telegramLinkService.UnlinkAsync(User, cancellationToken);
}
