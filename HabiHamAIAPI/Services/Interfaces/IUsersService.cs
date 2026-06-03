using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IUsersService
{
    Task<IActionResult> GetMyProfileAsync(ClaimsPrincipal principal);
    Task<IActionResult> UpdateMyProfileAsync(ClaimsPrincipal principal, UpdateUserProfileRequest request);
    Task<IActionResult> GetMyWeightTrackerAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> UpsertMyWeightTrackerEntryAsync(ClaimsPrincipal principal, UpsertUserWeightEntryRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyWeightTrackerEntryAsync(ClaimsPrincipal principal, Guid entryId, CancellationToken cancellationToken);
    Task<IActionResult> GetMyCategoriesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    // Habits
    Task<IActionResult> GetMyHabitsOverviewAsync(ClaimsPrincipal principal, DateOnly? asOfDate, CancellationToken cancellationToken);
    Task<IActionResult> CreateMyHabitAsync(ClaimsPrincipal principal, CreateUserHabitRequest request, CancellationToken cancellationToken);
    Task<IActionResult> UpdateMyHabitAsync(ClaimsPrincipal principal, Guid habitId, UpdateUserHabitRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyHabitAsync(ClaimsPrincipal principal, Guid habitId, CancellationToken cancellationToken);
    Task<IActionResult> GetMyHabitCheckinsAsync(ClaimsPrincipal principal, Guid habitId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<IActionResult> UpsertMyHabitCheckinAsync(ClaimsPrincipal principal, Guid habitId, UpsertUserHabitCheckinRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyHabitCheckinAsync(ClaimsPrincipal principal, Guid habitId, DateOnly date, CancellationToken cancellationToken);

    // Todos
    Task<IActionResult> GetMyTodosAsync(ClaimsPrincipal principal, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<IActionResult> CreateMyTodoAsync(ClaimsPrincipal principal, CreateUserTodoItemRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyTodoAsync(ClaimsPrincipal principal, Guid todoId, CancellationToken cancellationToken);
    Task<IActionResult> UpsertMyTodoDoneAsync(ClaimsPrincipal principal, Guid todoId, UpsertUserTodoDoneRequest request, CancellationToken cancellationToken);
}
