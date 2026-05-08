using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users/me/workouts")]
[Authorize]
public sealed class WorkoutsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public WorkoutsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWorkouts([FromQuery] bool includeHistory = true, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var query = _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == user.Id);

        if (!includeHistory)
        {
            query = query.Where(x => !EF.Functions.ILike(x.SessionCode, "workout::%") || x.IsActive == true);
        }

        var sessions = await query
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(sessions.Select(MapToResponse));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetMyWorkoutHistory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? program,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var query = _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == user.Id
                && EF.Functions.ILike(x.SessionCode, "workout::%")
                && x.IsActive != true);

        if (from.HasValue)
        {
            query = query.Where(x => x.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.Date <= to.Value);
        }

        var normalizedProgram = (program ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(normalizedProgram))
        {
            query = query.Where(x => x.Day == normalizedProgram);
        }

        var sessions = await query
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(sessions.Select(MapToResponse));
    }

    [HttpGet("history/options")]
    public async Task<IActionResult> GetMyWorkoutHistoryOptions(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var options = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == user.Id
                && EF.Functions.ILike(x.SessionCode, "workout::%")
                && x.IsActive != true)
            .GroupBy(x => x.Day)
            .Select(g => g.Key)
            .Where(x => x != null && x != string.Empty)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var mapped = options
            .Select(x => new
            {
                program = x
            })
            .ToList();

        return Ok(mapped);
    }

    [HttpGet("import-template")]
    public IActionResult GetWorkoutImportTemplate()
    {
        return Ok(WorkoutImportTemplateFactory.BuildSessionTemplate());
    }

    [HttpGet("planning/import-template")]
    public IActionResult GetWorkoutPlanningImportTemplate()
    {
        return Ok(WorkoutImportTemplateFactory.BuildTemplate<ImportWorkoutPlanningRequest>());
    }

    [HttpPost("planning/import")]
    public async Task<IActionResult> ImportWorkoutPlanning([FromBody] ImportWorkoutPlanningRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var now = DateTime.UtcNow;

        foreach (var program in request.Programs)
        {
            var day = (program.Day ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(day))
            {
                skipped++;
                continue;
            }

            var codeSource = string.IsNullOrWhiteSpace(program.ProgramCode) ? day : program.ProgramCode;
            var normalizedCode = $"program::{Slugify(codeSource)}";

            var existing = await _dbContext.WorkoutSessions
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.SessionCode == normalizedCode, cancellationToken);

            if (existing is null)
            {
                existing = WorkoutSession.Create(user.Id, normalizedCode, DateOnly.FromDateTime(DateTime.UtcNow), day, program.Notes ?? string.Empty, now, false);
                _dbContext.WorkoutSessions.Add(existing);
                created++;
            }
            else
            {
                existing.UpdateDetails(DateOnly.FromDateTime(DateTime.UtcNow), day, program.Notes ?? string.Empty, now);
                existing.IsActive = false;
                await _dbContext.WorkoutExercises
                    .Where(x => x.SessionId == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);
                updated++;
            }

            for (var index = 0; index < program.Exercises.Count; index++)
            {
                var importExercise = program.Exercises[index];
                var exerciseName = (importExercise.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(exerciseName))
                {
                    continue;
                }

                var exercise = WorkoutExercise.Create(
                    existing.Id,
                    exerciseName,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sourceExerciseId = string.IsNullOrWhiteSpace(importExercise.SourceExerciseId) ? null : importExercise.SourceExerciseId.Trim(),
                        comment = (importExercise.Comment ?? string.Empty).Trim()
                    }),
                    index + 1);

                _dbContext.WorkoutExercises.Add(exercise);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { created, updated, skipped });
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetMyWorkoutById(Guid sessionId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var session = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.Id == sessionId && x.UserId == user.Id)
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(cancellationToken);

        return session is null ? NotFound(new { message = "Workout session not found." }) : Ok(MapToResponse(session));
    }

    [HttpPost]
    public async Task<IActionResult> UpsertMyWorkout([FromBody] UpsertWorkoutSessionRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        if (string.IsNullOrWhiteSpace(request.SessionCode))
        {
            return BadRequest(new { message = "SessionCode is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Day))
        {
            return BadRequest(new { message = "Day is required." });
        }

        var normalizedCode = request.SessionCode.Trim();
        var normalizedDay = request.Day.Trim();
        var normalizedNotes = request.Notes ?? string.Empty;
        var now = DateTime.UtcNow;
        var isWorkoutLog = normalizedCode.StartsWith("workout::", StringComparison.OrdinalIgnoreCase);

        var existing = await _dbContext.WorkoutSessions
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.SessionCode == normalizedCode, cancellationToken);

        if (existing is null)
        {
            var isActive = isWorkoutLog && (request.IsActive ?? true);
            existing = WorkoutSession.Create(user.Id, normalizedCode, request.Date, normalizedDay, normalizedNotes, now, isActive);

            _dbContext.WorkoutSessions.Add(existing);
        }
        else
        {
            existing.UpdateDetails(request.Date, normalizedDay, normalizedNotes, now);
            if (!isWorkoutLog)
            {
                existing.IsActive = false;
            }
            else
            {
                existing.IsActive = request.IsActive ?? existing.IsActive;
            }

            // Use direct set-based delete to avoid tracked-collection delete races.
            await _dbContext.WorkoutExercises
                .Where(x => x.SessionId == existing.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (isWorkoutLog && existing.IsActive)
        {
            // Без StringComparison: перегрузка с OrdinalIgnoreCase не транслируется в SQL для ExecuteUpdate.
            var existingId = existing.Id;
            await _dbContext.WorkoutSessions
                .Where(x => x.UserId == user.Id
                    && EF.Functions.ILike(x.SessionCode, "workout::%")
                    && x.Id != existingId
                    && x.IsActive)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), cancellationToken);
        }

        for (var exerciseIndex = 0; exerciseIndex < request.Exercises.Count; exerciseIndex++)
        {
            var exerciseRequest = request.Exercises[exerciseIndex];
            if (string.IsNullOrWhiteSpace(exerciseRequest.Name))
            {
                continue;
            }

            var exercise = WorkoutExercise.Create(
                existing.Id,
                exerciseRequest.Name,
                exerciseRequest.Meta,
                exerciseIndex + 1);

            var builtSets = new List<WorkoutSet>();
            for (var setIndex = 0; setIndex < exerciseRequest.Sets.Count; setIndex++)
            {
                var setRequest = exerciseRequest.Sets[setIndex];
                builtSets.Add(WorkoutSet.Create(
                    exercise.Id,
                    setRequest.Weight,
                    setRequest.Reps,
                    setRequest.Rpe,
                    setIndex + 1));
            }

            exercise.SetSets(builtSets);
            _dbContext.WorkoutExercises.Add(exercise);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var refreshed = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .FirstAsync(x => x.Id == existing.Id, cancellationToken);

        return Ok(MapToResponse(refreshed));
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> DeleteMyWorkout(Guid sessionId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var session = await _dbContext.WorkoutSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == user.Id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Workout session not found." });
        }

        _dbContext.WorkoutSessions.Remove(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Workout session deleted." });
    }

    [HttpGet("exercises")]
    public async Task<IActionResult> GetMyWorkoutExercises(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var exercises = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Session != null && x.Session.UserId == user.Id)
            .Include(x => x.Sets.OrderBy(s => s.Order))
            .OrderBy(x => x.Session!.Date)
            .ThenBy(x => x.Order)
            .Select(MapExerciseProjection())
            .ToListAsync(cancellationToken);

        return Ok(exercises);
    }

    [HttpGet("exercises/import-template")]
    public IActionResult GetWorkoutExercisesImportTemplate()
    {
        return Ok(WorkoutImportTemplateFactory.BuildTemplate<ImportWorkoutExercisesRequest>());
    }

    [HttpGet("exercises/export")]
    public async Task<IActionResult> ExportWorkoutExercises(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var exercises = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Session != null && x.Session.UserId == user.Id)
            .Select(x => new { x.Id, x.Name })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var unique = exercises
            .GroupBy(x => x.Name.Trim().ToLower())
            .Select(g => g.First())
            .Select(x => new
            {
                id = x.Id,
                name = x.Name
            })
            .ToList();

        return Ok(new { exercises = unique });
    }

    [HttpPost("exercises/import")]
    public async Task<IActionResult> ImportWorkoutExercises([FromBody] ImportWorkoutExercisesRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var incoming = request.Exercises
            .Select(x => new
            {
                Name = (x.Name ?? string.Empty).Trim(),
                Meta = (x.Meta ?? string.Empty).Trim()
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();

        if (incoming.Count == 0)
        {
            return BadRequest(new { message = "Exercises list is empty." });
        }

        var existingNames = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Session != null && x.Session.UserId == user.Id)
            .Select(x => x.Name.ToLower())
            .Distinct()
            .ToListAsync(cancellationToken);
        var existingSet = existingNames.ToHashSet();

        var created = 0;
        var skipped = 0;
        var now = DateTime.UtcNow;

        for (var index = 0; index < incoming.Count; index++)
        {
            var item = incoming[index];
            var normalized = item.Name.ToLowerInvariant();
            if (existingSet.Contains(normalized))
            {
                skipped++;
                continue;
            }

            var session = WorkoutSession.Create(
                user.Id,
                $"catalog::{Slugify(item.Name)}-{Guid.NewGuid():N}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"Каталог: {item.Name}",
                "Импортировано из JSON",
                now,
                false);

            var exercise = WorkoutExercise.Create(session.Id, item.Name, item.Meta, 1);
            session.Exercises.Add(exercise);
            _dbContext.WorkoutSessions.Add(session);
            existingSet.Add(normalized);
            created++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { created, skipped });
    }

    [HttpGet("exercises/{exerciseId:guid}")]
    public async Task<IActionResult> GetMyWorkoutExercise(Guid exerciseId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var exercise = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Id == exerciseId && x.Session != null && x.Session.UserId == user.Id)
            .Include(x => x.Sets.OrderBy(s => s.Order))
            .Select(MapExerciseProjection())
            .FirstOrDefaultAsync(cancellationToken);

        return exercise is null ? NotFound(new { message = "Exercise not found." }) : Ok(exercise);
    }

    [HttpPost("{sessionId:guid}/exercises")]
    public async Task<IActionResult> CreateWorkoutExercise(Guid sessionId, [FromBody] CreateWorkoutExerciseRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Exercise name is required." });
        }

        var session = await _dbContext.WorkoutSessions
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == user.Id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Workout session not found." });
        }

        var nextOrder = request.Order is > 0
            ? request.Order.Value
            : (session.Exercises.Count == 0 ? 1 : session.Exercises.Max(x => x.Order) + 1);

        var exercise = new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Name = request.Name.Trim(),
            Meta = (request.Meta ?? string.Empty).Trim(),
            Order = nextOrder
        };

        for (var setIndex = 0; setIndex < request.Sets.Count; setIndex++)
        {
            var setRequest = request.Sets[setIndex];
            exercise.Sets.Add(new WorkoutSet
            {
                Id = Guid.NewGuid(),
                ExerciseId = exercise.Id,
                Weight = (setRequest.Weight ?? string.Empty).Trim(),
                Reps = (setRequest.Reps ?? string.Empty).Trim(),
                Rpe = (setRequest.Rpe ?? string.Empty).Trim(),
                Order = setIndex + 1
            });
        }

        _dbContext.WorkoutExercises.Add(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Id == exercise.Id)
            .Include(x => x.Sets.OrderBy(s => s.Order))
            .Select(MapExerciseProjection())
            .FirstAsync(cancellationToken);

        return Ok(created);
    }

    [HttpPut("exercises/{exerciseId:guid}")]
    public async Task<IActionResult> UpdateWorkoutExercise(Guid exerciseId, [FromBody] UpdateWorkoutExerciseRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Exercise name is required." });
        }

        var exercise = await _dbContext.WorkoutExercises
            .Include(x => x.Session)
            .Include(x => x.Sets.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(x => x.Id == exerciseId, cancellationToken);
        if (exercise is null || exercise.Session is null || exercise.Session.UserId != user.Id)
        {
            return NotFound(new { message = "Exercise not found." });
        }

        exercise.Name = request.Name.Trim();
        exercise.Meta = (request.Meta ?? string.Empty).Trim();
        if (request.Order is > 0)
        {
            exercise.Order = request.Order.Value;
        }

        _dbContext.WorkoutSets.RemoveRange(exercise.Sets);
        exercise.Sets.Clear();
        for (var setIndex = 0; setIndex < request.Sets.Count; setIndex++)
        {
            var setRequest = request.Sets[setIndex];
            exercise.Sets.Add(new WorkoutSet
            {
                Id = Guid.NewGuid(),
                ExerciseId = exercise.Id,
                Weight = (setRequest.Weight ?? string.Empty).Trim(),
                Reps = (setRequest.Reps ?? string.Empty).Trim(),
                Rpe = (setRequest.Rpe ?? string.Empty).Trim(),
                Order = setIndex + 1
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.WorkoutExercises
            .AsNoTracking()
            .Where(x => x.Id == exercise.Id)
            .Include(x => x.Sets.OrderBy(s => s.Order))
            .Select(MapExerciseProjection())
            .FirstAsync(cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("exercises/{exerciseId:guid}")]
    public async Task<IActionResult> DeleteWorkoutExercise(Guid exerciseId, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var exercise = await _dbContext.WorkoutExercises
            .Include(x => x.Session)
            .FirstOrDefaultAsync(x => x.Id == exerciseId, cancellationToken);
        if (exercise is null || exercise.Session is null || exercise.Session.UserId != user.Id)
        {
            return NotFound(new { message = "Exercise not found." });
        }

        _dbContext.WorkoutExercises.Remove(exercise);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Exercise deleted." });
    }

    private async Task<AppUser?> GetCurrentUser(CancellationToken cancellationToken)
    {
        var username = User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);
    }

    private static WorkoutSessionResponse MapToResponse(WorkoutSession session)
    {
        return new WorkoutSessionResponse
        {
            Id = session.Id,
            SessionCode = session.SessionCode,
            Date = session.Date,
            Day = session.Day,
            Notes = session.Notes,
            CreatedAtUtc = session.CreatedAtUtc,
            _date = session.Date,
            IsActive = session.IsActive,
            Exercises = session.Exercises
                .OrderBy(x => x.Order)
                .Select(exercise => new WorkoutExerciseResponse
                {
                    Id = exercise.Id,
                    SessionId = exercise.SessionId,
                    Name = exercise.Name,
                    Meta = exercise.Meta,
                    Order = exercise.Order,
                    Sets = exercise.Sets
                        .OrderBy(x => x.Order)
                        .Select(setItem => new WorkoutSetResponse
                        {
                            Id = setItem.Id,
                            Weight = setItem.Weight,
                            Reps = setItem.Reps,
                            Rpe = setItem.Rpe,
                            Order = setItem.Order
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static System.Linq.Expressions.Expression<Func<WorkoutExercise, WorkoutExerciseResponse>> MapExerciseProjection()
    {
        return exercise => new WorkoutExerciseResponse
        {
            Id = exercise.Id,
            SessionId = exercise.SessionId,
            Name = exercise.Name,
            Meta = exercise.Meta,
            Order = exercise.Order,
            Sets = exercise.Sets
                .OrderBy(x => x.Order)
                .Select(setItem => new WorkoutSetResponse
                {
                    Id = setItem.Id,
                    Weight = setItem.Weight,
                    Reps = setItem.Reps,
                    Rpe = setItem.Rpe,
                    Order = setItem.Order
                })
                .ToList()
        };
    }

    private static string Slugify(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^a-z0-9а-яё_-]", string.Empty, RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(normalized) ? $"item-{Guid.NewGuid():N}" : normalized;
    }
}
