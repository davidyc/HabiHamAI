using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> GetMyWorkouts(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        var sessions = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(sessions.Select(MapToResponse));
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
}
