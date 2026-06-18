using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using HabiHamAIAPI.Services.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Ai;

public sealed class UserAiChatService : IUserAiChatService
{
    private const string TrainerToolsSystemAppendix = """

        ### ИНСТРУМЕНТЫ (MCP):
        У тебя есть tools для чтения реальных данных пользователя: силовые тренировки, программы, активная тренировка, велозаезды, вес тела (get_current_weight, get_weight_entries), профиль, сводка за период (get_weekly_training_summary).
        Если вопрос про факты, цифры, прогресс, вес тела или историю — сначала вызови нужные tools, затем отвечай. Не выдумывай веса, подходы и заезды.
        По вопросам о весе тела вызывай get_current_weight или get_weight_entries; не путай с весом штанги в упражнениях.

        ### ОБЗОР ЗА НЕДЕЛЮ:
        По запросу обзора/итогов за неделю (или последние N дней) сначала вызови get_weekly_training_summary, затем при необходимости get_strength_workout_history или get_bike_activities для деталей.
        Ответ: резюме регулярности и объёма, силовые (прогресс/застой по ключевым упражнениям), вело и вес если есть данные, 2–3 рекомендации на следующую неделю. Сравнивай с previousPeriod в сводке, если она есть.
        """;

    private static readonly Regex UserFieldPlaceholderRegex = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private readonly IKernestalAiService _kernestalAiService;
    private readonly ITrainerAgentService _trainerAgentService;
    private readonly TrainerDataQueryService _trainerDataQueryService;
    private readonly AppDbContext _dbContext;
    private readonly TrainerMcpOptions _trainerMcpOptions;
    private readonly ILlmModelService _llmModelService;

    public UserAiChatService(
        IKernestalAiService kernestalAiService,
        ITrainerAgentService trainerAgentService,
        TrainerDataQueryService trainerDataQueryService,
        AppDbContext dbContext,
        IOptions<TrainerMcpOptions> trainerMcpOptions,
        ILlmModelService llmModelService)
    {
        _kernestalAiService = kernestalAiService;
        _trainerAgentService = trainerAgentService;
        _trainerDataQueryService = trainerDataQueryService;
        _dbContext = dbContext;
        _trainerMcpOptions = trainerMcpOptions.Value;
        _llmModelService = llmModelService;
    }

    public async Task<UserAiChatSendResult> SendMessageAsync(
        Guid userId,
        Guid? dialogId,
        string prompt,
        Guid? assistantId,
        CancellationToken cancellationToken,
        string? model = null)
    {
        var dialog = await ResolveOrCreateDialogAsync(userId, dialogId, cancellationToken);
        if (dialog is null)
        {
            throw new InvalidOperationException("Dialog not found.");
        }

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            DialogId = dialog.Id,
            Role = "user",
            Content = prompt.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(userMessage);
        dialog.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var assistantForChat = assistantId;
        if (assistantForChat is null || assistantForChat == Guid.Empty)
        {
            assistantForChat = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.SelectedAiAssistantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (assistantForChat is { } selectedId && selectedId != Guid.Empty)
        {
            var assistantOk = await _dbContext.AiAssistants.AnyAsync(
                x => x.Id == selectedId && x.IsActive,
                cancellationToken);
            if (!assistantOk)
            {
                assistantForChat = null;
            }
        }

        var dialogRows = await _dbContext.ChatMessages
            .Where(x => x.DialogId == dialog.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.Role, x.Content })
            .ToListAsync(cancellationToken);

        var allMessages = dialogRows
            .Select(x => new KernestalAiService.AiChatMessage(x.Role, x.Content))
            .ToList();

        var messagesForLlm = await BuildMessagesWithSystemPromptAsync(
            userId,
            assistantForChat,
            allMessages,
            cancellationToken);

        var modelForChat = await ResolveModelForChatAsync(model, assistantForChat, cancellationToken);
        var response = await CompleteAsync(userId, assistantForChat, messagesForLlm, modelForChat, cancellationToken);

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            DialogId = dialog.Id,
            Role = "assistant",
            Content = response,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(assistantMessage);
        dialog.AiAssistantId = assistantForChat;
        dialog.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserAiChatSendResult(dialog.Id, dialog.Title, response);
    }

    public async Task<string> CompleteTrainerPromptAsync(
        Guid userId,
        IReadOnlyList<KernestalAiService.AiChatMessage> messages,
        CancellationToken cancellationToken,
        string? model = null)
    {
        var trainerId = await ResolveTrainerAssistantIdAsync(cancellationToken);
        if (trainerId is null)
        {
            throw new InvalidOperationException("Trainer assistant not found.");
        }

        var messagesForLlm = await BuildMessagesWithSystemPromptAsync(
            userId,
            trainerId,
            messages,
            cancellationToken);

        var modelForChat = await ResolveModelForChatAsync(model, trainerId, cancellationToken);
        return await CompleteAsync(userId, trainerId, messagesForLlm, modelForChat, cancellationToken);
    }

    public Task<Guid?> ResolveTrainerAssistantIdAsync(CancellationToken cancellationToken) =>
        _dbContext.AiAssistants
            .AsNoTracking()
            .Where(x => x.AssistantCode == "trainer" && x.IsActive)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string> CompleteAsync(
        Guid userId,
        Guid? assistantForChat,
        IReadOnlyList<KernestalAiService.AiChatMessage> messagesForLlm,
        string modelForChat,
        CancellationToken cancellationToken)
    {
        var useTrainerTools = await ShouldUseTrainerToolsAsync(assistantForChat, cancellationToken);
        if (useTrainerTools)
        {
            try
            {
                return await _trainerAgentService.CompleteWithToolsAsync(
                    userId,
                    assistantForChat!.Value,
                    messagesForLlm,
                    cancellationToken,
                    modelForChat);
            }
            catch (InvalidOperationException ex) when (LooksLikeToolsUnsupportedByLlm(ex.Message))
            {
                return await _kernestalAiService.GetCompletionAsync(messagesForLlm, cancellationToken, modelForChat);
            }
        }

        return await _kernestalAiService.GetCompletionAsync(messagesForLlm, cancellationToken, modelForChat);
    }

    private async Task<List<KernestalAiService.AiChatMessage>> BuildMessagesWithSystemPromptAsync(
        Guid userId,
        Guid? assistantIdForChat,
        IReadOnlyList<KernestalAiService.AiChatMessage> dialogMessages,
        CancellationToken cancellationToken)
    {
        if (assistantIdForChat is null || assistantIdForChat == Guid.Empty)
        {
            return dialogMessages.ToList();
        }

        var systemPrompt = await _dbContext.AiAssistants
            .AsNoTracking()
            .Where(x => x.Id == assistantIdForChat && x.IsActive)
            .Select(x => x.SystemPrompt)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            return dialogMessages.ToList();
        }

        var assistantId = assistantIdForChat.Value;
        var trimmedPrompt = systemPrompt.Trim();
        var extrasMap = await LoadUserExtrasMapAsync(userId, assistantId, cancellationToken);
        var usesPlaceholders = ContainsUserFieldPlaceholders(trimmedPrompt);
        var fullSystem = usesPlaceholders
            ? ApplyUserFieldPlaceholders(trimmedPrompt, extrasMap)
            : trimmedPrompt;

        if (!usesPlaceholders)
        {
            var extrasBlock = await BuildUserExtrasBlockFromMapAsync(assistantId, extrasMap, cancellationToken);
            if (!string.IsNullOrWhiteSpace(extrasBlock))
            {
                fullSystem += "\n\n" + extrasBlock;
            }
        }

        if (_trainerMcpOptions.Enabled && await IsTrainerAssistantAsync(assistantId, cancellationToken))
        {
            fullSystem += TrainerToolsSystemAppendix;
            var weightBlock = await _trainerDataQueryService.BuildWeightContextBlockAsync(userId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(weightBlock))
            {
                fullSystem += "\n\n" + weightBlock;
            }

            var savedSummary = await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.AiSummary)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(savedSummary))
            {
                fullSystem += """

                    ### СОХРАНЁННОЕ САММАРИ ТРЕНИРОВОК:
                    """ + savedSummary.Trim() + """

                    Используй это саммари как постоянный контекст о тренировках пользователя. При необходимости уточняй актуальные данные через tools.
                    """;
            }
        }

        var list = new List<KernestalAiService.AiChatMessage> { new("system", fullSystem) };
        list.AddRange(dialogMessages);
        return list;
    }

    private static bool ContainsUserFieldPlaceholders(string template) =>
        UserFieldPlaceholderRegex.IsMatch(template ?? "");

    private static string ApplyUserFieldPlaceholders(string template, IReadOnlyDictionary<string, string> values)
    {
        return UserFieldPlaceholderRegex.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            if (TryGetExtraValue(values, key, out var v))
            {
                return v.Trim();
            }

            return "";
        });
    }

    private static bool TryGetExtraValue(IReadOnlyDictionary<string, string> values, string key, out string value)
    {
        if (values.TryGetValue(key, out var raw))
        {
            value = raw ?? "";
            return true;
        }

        foreach (var kv in values)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value ?? "";
                return true;
            }
        }

        value = "";
        return false;
    }

    private async Task<Dictionary<string, string>> LoadUserExtrasMapAsync(Guid userId, Guid assistantId, CancellationToken cancellationToken)
    {
        var row = await _dbContext.UserAiAssistantExtras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AiAssistantId == assistantId, cancellationToken);

        if (row is null || string.IsNullOrWhiteSpace(row.ValuesJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(row.ValuesJson);
            return parsed ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private async Task<string?> BuildUserExtrasBlockFromMapAsync(Guid assistantId, IReadOnlyDictionary<string, string> map, CancellationToken cancellationToken)
    {
        var defs = await _dbContext.AiAssistantFieldDefinitions
            .AsNoTracking()
            .Where(x => x.AiAssistantId == assistantId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .ToListAsync(cancellationToken);

        if (defs.Count == 0)
        {
            return null;
        }

        var lines = new List<string>();
        foreach (var d in defs)
        {
            if (!TryGetExtraValue(map, d.FieldKey, out var val))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(val))
            {
                continue;
            }

            lines.Add($"{d.Label}: {val.Trim()}");
        }

        return lines.Count == 0 ? null : "Дополнительные данные пользователя:\n" + string.Join("\n", lines);
    }

    private static bool LooksLikeToolsUnsupportedByLlm(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var m = message.ToLowerInvariant();
        return m.Contains("tool", StringComparison.Ordinal)
            || m.Contains("function", StringComparison.Ordinal)
            || m.Contains("unsupported", StringComparison.Ordinal)
            || m.Contains("not allowed", StringComparison.Ordinal)
            || m.Contains("invalid parameter", StringComparison.Ordinal);
    }

    private async Task<bool> ShouldUseTrainerToolsAsync(Guid? assistantId, CancellationToken cancellationToken)
    {
        if (!_trainerMcpOptions.Enabled || assistantId is null || assistantId == Guid.Empty)
        {
            return false;
        }

        return await IsTrainerAssistantAsync(assistantId.Value, cancellationToken);
    }

    private Task<bool> IsTrainerAssistantAsync(Guid assistantId, CancellationToken cancellationToken) =>
        _dbContext.AiAssistants
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == assistantId && x.IsActive && x.AssistantCode == "trainer",
                cancellationToken);

    private async Task<string> ResolveModelForChatAsync(
        string? requestedModel,
        Guid? assistantId,
        CancellationToken cancellationToken)
    {
        var normalized = ILlmModelService.NormalizeModelOrNull(requestedModel);
        if (normalized is not null && await _llmModelService.IsAllowedModelAsync(normalized, cancellationToken))
        {
            return normalized;
        }

        if (assistantId is { } id && id != Guid.Empty)
        {
            var assistantModel = await _dbContext.AiAssistants
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.Model)
                .FirstOrDefaultAsync(cancellationToken);
            var fromAssistant = ILlmModelService.NormalizeModelOrNull(assistantModel);
            if (fromAssistant is not null && await _llmModelService.IsAllowedModelAsync(fromAssistant, cancellationToken))
            {
                return fromAssistant;
            }
        }

        var defaultModel = await _llmModelService.GetDefaultModelNameAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(defaultModel))
        {
            return defaultModel;
        }

        return "gpt-4";
    }

    private async Task<ChatDialog?> ResolveOrCreateDialogAsync(Guid userId, Guid? dialogId, CancellationToken cancellationToken)
    {
        if (dialogId is null || dialogId == Guid.Empty)
        {
            var now = DateTime.UtcNow;
            var createdDialog = new ChatDialog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Новый диалог",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _dbContext.ChatDialogs.Add(createdDialog);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return createdDialog;
        }

        return await _dbContext.ChatDialogs
            .FirstOrDefaultAsync(x => x.Id == dialogId && x.UserId == userId, cancellationToken);
    }
}
