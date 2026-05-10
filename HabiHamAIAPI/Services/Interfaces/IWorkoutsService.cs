using System.Security.Claims;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IWorkoutsService
{
    Task<IActionResult> GetMyWorkoutsAsync(ClaimsPrincipal principal, bool includeHistory, CancellationToken cancellationToken);
    Task<IActionResult> GetMyWorkoutHistoryAsync(ClaimsPrincipal principal, DateOnly? from, DateOnly? to, string? program, CancellationToken cancellationToken);
    Task<IActionResult> GetMyWorkoutHistoryOptionsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    IActionResult GetWorkoutImportTemplate();
    IActionResult GetWorkoutPlanningImportTemplate();
    Task<IActionResult> ImportWorkoutPlanningAsync(ClaimsPrincipal principal, ImportWorkoutPlanningRequest request, CancellationToken cancellationToken);
    Task<IActionResult> GetMyWorkoutByIdAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken cancellationToken);
    Task<IActionResult> UpsertMyWorkoutAsync(ClaimsPrincipal principal, UpsertWorkoutSessionRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteMyWorkoutAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken cancellationToken);
    Task<IActionResult> GetMyWorkoutExercisesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    IActionResult GetWorkoutExercisesImportTemplate();
    Task<IActionResult> ExportWorkoutExercisesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<IActionResult> ImportWorkoutExercisesAsync(ClaimsPrincipal principal, ImportWorkoutExercisesRequest request, CancellationToken cancellationToken);
    Task<IActionResult> GetMyWorkoutExerciseAsync(ClaimsPrincipal principal, Guid exerciseId, CancellationToken cancellationToken);
    Task<IActionResult> CreateWorkoutExerciseAsync(ClaimsPrincipal principal, Guid sessionId, CreateWorkoutExerciseRequest request, CancellationToken cancellationToken);
    Task<IActionResult> UpdateWorkoutExerciseAsync(ClaimsPrincipal principal, Guid exerciseId, UpdateWorkoutExerciseRequest request, CancellationToken cancellationToken);
    Task<IActionResult> DeleteWorkoutExerciseAsync(ClaimsPrincipal principal, Guid exerciseId, CancellationToken cancellationToken);
}
