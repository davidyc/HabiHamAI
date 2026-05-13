using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class UsersService : IUsersService, IUserWeightRecordingService
{
    private readonly AppDbContext _dbContext;

    public UsersService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> GetMyProfileAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        return new OkObjectResult(MapToProfileResponse(user));
    }

    public async Task<IActionResult> UpdateMyProfileAsync(ClaimsPrincipal principal, UpdateUserProfileRequest request)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        if (request.HeightCm is < 0 or > 300)
        {
            return new BadRequestObjectResult(new { message = "Height must be between 0 and 300 cm." });
        }

        if (request.WeightKg is < 0 or > 700)
        {
            return new BadRequestObjectResult(new { message = "Weight must be between 0 and 700 kg." });
        }

        user.BirthDate = request.BirthDate;
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.Phone = NormalizeOrNull(request.Phone, 30);
        user.City = NormalizeOrNull(request.City, 120);
        user.About = NormalizeOrNull(request.About, 500);
        user.FirstName = NormalizeOrNull(request.FirstName, 100);
        user.LastName = NormalizeOrNull(request.LastName, 100);

        await SyncWeightWithTrackerAndAiAsync(
            user,
            request.WeightKg,
            DateOnly.FromDateTime(DateTime.UtcNow),
            upsertTrackerEntry: true,
            CancellationToken.None);

        await _dbContext.SaveChangesAsync();
        return new OkObjectResult(MapToProfileResponse(user));
    }

    public async Task<IActionResult> GetMyWeightTrackerAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var rows = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Select(x => new UserWeightEntryResponse
            {
                Id = x.Id,
                Date = x.Date,
                WeightKg = x.WeightKg,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> UpsertMyWeightTrackerEntryAsync(
        ClaimsPrincipal principal,
        UpsertUserWeightEntryRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        if (request.WeightKg is <= 0 or > 700)
        {
            return new BadRequestObjectResult(new { message = "Weight must be between 0 and 700 kg." });
        }

        await UpsertWeightEntryAsync(user.Id, request.Date, request.WeightKg, cancellationToken);

        var latestWeight = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(cancellationToken);

        user.WeightKg = latestWeight;
        await UpsertTrainerWeightAsync(user.Id, latestWeight, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyWeightTrackerAsync(principal, cancellationToken);
    }

    public async Task<IActionResult> DeleteMyWeightTrackerEntryAsync(
        ClaimsPrincipal principal,
        Guid entryId,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var entry = await _dbContext.UserWeightEntries
            .FirstOrDefaultAsync(x => x.Id == entryId && x.UserId == user.Id, cancellationToken);
        if (entry is null)
        {
            return new NotFoundObjectResult(new { message = "Weight entry not found." });
        }

        _dbContext.UserWeightEntries.Remove(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var latestWeight = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(cancellationToken);

        user.WeightKg = latestWeight;
        await UpsertTrainerWeightAsync(user.Id, latestWeight, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyWeightTrackerAsync(principal, cancellationToken);
    }

    public async Task RecordWeightTrackerEntryAsync(Guid userId, DateOnly date, decimal weightKg, CancellationToken cancellationToken)
    {
        if (weightKg is <= 0 or > 700)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be between 0 and 700 kg.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        await UpsertWeightEntryAsync(user.Id, date, weightKg, cancellationToken);

        var latestWeight = await _dbContext.UserWeightEntries
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(cancellationToken);

        user.WeightKg = latestWeight;
        await UpsertTrainerWeightAsync(user.Id, latestWeight, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AppUser?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
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

    private async Task SyncWeightWithTrackerAndAiAsync(
        AppUser user,
        decimal? weightKg,
        DateOnly date,
        bool upsertTrackerEntry,
        CancellationToken cancellationToken)
    {
        if (weightKg is > 0 && upsertTrackerEntry)
        {
            await UpsertWeightEntryAsync(user.Id, date, weightKg.Value, cancellationToken);
        }

        await UpsertTrainerWeightAsync(user.Id, weightKg, cancellationToken);
    }

    private async Task UpsertWeightEntryAsync(Guid userId, DateOnly date, decimal weightKg, CancellationToken cancellationToken)
    {
        var row = await _dbContext.UserWeightEntries
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Date == date, cancellationToken);
        var now = DateTime.UtcNow;

        if (row is null)
        {
            row = new UserWeightEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                WeightKg = weightKg,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _dbContext.UserWeightEntries.Add(row);
        }
        else
        {
            row.WeightKg = weightKg;
            row.UpdatedAtUtc = now;
        }
    }

    private async Task UpsertTrainerWeightAsync(Guid userId, decimal? weightKg, CancellationToken cancellationToken)
    {
        var trainerId = await _dbContext.AiAssistants
            .AsNoTracking()
            .Where(x => x.AssistantCode == "trainer")
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (trainerId == Guid.Empty)
        {
            return;
        }

        var hasWeightField = await _dbContext.AiAssistantFieldDefinitions
            .AsNoTracking()
            .AnyAsync(x => x.AiAssistantId == trainerId && x.FieldKey == "weight", cancellationToken);
        if (!hasWeightField)
        {
            return;
        }

        var row = await _dbContext.UserAiAssistantExtras
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AiAssistantId == trainerId, cancellationToken);

        Dictionary<string, string> values;
        if (row is null || string.IsNullOrWhiteSpace(row.ValuesJson))
        {
            values = new Dictionary<string, string>(StringComparer.Ordinal);
        }
        else
        {
            try
            {
                values = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ValuesJson)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch
            {
                values = new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        values["weight"] = weightKg.HasValue
            ? weightKg.Value.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        if (row is null)
        {
            row = new UserAiAssistantExtras
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AiAssistantId = trainerId,
                ValuesJson = JsonSerializer.Serialize(values)
            };
            _dbContext.UserAiAssistantExtras.Add(row);
        }
        else
        {
            row.ValuesJson = JsonSerializer.Serialize(values);
        }
    }

    private static string? NormalizeOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static UserProfileResponse MapToProfileResponse(AppUser user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            CreatedAtUtc = user.CreatedAtUtc,
            BirthDate = user.BirthDate,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            Phone = user.Phone,
            City = user.City,
            About = user.About,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AiSummary = user.AiSummary,
            TelegramLinked = user.TelegramChatId.HasValue
        };
    }
}
