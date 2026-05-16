using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Mcp;

public sealed class TrainerDataQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly AppDbContext _dbContext;
    private readonly TrainerMcpOptions _options;

    public TrainerDataQueryService(AppDbContext dbContext, IOptions<TrainerMcpOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<string> GetStrengthWorkoutHistoryAsync(
        Guid userId,
        string? from,
        string? to,
        string? program,
        string? exerciseNameContains,
        int? limit,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveDateRange(from, to);
        var take = Clamp(limit, 1, _options.MaxStrengthSessions);

        var query = _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.ILike(x.SessionCode, "workout::%")
                && x.IsActive != true
                && x.Date >= fromDate
                && x.Date <= toDate);

        var programFilter = (program ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(programFilter))
        {
            query = query.Where(x => x.Day == programFilter);
        }

        var sessions = await query
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        var exerciseFilter = (exerciseNameContains ?? string.Empty).Trim();
        var payload = sessions.Select(s => new
        {
            id = s.Id,
            date = s.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            program = s.Day,
            notes = string.IsNullOrWhiteSpace(s.Notes) ? null : s.Notes.Trim(),
            exercises = s.Exercises
                .Where(e => string.IsNullOrWhiteSpace(exerciseFilter)
                    || e.Name.Contains(exerciseFilter, StringComparison.OrdinalIgnoreCase))
                .Select(e => new
                {
                    name = e.Name,
                    sets = e.Sets
                        .OrderBy(set => set.Order)
                        .Select(set => new
                        {
                            weight = set.Weight,
                            reps = set.Reps,
                            rpe = string.IsNullOrWhiteSpace(set.Rpe) ? null : set.Rpe
                        })
                        .ToList()
                })
                .Where(e => e.sets.Count > 0 || string.IsNullOrWhiteSpace(exerciseFilter))
                .ToList()
        });

        return JsonSerializer.Serialize(new { from = fromDate, to = toDate, workouts = payload }, JsonOptions);
    }

    public async Task<string> GetStrengthProgramsAsync(Guid userId, int? limit, CancellationToken cancellationToken)
    {
        var take = Clamp(limit, 1, _options.MaxPrograms);
        var sessions = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId && EF.Functions.ILike(x.SessionCode, "program::%"))
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        var payload = sessions.Select(s => new
        {
            id = s.Id,
            sessionCode = s.SessionCode,
            day = s.Day,
            notes = string.IsNullOrWhiteSpace(s.Notes) ? null : s.Notes.Trim(),
            exercises = s.Exercises.Select(e => new
            {
                name = e.Name,
                plannedSets = e.Sets.Count,
                sets = e.Sets.OrderBy(set => set.Order).Select(set => new { set.Weight, set.Reps, set.Rpe })
            })
        });

        return JsonSerializer.Serialize(new { programs = payload }, JsonOptions);
    }

    public async Task<string> GetActiveStrengthWorkoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.ILike(x.SessionCode, "workout::%")
                && x.IsActive)
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return JsonSerializer.Serialize(new { active = (object?)null }, JsonOptions);
        }

        var payload = new
        {
            active = new
            {
                id = session.Id,
                date = session.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                program = session.Day,
                notes = session.Notes,
                exercises = session.Exercises.Select(e => new
                {
                    e.Name,
                    sets = e.Sets.OrderBy(s => s.Order).Select(s => new { s.Weight, s.Reps, s.Rpe })
                })
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public async Task<string> GetBikeActivitiesAsync(
        Guid userId,
        string? from,
        string? to,
        int? limit,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveDateRange(from, to);
        var take = Clamp(limit, 1, _options.MaxBikeActivities);

        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var list = await _dbContext.UserBikeActivities
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.ILike(x.Sport, "Biking")
                && x.StartTimeUtc >= fromUtc
                && x.StartTimeUtc < toExclusive)
            .OrderByDescending(x => x.StartTimeUtc)
            .Take(take)
            .Select(x => new
            {
                id = x.Id,
                startTimeUtc = x.StartTimeUtc,
                distanceKm = x.DistanceMeters.HasValue ? Math.Round(x.DistanceMeters.Value / 1000.0, 2) : (double?)null,
                durationMinutes = x.TotalSeconds.HasValue ? Math.Round(x.TotalSeconds.Value / 60.0, 1) : (double?)null,
                calories = x.Calories,
                avgHeartRate = x.AverageHeartRateBpm,
                maxHeartRate = x.MaxHeartRateBpm,
                trackpointCount = x.TrackpointCount,
                notes = x.Notes
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(new { from = fromDate, to = toDate, rides = list }, JsonOptions);
    }

    public async Task<string> GetBikeActivityAsync(Guid userId, Guid activityId, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.UserBikeActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == activityId && x.UserId == userId, cancellationToken);

        if (activity is null)
        {
            return JsonSerializer.Serialize(new { error = "Activity not found." }, JsonOptions);
        }

        var payload = new
        {
            id = activity.Id,
            sport = activity.Sport,
            startTimeUtc = activity.StartTimeUtc,
            distanceKm = activity.DistanceMeters.HasValue ? Math.Round(activity.DistanceMeters.Value / 1000.0, 2) : (double?)null,
            durationMinutes = activity.TotalSeconds.HasValue ? Math.Round(activity.TotalSeconds.Value / 60.0, 1) : (double?)null,
            calories = activity.Calories,
            avgHeartRate = activity.AverageHeartRateBpm,
            maxHeartRate = activity.MaxHeartRateBpm,
            intensity = activity.Intensity,
            notes = activity.Notes,
            trackpointCount = activity.TrackpointCount
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public async Task<string> GetWeightEntriesAsync(
        Guid userId,
        string? from,
        string? to,
        int? limit,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate) = ResolveDateRange(from, to);
        var take = Clamp(limit, 1, _options.MaxWeightEntries);

        var entries = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= fromDate && x.Date <= toDate)
            .OrderByDescending(x => x.Date)
            .Take(take)
            .Select(x => new
            {
                date = x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                weightKg = x.WeightKg
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(new { from = fromDate, to = toDate, entries }, JsonOptions);
    }

    public async Task<string> GetTrainerProfileAsync(
        Guid userId,
        Guid? trainerAssistantId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return JsonSerializer.Serialize(new { error = "User not found." }, JsonOptions);
        }

        Dictionary<string, string> extras = new();
        if (trainerAssistantId is { } aid && aid != Guid.Empty)
        {
            var row = await _dbContext.UserAiAssistantExtras
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AiAssistantId == aid, cancellationToken);

            if (row is not null && !string.IsNullOrWhiteSpace(row.ValuesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ValuesJson);
                    if (parsed is not null)
                    {
                        extras = parsed;
                    }
                }
                catch
                {
                }
            }
        }

        var payload = new
        {
            heightCm = user.HeightCm,
            weightKg = user.WeightKg,
            birthDate = user.BirthDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            about = user.About,
            aiSummary = user.AiSummary,
            trainerExtras = extras
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private (DateOnly From, DateOnly To) ResolveDateRange(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var defaultFrom = today.AddDays(-_options.DefaultHistoryDays);

        var fromDate = TryParseDate(from) ?? defaultFrom;
        var toDate = TryParseDate(to) ?? today;
        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        return (fromDate, toDate);
    }

    private static DateOnly? TryParseDate(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    private static int Clamp(int? value, int min, int max)
    {
        if (!value.HasValue)
        {
            return max;
        }

        return Math.Clamp(value.Value, min, max);
    }
}
