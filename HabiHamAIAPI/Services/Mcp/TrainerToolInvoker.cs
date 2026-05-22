using System.Text.Json;
using HabiHamAIAPI.Services;

namespace HabiHamAIAPI.Services.Mcp;

public sealed class TrainerToolInvoker : ITrainerToolInvoker
{
    private readonly TrainerDataQueryService _data;
    private readonly TrainerToolExecutionContext _context;

    public TrainerToolInvoker(
        TrainerDataQueryService data,
        TrainerToolExecutionContext context)
    {
        _data = data;
        _context = context;
    }

    public IReadOnlyList<KernestalAiService.AiToolDefinition> GetToolDefinitions() =>
    [
        Tool(
            "get_strength_workout_history",
            "Завершённые силовые тренировки: дата, программа, упражнения, подходы (вес, повторы, RPE).",
            new
            {
                type = "object",
                properties = new
                {
                    from = new { type = "string", description = "YYYY-MM-DD" },
                    to = new { type = "string", description = "YYYY-MM-DD" },
                    program = new { type = "string" },
                    exerciseNameContains = new { type = "string", description = "Подстрока в названии упражнения" },
                    limit = new { type = "integer", minimum = 1, maximum = 15 }
                }
            }),
        Tool(
            "get_strength_programs",
            "Сохранённые программы силовых (шаблоны) с упражнениями.",
            new
            {
                type = "object",
                properties = new
                {
                    limit = new { type = "integer", minimum = 1, maximum = 10 }
                }
            }),
        Tool(
            "get_active_strength_workout",
            "Текущая активная силовая тренировка.",
            new { type = "object", properties = new { } }),
        Tool(
            "get_bike_activities",
            "Список велозаездов: дистанция, время, пульс.",
            new
            {
                type = "object",
                properties = new
                {
                    from = new { type = "string" },
                    to = new { type = "string" },
                    limit = new { type = "integer", minimum = 1, maximum = 15 }
                }
            }),
        Tool(
            "get_bike_activity",
            "Детали одного велозаезда по id.",
            new
            {
                type = "object",
                properties = new
                {
                    activityId = new { type = "string", description = "UUID заезда" }
                },
                required = new[] { "activityId" }
            }),
        Tool(
            "get_weight_entries",
            "Дневник веса за период.",
            new
            {
                type = "object",
                properties = new
                {
                    from = new { type = "string" },
                    to = new { type = "string" },
                    limit = new { type = "integer", minimum = 1, maximum = 60 }
                }
            }),
        Tool(
            "get_weekly_training_summary",
            "Сводка за период (по умолчанию 7 дней): силовые, вело, вес; сравнение с предыдущим периодом. Для недельного обзора — первым.",
            new
            {
                type = "object",
                properties = new
                {
                    days = new { type = "integer", minimum = 1, maximum = 14, description = "Дней в периоде (по умолчанию 7)" },
                    endingOn = new { type = "string", description = "Последний день YYYY-MM-DD" }
                }
            }),
        Tool(
            "get_trainer_profile",
            "Профиль: рост, вес, цель, опыт, оборудование, AI summary.",
            new { type = "object", properties = new { } })
    ];

    public async Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken cancellationToken)
    {
        if (_context.UserId == Guid.Empty)
        {
            return JsonSerializer.Serialize(new { error = "User context missing." });
        }

        var userId = _context.UserId;
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        var root = doc.RootElement;

        return toolName switch
        {
            "get_strength_workout_history" => await _data.GetStrengthWorkoutHistoryAsync(
                userId,
                GetString(root, "from"),
                GetString(root, "to"),
                GetString(root, "program"),
                GetString(root, "exerciseNameContains"),
                GetInt(root, "limit"),
                cancellationToken),
            "get_strength_programs" => await _data.GetStrengthProgramsAsync(
                userId,
                GetInt(root, "limit"),
                cancellationToken),
            "get_active_strength_workout" => await _data.GetActiveStrengthWorkoutAsync(userId, cancellationToken),
            "get_bike_activities" => await _data.GetBikeActivitiesAsync(
                userId,
                GetString(root, "from"),
                GetString(root, "to"),
                GetInt(root, "limit"),
                cancellationToken),
            "get_bike_activity" => await InvokeBikeActivityAsync(userId, root, cancellationToken),
            "get_weight_entries" => await _data.GetWeightEntriesAsync(
                userId,
                GetString(root, "from"),
                GetString(root, "to"),
                GetInt(root, "limit"),
                cancellationToken),
            "get_weekly_training_summary" => await _data.GetWeeklyTrainingSummaryAsync(
                userId,
                GetInt(root, "days"),
                GetString(root, "endingOn"),
                cancellationToken),
            "get_trainer_profile" => await _data.GetTrainerProfileAsync(
                userId,
                _context.TrainerAssistantId,
                cancellationToken),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
        };
    }

    private async Task<string> InvokeBikeActivityAsync(Guid userId, JsonElement root, CancellationToken cancellationToken)
    {
        var idRaw = GetString(root, "activityId");
        if (!Guid.TryParse(idRaw, out var activityId))
        {
            return JsonSerializer.Serialize(new { error = "activityId (UUID) is required." });
        }

        return await _data.GetBikeActivityAsync(userId, activityId, cancellationToken);
    }

    private static KernestalAiService.AiToolDefinition Tool(string name, string description, object parametersSchema) =>
        new(name, description, parametersSchema);

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static int? GetInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)
            ? v
            : null;
}
