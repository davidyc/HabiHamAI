using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users/me/workouts")]
[Authorize]
public sealed class WorkoutsController : ControllerBase
{
    private readonly IWorkoutsService _service;

    public WorkoutsController(IWorkoutsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> GetMyWorkouts([FromQuery] bool includeHistory = true, CancellationToken cancellationToken = default) =>
        _service.GetMyWorkoutsAsync(User, includeHistory, cancellationToken);

    [HttpGet("history")]
    public Task<IActionResult> GetMyWorkoutHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? program,
        CancellationToken cancellationToken) =>
        _service.GetMyWorkoutHistoryAsync(User, from, to, program, cancellationToken);

    [HttpGet("history/options")]
    public Task<IActionResult> GetMyWorkoutHistoryOptions(CancellationToken cancellationToken) =>
        _service.GetMyWorkoutHistoryOptionsAsync(User, cancellationToken);

    [HttpGet("import-template")]
    public IActionResult GetWorkoutImportTemplate() =>
        _service.GetWorkoutImportTemplate();

    [HttpGet("planning/import-template")]
    public IActionResult GetWorkoutPlanningImportTemplate() =>
        _service.GetWorkoutPlanningImportTemplate();

    [HttpPost("planning/import")]
    public Task<IActionResult> ImportWorkoutPlanning([FromBody] ImportWorkoutPlanningRequest request, CancellationToken cancellationToken) =>
        _service.ImportWorkoutPlanningAsync(User, request, cancellationToken);

    [HttpGet("{sessionId:guid}")]
    public Task<IActionResult> GetMyWorkoutById(Guid sessionId, CancellationToken cancellationToken) =>
        _service.GetMyWorkoutByIdAsync(User, sessionId, cancellationToken);

    [HttpPost]
    public Task<IActionResult> UpsertMyWorkout([FromBody] UpsertWorkoutSessionRequest request, CancellationToken cancellationToken) =>
        _service.UpsertMyWorkoutAsync(User, request, cancellationToken);

    [HttpDelete("{sessionId:guid}")]
    public Task<IActionResult> DeleteMyWorkout(Guid sessionId, CancellationToken cancellationToken) =>
        _service.DeleteMyWorkoutAsync(User, sessionId, cancellationToken);

    [HttpGet("exercises")]
    public Task<IActionResult> GetMyWorkoutExercises(CancellationToken cancellationToken) =>
        _service.GetMyWorkoutExercisesAsync(User, cancellationToken);

    [HttpGet("exercises/import-template")]
    public IActionResult GetWorkoutExercisesImportTemplate() =>
        _service.GetWorkoutExercisesImportTemplate();

    [HttpGet("exercises/export")]
    public Task<IActionResult> ExportWorkoutExercises(CancellationToken cancellationToken) =>
        _service.ExportWorkoutExercisesAsync(User, cancellationToken);

    [HttpPost("exercises/import")]
    public Task<IActionResult> ImportWorkoutExercises([FromBody] ImportWorkoutExercisesRequest request, CancellationToken cancellationToken) =>
        _service.ImportWorkoutExercisesAsync(User, request, cancellationToken);

    [HttpGet("exercises/{exerciseId:guid}")]
    public Task<IActionResult> GetMyWorkoutExercise(Guid exerciseId, CancellationToken cancellationToken) =>
        _service.GetMyWorkoutExerciseAsync(User, exerciseId, cancellationToken);

    [HttpPost("{sessionId:guid}/exercises")]
    public Task<IActionResult> CreateWorkoutExercise(Guid sessionId, [FromBody] CreateWorkoutExerciseRequest request, CancellationToken cancellationToken) =>
        _service.CreateWorkoutExerciseAsync(User, sessionId, request, cancellationToken);

    [HttpPut("exercises/{exerciseId:guid}")]
    public Task<IActionResult> UpdateWorkoutExercise(Guid exerciseId, [FromBody] UpdateWorkoutExerciseRequest request, CancellationToken cancellationToken) =>
        _service.UpdateWorkoutExerciseAsync(User, exerciseId, request, cancellationToken);

    [HttpDelete("exercises/{exerciseId:guid}")]
    public Task<IActionResult> DeleteWorkoutExercise(Guid exerciseId, CancellationToken cancellationToken) =>
        _service.DeleteWorkoutExerciseAsync(User, exerciseId, cancellationToken);
}
