using System.ComponentModel;
using System.Security.Claims;
using HabiHamAIAPI.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace HabiHamAIAPI.Services.Mcp;

[McpServerToolType]
public sealed class TrainerMcpTools
{
    private readonly TrainerDataQueryService _data;
    private readonly TrainerToolExecutionContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _dbContext;

    public TrainerMcpTools(
        TrainerDataQueryService data,
        TrainerToolExecutionContext context,
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _data = data;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    [McpServerTool(Name = "get_strength_workout_history")]
    [Description("Завершённые силовые тренировки пользователя: дата, программа, упражнения, подходы (вес, повторы, RPE).")]
    public Task<string> GetStrengthWorkoutHistory(
        [Description("Начало периода YYYY-MM-DD. По умолчанию — последние 90 дней.")] string? from = null,
        [Description("Конец периода YYYY-MM-DD.")] string? to = null,
        [Description("Фильтр по названию программы/дня.")] string? program = null,
        [Description("Подстрока в названии упражнения (например «жим»).")] string? exerciseNameContains = null,
        [Description("Макс. число тренировок (1–15).")] int? limit = null,
        CancellationToken cancellationToken = default) =>
        _data.GetStrengthWorkoutHistoryAsync(
            ResolveUserId(),
            from,
            to,
            program,
            exerciseNameContains,
            limit,
            cancellationToken);

    [McpServerTool(Name = "get_strength_programs")]
    [Description("Сохранённые программы силовых тренировок (шаблоны program::) с упражнениями и плановыми подходами.")]
    public Task<string> GetStrengthPrograms(
        [Description("Макс. число программ (1–10).")] int? limit = null,
        CancellationToken cancellationToken = default) =>
        _data.GetStrengthProgramsAsync(ResolveUserId(), limit, cancellationToken);

    [McpServerTool(Name = "get_active_strength_workout")]
    [Description("Текущая активная (незавершённая) силовая тренировка, если есть.")]
    public Task<string> GetActiveStrengthWorkout(CancellationToken cancellationToken = default) =>
        _data.GetActiveStrengthWorkoutAsync(ResolveUserId(), cancellationToken);

    [McpServerTool(Name = "get_bike_activities")]
    [Description("Список велозаездов (TCX, Biking): дата, дистанция, время, пульс, калории.")]
    public Task<string> GetBikeActivities(
        [Description("Начало периода YYYY-MM-DD.")] string? from = null,
        [Description("Конец периода YYYY-MM-DD.")] string? to = null,
        [Description("Макс. число заездов (1–15).")] int? limit = null,
        CancellationToken cancellationToken = default) =>
        _data.GetBikeActivitiesAsync(ResolveUserId(), from, to, limit, cancellationToken);

    [McpServerTool(Name = "get_bike_activity")]
    [Description("Детали одного велозаезда по id (без GPS-трека).")]
    public Task<string> GetBikeActivity(
        [Description("UUID заезда из get_bike_activities.")] Guid activityId,
        CancellationToken cancellationToken = default) =>
        _data.GetBikeActivityAsync(ResolveUserId(), activityId, cancellationToken);

    [McpServerTool(Name = "get_weight_entries")]
    [Description("Дневник веса за период.")]
    public Task<string> GetWeightEntries(
        [Description("Начало периода YYYY-MM-DD.")] string? from = null,
        [Description("Конец периода YYYY-MM-DD.")] string? to = null,
        [Description("Макс. записей (1–60).")] int? limit = null,
        CancellationToken cancellationToken = default) =>
        _data.GetWeightEntriesAsync(ResolveUserId(), from, to, limit, cancellationToken);

    [McpServerTool(Name = "get_weekly_training_summary")]
    [Description("Сводка тренировок за период (по умолчанию 7 дней): силовые, вело, вес; сравнение с предыдущим таким же периодом. Для недельного обзора вызывай в первую очередь.")]
    public Task<string> GetWeeklyTrainingSummary(
        [Description("Число дней в периоде (1–14). По умолчанию 7.")] int? days = null,
        [Description("Последний день периода YYYY-MM-DD. По умолчанию — сегодня (UTC).")] string? endingOn = null,
        CancellationToken cancellationToken = default) =>
        _data.GetWeeklyTrainingSummaryAsync(ResolveUserId(), days, endingOn, cancellationToken);

    [McpServerTool(Name = "get_trainer_profile")]
    [Description("Профиль для тренера: рост, вес, дата рождения, AI summary, доп. поля ассистента (цель, опыт, оборудование).")]
    public Task<string> GetTrainerProfile(CancellationToken cancellationToken = default) =>
        _data.GetTrainerProfileAsync(ResolveUserId(), _context.TrainerAssistantId, cancellationToken);

    private Guid ResolveUserId()
    {
        if (_context.UserId != Guid.Empty)
        {
            return _context.UserId;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub");
            if (Guid.TryParse(userIdClaim, out var id))
            {
                return id;
            }

            var username = principal.Identity.Name?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(username))
            {
                var user = _dbContext.Users.AsNoTracking()
                    .FirstOrDefault(x => x.Username == username);
                if (user is not null)
                {
                    return user.Id;
                }
            }
        }

        throw new InvalidOperationException("Trainer MCP: user context is not set. Authenticate or use chat API.");
    }
}
