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

    public async Task<IActionResult> GetMyCategoriesAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var categories = await _dbContext.UserCategories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new UserCategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(categories);
    }

    public async Task<IActionResult> GetMyHabitsOverviewAsync(
        ClaimsPrincipal principal,
        DateOnly? asOfDate,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var habits = await _dbContext.UserHabits
            .AsNoTracking()
            .Where(x => x.UserId == user.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new UserHabitResponse
            {
                Id = x.Id,
                Name = x.Name,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        // Minimal "streak" computation: consecutive done days ending at `date`.
        // We only need check-ins up to ~3 years for typical usage.
        const int streakWindowDays = 365 * 3;
        var from = date.AddDays(-streakWindowDays);

        // NOTE: Npgsql/EF иногда плохо переводит DateOnly-предикаты в SQL.
        // Поэтому достаем все check-in'ы пользователя и фильтруем по окну на клиенте.
        var checkinsAll = await _dbContext.UserHabitCheckins
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => new { x.HabitId, x.Date, x.Status })
            .ToListAsync(cancellationToken);

        var checkinsWindow = checkinsAll
            .Where(x => x.Date >= from && x.Date <= date)
            .ToList();

        var checkinsByHabit = checkinsWindow
            .GroupBy(x => x.HabitId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var response = habits.Select(h =>
        {
            checkinsByHabit.TryGetValue(h.Id, out var habitCheckins);
            habitCheckins ??= [];

            var doneDates = habitCheckins
                .Where(x => x.Status == UserHabitCheckinStatus.Done)
                .Select(x => x.Date)
                .ToList();

            var doneSet = new HashSet<DateOnly>(doneDates);

            var current = 0;
            var d = date;
            while (doneSet.Contains(d))
            {
                current++;
                d = d.AddDays(-1);
            }

            var lastDoneDate = doneDates.Count > 0 ? doneDates.Max() : (DateOnly?)null;
            var todayCheckin = habitCheckins.FirstOrDefault(x => x.Date == date);
            var todayStatus = todayCheckin?.Status;
            var isDoneToday = todayStatus == UserHabitCheckinStatus.Done;

            return new UserHabitOverviewResponse
            {
                Id = h.Id,
                Name = h.Name,
                CategoryId = h.CategoryId,
                CategoryName = h.CategoryName,
                IsActive = h.IsActive,
                CreatedAtUtc = h.CreatedAtUtc,
                CurrentStreakDays = current,
                IsDoneToday = isDoneToday,
                TodayStatus = todayStatus,
                LastDoneDate = lastDoneDate
            };
        }).ToList();

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> CreateMyHabitAsync(
        ClaimsPrincipal principal,
        CreateUserHabitRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            return new BadRequestObjectResult(new { message = "Habit name is invalid." });
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.UserCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.CategoryId.Value && x.IsActive, cancellationToken);
            if (!categoryExists)
            {
                return new BadRequestObjectResult(new { message = "Category not found or inactive." });
            }
        }

        var maxSortOrder = await _dbContext.UserHabits
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;

        var habit = new UserHabit
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = request.CategoryId,
            Name = name,
            IsActive = true,
            SortOrder = maxSortOrder + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.UserHabits.Add(habit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyHabitsOverviewAsync(principal, null, cancellationToken);
    }

    public async Task<IActionResult> DeleteMyHabitAsync(
        ClaimsPrincipal principal,
        Guid habitId,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var habit = await _dbContext.UserHabits
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == user.Id, cancellationToken);

        if (habit is null)
        {
            return new NotFoundObjectResult(new { message = "Habit not found." });
        }

        _dbContext.UserHabits.Remove(habit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyHabitsOverviewAsync(principal, null, cancellationToken);
    }

    public async Task<IActionResult> GetMyHabitCheckinsAsync(
        ClaimsPrincipal principal,
        Guid habitId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var habit = await _dbContext.UserHabits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == user.Id && x.IsActive, cancellationToken);
        if (habit is null)
        {
            return new NotFoundObjectResult(new { message = "Habit not found." });
        }

        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        // NOTE: фильтр DateOnly по диапазону делаем на клиенте, чтобы
        // избежать проблем перевода DateOnly-предикатов в SQL.
        var rowsAll = await _dbContext.UserHabitCheckins
            .AsNoTracking()
            .Where(x => x.UserId == user.Id && x.HabitId == habitId)
            .OrderBy(x => x.Date)
            .Select(x => new UserHabitCheckinResponse
            {
                Date = x.Date,
                Status = x.Status,
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var rows = rowsAll
            .Where(x => x.Date >= start && x.Date <= end)
            .ToList();

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> UpsertMyHabitCheckinAsync(
        ClaimsPrincipal principal,
        Guid habitId,
        UpsertUserHabitCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var habit = await _dbContext.UserHabits
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == user.Id && x.IsActive, cancellationToken);
        if (habit is null)
        {
            return new NotFoundObjectResult(new { message = "Habit not found." });
        }

        var date = request.Date;
        if (date > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(365))
        {
            return new BadRequestObjectResult(new { message = "Date is too far in the future." });
        }

        var status = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
        if (!UserHabitCheckinStatus.IsValid(status))
        {
            return new BadRequestObjectResult(new { message = "Invalid status. Use partial, done or failed." });
        }

        var existing = await _dbContext.UserHabitCheckins
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.HabitId == habitId && x.Date == date, cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            _dbContext.UserHabitCheckins.Add(new UserHabitCheckin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                HabitId = habitId,
                Date = date,
                Status = status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.Status = status;
            existing.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "OK" });
    }

    public async Task<IActionResult> DeleteMyHabitCheckinAsync(
        ClaimsPrincipal principal,
        Guid habitId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var habit = await _dbContext.UserHabits
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == user.Id && x.IsActive, cancellationToken);
        if (habit is null)
        {
            return new NotFoundObjectResult(new { message = "Habit not found." });
        }

        var existing = await _dbContext.UserHabitCheckins
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.HabitId == habitId && x.Date == date, cancellationToken);
        if (existing is null)
        {
            return new NotFoundObjectResult(new { message = "Check-in not found." });
        }

        _dbContext.UserHabitCheckins.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "OK" });
    }

    public async Task<IActionResult> GetMyTodosAsync(
        ClaimsPrincipal principal,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        var startUtc = DateTime.SpecifyKind(start.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endExclusiveUtc = DateTime.SpecifyKind(end.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        // Minimal history: "relevant" items are those touched by date (created/due/done).
        var rows = await _dbContext.UserTodoItems
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Where(x =>
                // created is tracked as UTC timestamp; convert to DateOnly range.
                (x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endExclusiveUtc)
                || (x.DueDate.HasValue && x.DueDate.Value >= start && x.DueDate.Value <= end)
                || (x.DoneDate.HasValue && x.DoneDate.Value >= start && x.DoneDate.Value <= end))
            .OrderByDescending(x => x.DoneDate.HasValue)
            .ThenByDescending(x => x.DoneDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new UserTodoItemResponse
            {
                Id = x.Id,
                Title = x.Title,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                DueDate = x.DueDate,
                DoneDate = x.DoneDate,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> CreateMyTodoAsync(
        ClaimsPrincipal principal,
        CreateUserTodoItemRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var title = (request.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 500)
        {
            return new BadRequestObjectResult(new { message = "Todo title is invalid." });
        }

        if (request.DueDate.HasValue && request.DueDate.Value > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(365))
        {
            return new BadRequestObjectResult(new { message = "Due date is too far in the future." });
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.UserCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.CategoryId.Value && x.IsActive, cancellationToken);
            if (!categoryExists)
            {
                return new BadRequestObjectResult(new { message = "Category not found or inactive." });
            }
        }

        var now = DateTime.UtcNow;
        var row = new UserTodoItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = request.CategoryId,
            Title = title,
            DueDate = request.DueDate,
            DoneDate = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.UserTodoItems.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyTodosAsync(principal, null, null, cancellationToken);
    }

    public async Task<IActionResult> DeleteMyTodoAsync(
        ClaimsPrincipal principal,
        Guid todoId,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var row = await _dbContext.UserTodoItems
            .FirstOrDefaultAsync(x => x.Id == todoId && x.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Todo item not found." });
        }

        _dbContext.UserTodoItems.Remove(row);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMyTodosAsync(principal, null, null, cancellationToken);
    }

    public async Task<IActionResult> UpsertMyTodoDoneAsync(
        ClaimsPrincipal principal,
        Guid todoId,
        UpsertUserTodoDoneRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(principal, cancellationToken);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        var row = await _dbContext.UserTodoItems
            .FirstOrDefaultAsync(x => x.Id == todoId && x.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Todo item not found." });
        }

        if (request.IsDone)
        {
            if (request.Date is null)
            {
                return new BadRequestObjectResult(new { message = "Date is required when marking done." });
            }
            row.DoneDate = request.Date.Value;
        }
        else
        {
            row.DoneDate = null;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new { message = "OK" });
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
