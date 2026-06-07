using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
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
                && EF.Functions.Like(x.SessionCode, "workout::%")
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
            .Where(x => x.UserId == userId && EF.Functions.Like(x.SessionCode, "program::%"))
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
                && EF.Functions.Like(x.SessionCode, "workout::%")
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
                && EF.Functions.Like(x.Sport, "Biking")
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

    public (DateOnly From, DateOnly To, int DayCount) ResolveWeeklyPeriod(int? days, string? endingOn)
    {
        var dayCount = days.HasValue
            ? Math.Clamp(days.Value, 1, _options.MaxWeeklyReviewDays)
            : Math.Clamp(_options.DefaultWeeklyReviewDays, 1, _options.MaxWeeklyReviewDays);

        var toDate = TryParseDate(endingOn) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = toDate.AddDays(-(dayCount - 1));
        return (fromDate, toDate, dayCount);
    }

    public async Task<string> ComputeTrainingDataFingerprintAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var strengthStats = await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.Like(x.SessionCode, "workout::%")
                && x.IsActive != true
                && x.Date >= fromDate
                && x.Date <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                MaxUpdated = g.Max(x => x.UpdatedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var bikeStats = await _dbContext.UserBikeActivities
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.Like(x.Sport, "Biking")
                && x.StartTimeUtc >= fromUtc
                && x.StartTimeUtc < toExclusive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                MaxImported = g.Max(x => x.ImportedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var weightStats = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= fromDate && x.Date <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                MaxUpdated = g.Max(x => x.UpdatedAtUtc)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var sCount = strengthStats?.Count ?? 0;
        var sMax = strengthStats?.MaxUpdated ?? DateTime.MinValue;
        var bCount = bikeStats?.Count ?? 0;
        var bMax = bikeStats?.MaxImported ?? DateTime.MinValue;
        var wCount = weightStats?.Count ?? 0;
        var wMax = weightStats?.MaxUpdated ?? DateTime.MinValue;

        return FormattableString.Invariant(
            $"s:{sCount}:{sMax:O}|b:{bCount}:{bMax:O}|w:{wCount}:{wMax:O}");
    }

    public async Task<bool> HasAnyTrainingDataInPeriodAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.WorkoutSessions.AsNoTracking().AnyAsync(
                x => x.UserId == userId
                    && EF.Functions.Like(x.SessionCode, "workout::%")
                    && x.IsActive != true
                    && x.Date >= fromDate
                    && x.Date <= toDate,
                cancellationToken))
        {
            return true;
        }

        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (await _dbContext.UserBikeActivities.AsNoTracking().AnyAsync(
                x => x.UserId == userId
                    && EF.Functions.Like(x.Sport, "Biking")
                    && x.StartTimeUtc >= fromUtc
                    && x.StartTimeUtc < toExclusive,
                cancellationToken))
        {
            return true;
        }

        return await _dbContext.UserWeightEntries.AsNoTracking().AnyAsync(
            x => x.UserId == userId && x.Date >= fromDate && x.Date <= toDate,
            cancellationToken);
    }

    public async Task<string> GetWeeklyTrainingSummaryAsync(
        Guid userId,
        int? days,
        string? endingOn,
        CancellationToken cancellationToken)
    {
        var (fromDate, toDate, dayCount) = ResolveWeeklyPeriod(days, endingOn);
        var prevToDate = fromDate.AddDays(-1);
        var prevFromDate = prevToDate.AddDays(-(dayCount - 1));

        var currentStrength = await LoadCompletedStrengthSessionsAsync(userId, fromDate, toDate, cancellationToken);
        var previousStrength = await LoadCompletedStrengthSessionsAsync(userId, prevFromDate, prevToDate, cancellationToken);

        var currentStrengthSummary = BuildStrengthPeriodSummary(currentStrength);
        var previousStrengthSummary = BuildStrengthPeriodSummary(previousStrength);

        var fromUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var prevFromUtc = DateTime.SpecifyKind(prevFromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var prevToExclusive = DateTime.SpecifyKind(prevToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var currentRides = await LoadBikeActivitiesInRangeAsync(userId, fromUtc, toExclusive, cancellationToken);
        var previousRides = await LoadBikeActivitiesInRangeAsync(userId, prevFromUtc, prevToExclusive, cancellationToken);

        var currentWeight = await LoadWeightEntriesInRangeAsync(userId, fromDate, toDate, cancellationToken);
        var previousWeight = await LoadWeightEntriesInRangeAsync(userId, prevFromDate, prevToDate, cancellationToken);

        var payload = new
        {
            period = new
            {
                from = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                to = toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                days = dayCount
            },
            previousPeriod = new
            {
                from = prevFromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                to = prevToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                days = dayCount
            },
            strength = new
            {
                current = currentStrengthSummary,
                previous = previousStrengthSummary,
                sessionCountDelta = currentStrengthSummary.sessionCount - previousStrengthSummary.sessionCount,
                totalSetsDelta = currentStrengthSummary.totalSets - previousStrengthSummary.totalSets,
                totalVolumeKgDelta = RoundNullable(
                    currentStrengthSummary.totalVolumeKg - previousStrengthSummary.totalVolumeKg)
            },
            cycling = new
            {
                current = SummarizeBikeActivities(currentRides),
                previous = SummarizeBikeActivities(previousRides)
            },
            weight = new
            {
                current = SummarizeWeightEntries(currentWeight),
                previous = SummarizeWeightEntries(previousWeight)
            },
            hint = "Сводка для недельного обзора. Для деталей подходов вызови get_strength_workout_history с from/to из period."
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private async Task<List<WorkoutSession>> LoadCompletedStrengthSessionsAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken) =>
        await _dbContext.WorkoutSessions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.Like(x.SessionCode, "workout::%")
                && x.IsActive != true
                && x.Date >= fromDate
                && x.Date <= toDate)
            .Include(x => x.Exercises.OrderBy(e => e.Order))
            .ThenInclude(x => x.Sets.OrderBy(s => s.Order))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    private async Task<List<BikeActivityRow>> LoadBikeActivitiesInRangeAsync(
        Guid userId,
        DateTime fromUtc,
        DateTime toExclusive,
        CancellationToken cancellationToken) =>
        await _dbContext.UserBikeActivities
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId
                && EF.Functions.Like(x.Sport, "Biking")
                && x.StartTimeUtc >= fromUtc
                && x.StartTimeUtc < toExclusive)
            .OrderBy(x => x.StartTimeUtc)
            .Select(x => new BikeActivityRow(
                x.StartTimeUtc,
                x.DistanceMeters,
                x.TotalSeconds,
                x.AverageHeartRateBpm))
            .ToListAsync(cancellationToken);

    private async Task<List<WeightEntryRow>> LoadWeightEntriesInRangeAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken) =>
        await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .Select(x => new WeightEntryRow(x.Date, x.WeightKg))
            .ToListAsync(cancellationToken);

    private static StrengthPeriodSummary BuildStrengthPeriodSummary(IReadOnlyList<WorkoutSession> sessions)
    {
        var sessionSummaries = new List<object>();
        var exerciseAgg = new Dictionary<string, ExerciseWeekAgg>(StringComparer.OrdinalIgnoreCase);
        var totalSets = 0;
        double totalVolume = 0;

        foreach (var session in sessions)
        {
            var sessionSets = 0;
            double sessionVolume = 0;
            var exerciseBriefs = new List<object>();

            foreach (var exercise in session.Exercises)
            {
                var exSets = 0;
                double exVolume = 0;
                double? bestWeight = null;
                int? bestReps = null;

                foreach (var set in exercise.Sets.OrderBy(s => s.Order))
                {
                    if (!TryParseSetMetrics(set.Weight, set.Reps, out var weight, out var reps))
                    {
                        continue;
                    }

                    exSets++;
                    sessionSets++;
                    totalSets++;
                    var vol = weight * reps;
                    exVolume += vol;
                    sessionVolume += vol;
                    totalVolume += vol;

                    if (bestWeight is null || weight > bestWeight || (Math.Abs(weight - bestWeight.Value) < 0.001 && reps > bestReps))
                    {
                        bestWeight = weight;
                        bestReps = reps;
                    }
                }

                if (exSets == 0)
                {
                    continue;
                }

                exerciseBriefs.Add(new
                {
                    name = exercise.Name,
                    setCount = exSets,
                    volumeKg = Round(exVolume),
                    bestSet = bestWeight is null
                        ? null
                        : new { weightKg = bestWeight, reps = bestReps }
                });

                if (!exerciseAgg.TryGetValue(exercise.Name, out var agg))
                {
                    agg = new ExerciseWeekAgg();
                    exerciseAgg[exercise.Name] = agg;
                }

                agg.SessionCount++;
                agg.SetCount += exSets;
                agg.VolumeKg += exVolume;
                if (bestWeight is not null && (agg.MaxWeightKg is null || weightGreater(bestWeight.Value, agg.MaxWeightKg.Value, bestReps, agg.MaxRepsAtMaxWeight)))
                {
                    agg.MaxWeightKg = bestWeight;
                    agg.MaxRepsAtMaxWeight = bestReps;
                }
            }

            sessionSummaries.Add(new
            {
                date = session.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                program = session.Day,
                exerciseCount = exerciseBriefs.Count,
                setCount = sessionSets,
                volumeKg = Round(sessionVolume),
                exercises = exerciseBriefs
            });
        }

        var highlights = exerciseAgg
            .OrderByDescending(x => x.Value.SetCount)
            .Take(12)
            .Select(x => (object)new
            {
                name = x.Key,
                sessions = x.Value.SessionCount,
                setCount = x.Value.SetCount,
                volumeKg = Round(x.Value.VolumeKg),
                maxWeightKg = x.Value.MaxWeightKg,
                repsAtMaxWeight = x.Value.MaxRepsAtMaxWeight
            })
            .ToList();

        return new StrengthPeriodSummary(
            sessions.Count,
            totalSets,
            sessions.Count > 0 ? Round(totalVolume) : (double?)null,
            sessionSummaries,
            highlights);
    }

    private static bool weightGreater(double w1, double w2, int? r1, int? r2) =>
        w1 > w2 || (Math.Abs(w1 - w2) < 0.001 && (r1 ?? 0) > (r2 ?? 0));

    private static object SummarizeBikeActivities(IReadOnlyList<BikeActivityRow> rides)
    {
        if (rides.Count == 0)
        {
            return new { rideCount = 0, totalDistanceKm = (double?)null, totalDurationMinutes = (double?)null, avgHeartRate = (int?)null };
        }

        double? distanceSum = 0;
        double? durationSum = 0;
        int hrCount = 0;
        int hrSum = 0;

        foreach (var ride in rides)
        {
            if (ride.DistanceMeters.HasValue)
            {
                distanceSum = (distanceSum ?? 0) + ride.DistanceMeters.Value;
            }

            if (ride.TotalSeconds.HasValue)
            {
                durationSum = (durationSum ?? 0) + ride.TotalSeconds.Value;
            }

            if (ride.AverageHeartRateBpm.HasValue)
            {
                hrSum += ride.AverageHeartRateBpm.Value;
                hrCount++;
            }
        }

        return new
        {
            rideCount = rides.Count,
            totalDistanceKm = distanceSum.HasValue ? Round(distanceSum.Value / 1000.0) : (double?)null,
            totalDurationMinutes = durationSum.HasValue ? Round(durationSum.Value / 60.0) : (double?)null,
            avgHeartRate = hrCount > 0 ? (int?)Math.Round(hrSum / (double)hrCount) : null
        };
    }

    private static object? SummarizeWeightEntries(IReadOnlyList<WeightEntryRow> entries)
    {
        if (entries.Count == 0)
        {
            return null;
        }

        var first = entries[0];
        var last = entries[^1];
        var firstKg = (double)first.WeightKg;
        var lastKg = (double)last.WeightKg;
        return new
        {
            entryCount = entries.Count,
            firstDate = first.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            lastDate = last.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            firstKg,
            lastKg,
            changeKg = Round(lastKg - firstKg)
        };
    }

    private static bool TryParseSetMetrics(string? weightRaw, string? repsRaw, out double weight, out int reps)
    {
        weight = 0;
        reps = 0;
        var w = (weightRaw ?? string.Empty).Trim().Replace(',', '.');
        var r = (repsRaw ?? string.Empty).Trim();
        if (!double.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out weight) || weight <= 0)
        {
            return false;
        }

        if (!int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out reps) || reps <= 0)
        {
            return false;
        }

        return true;
    }

    private static double Round(double value) => Math.Round(value, 2);

    private static double? RoundNullable(double? value) =>
        value.HasValue ? Math.Round(value.Value, 2) : null;

    private sealed record StrengthPeriodSummary(
        int sessionCount,
        int totalSets,
        double? totalVolumeKg,
        List<object> sessions,
        List<object> exerciseHighlights);

    private sealed class ExerciseWeekAgg
    {
        public int SessionCount { get; set; }
        public int SetCount { get; set; }
        public double VolumeKg { get; set; }
        public double? MaxWeightKg { get; set; }
        public int? MaxRepsAtMaxWeight { get; set; }
    }

    private sealed record BikeActivityRow(
        DateTime StartTimeUtc,
        double? DistanceMeters,
        double? TotalSeconds,
        int? AverageHeartRateBpm);

    private sealed record WeightEntryRow(DateOnly Date, decimal WeightKg);

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
