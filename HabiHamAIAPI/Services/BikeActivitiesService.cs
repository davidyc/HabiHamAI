using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services.BikeActivities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class BikeActivitiesService : IBikeActivitiesService
{
    private const int MaxTrackpointsReturned = 5000;
    private readonly AppDbContext _dbContext;

    public BikeActivitiesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> ImportTcxAsync(ClaimsPrincipal principal, IFormFile? file, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        if (file is null || file.Length == 0)
        {
            return new BadRequestObjectResult(new { message = "Выберите файл TCX." });
        }

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".tcx", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(new { message = "Ожидается файл с расширением .tcx." });
        }

        await using var stream = file.OpenReadStream();
        TcxActivityParser.ParsedActivity parsed;
        try
        {
            parsed = TcxActivityParser.Parse(stream);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { message = ex.Message });
        }

        if (!string.Equals(parsed.Sport, "Biking", StringComparison.OrdinalIgnoreCase))
        {
            return new BadRequestObjectResult(new
            {
                message = "Вкладка «Велотренировки» принимает только активности со спортом Biking (велосипед).",
                sport = parsed.Sport
            });
        }

        if (parsed.Trackpoints.Count == 0)
        {
            return new BadRequestObjectResult(new { message = "В TCX нет точек трека." });
        }

        var now = DateTime.UtcNow;
        var entity = new UserBikeActivity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Sport = parsed.Sport,
            Notes = parsed.Notes,
            StartTimeUtc = parsed.ActivityIdUtc,
            TotalSeconds = parsed.TotalTimeSeconds,
            DistanceMeters = parsed.DistanceMeters,
            Calories = parsed.Calories,
            AverageHeartRateBpm = parsed.AverageHeartRateBpm,
            MaxHeartRateBpm = parsed.MaxHeartRateBpm,
            Intensity = parsed.Intensity,
            TriggerMethod = parsed.TriggerMethod,
            ImportedAtUtc = now,
            TrackpointCount = parsed.Trackpoints.Count
        };

        var order = 0;
        foreach (var p in parsed.Trackpoints)
        {
            entity.TrackPoints.Add(new UserBikeActivityTrackPoint
            {
                Id = Guid.NewGuid(),
                ActivityId = entity.Id,
                OrderIndex = order++,
                TimeUtc = p.TimeUtc,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                AltitudeMeters = p.AltitudeMeters,
                HeartRateBpm = p.HeartRateBpm,
                Cadence = p.Cadence,
                SpeedMetersPerSecond = p.SpeedMetersPerSecond
            });
        }

        _dbContext.UserBikeActivities.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(MapSummary(entity));
    }

    public async Task<IActionResult> ListAsync(
        ClaimsPrincipal principal,
        DateOnly? from,
        DateOnly? to,
        string? sport,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var query = _dbContext.UserBikeActivities.AsNoTracking().Where(x => x.UserId == user.Id);

        var sportFilter = string.IsNullOrWhiteSpace(sport) ? "Biking" : sport.Trim();
        query = query.Where(x => EF.Functions.ILike(x.Sport, sportFilter));

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.StartTimeUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(x => x.StartTimeUtc < toExclusive);
        }

        var list = await query
            .OrderByDescending(x => x.StartTimeUtc)
            .Select(x => new BikeActivitySummaryResponse
            {
                Id = x.Id,
                Sport = x.Sport,
                Notes = x.Notes,
                StartTimeUtc = x.StartTimeUtc,
                TotalSeconds = x.TotalSeconds,
                DistanceMeters = x.DistanceMeters,
                Calories = x.Calories,
                AverageHeartRateBpm = x.AverageHeartRateBpm,
                MaxHeartRateBpm = x.MaxHeartRateBpm,
                TrackpointCount = x.TrackpointCount,
                ImportedAtUtc = x.ImportedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(list);
    }

    public async Task<IActionResult> GetByIdAsync(
        ClaimsPrincipal principal,
        Guid id,
        int? trackpointLimit,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var limit = trackpointLimit is > 0 and <= MaxTrackpointsReturned
            ? trackpointLimit.Value
            : Math.Min(2000, MaxTrackpointsReturned);

        var activity = await _dbContext.UserBikeActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id, cancellationToken);

        if (activity is null)
        {
            return new NotFoundObjectResult(new { message = "Активность не найдена." });
        }

        var points = await _dbContext.UserBikeActivityTrackPoints
            .AsNoTracking()
            .Where(p => p.ActivityId == id)
            .OrderBy(p => p.OrderIndex)
            .Take(limit)
            .Select(p => new BikeActivityTrackPointResponse
            {
                TimeUtc = p.TimeUtc,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                AltitudeMeters = p.AltitudeMeters,
                HeartRateBpm = p.HeartRateBpm,
                Cadence = p.Cadence,
                SpeedMetersPerSecond = p.SpeedMetersPerSecond
            })
            .ToListAsync(cancellationToken);

        var response = new BikeActivityDetailResponse
        {
            Id = activity.Id,
            Sport = activity.Sport,
            Notes = activity.Notes,
            StartTimeUtc = activity.StartTimeUtc,
            TotalSeconds = activity.TotalSeconds,
            DistanceMeters = activity.DistanceMeters,
            Calories = activity.Calories,
            AverageHeartRateBpm = activity.AverageHeartRateBpm,
            MaxHeartRateBpm = activity.MaxHeartRateBpm,
            TrackpointCount = activity.TrackpointCount,
            ImportedAtUtc = activity.ImportedAtUtc,
            Intensity = activity.Intensity,
            TriggerMethod = activity.TriggerMethod,
            Trackpoints = points
        };

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> DeleteAsync(ClaimsPrincipal principal, Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var entity = await _dbContext.UserBikeActivities.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id, cancellationToken);
        if (entity is null)
        {
            return new NotFoundObjectResult(new { message = "Активность не найдена." });
        }

        _dbContext.UserBikeActivities.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new NoContentResult();
    }

    private static BikeActivitySummaryResponse MapSummary(UserBikeActivity x) => new()
    {
        Id = x.Id,
        Sport = x.Sport,
        Notes = x.Notes,
        StartTimeUtc = x.StartTimeUtc,
        TotalSeconds = x.TotalSeconds,
        DistanceMeters = x.DistanceMeters,
        Calories = x.Calories,
        AverageHeartRateBpm = x.AverageHeartRateBpm,
        MaxHeartRateBpm = x.MaxHeartRateBpm,
        TrackpointCount = x.TrackpointCount,
        ImportedAtUtc = x.ImportedAtUtc
    };

    private async Task<AppUser?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);
    }
}
