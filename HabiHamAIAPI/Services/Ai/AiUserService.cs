using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using HabiHamAIAPI.Services.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Ai;

public sealed class AiUserService : IAiUserService
{
    private const string TrainerToolsSystemAppendix = """

        ### ИНСТРУМЕНТЫ (MCP):
        У тебя есть tools для чтения реальных данных пользователя: силовые тренировки, программы, активная тренировка, велозаезды, дневник веса, профиль, сводка за период (get_weekly_training_summary).
        Если вопрос про факты, цифры, прогресс или историю — сначала вызови нужные tools, затем отвечай. Не выдумывай веса, подходы и заезды.

        ### ОБЗОР ЗА НЕДЕЛЮ:
        По запросу обзора/итогов за неделю (или последние N дней) сначала вызови get_weekly_training_summary, затем при необходимости get_strength_workout_history или get_bike_activities для деталей.
        Ответ: резюме регулярности и объёма, силовые (прогресс/застой по ключевым упражнениям), вело и вес если есть данные, 2–3 рекомендации на следующую неделю. Сравнивай с previousPeriod в сводке, если она есть.
        """;

    private const string WeeklyReviewChatUserLabel = "Обзор тренировок за неделю";

    private const string WeeklyReviewLlmPrompt = """
        Сделай обзор моих тренировок за период {0} — {1} ({2} дн.).

        Обязательно вызови tool get_weekly_training_summary (days={2}, endingOn={1}), при необходимости — get_strength_workout_history или get_bike_activities.

        Структура ответа:
        1. Краткое резюме недели (регулярность, объём, баланс силовые/вело).
        2. Силовые: что сделано, прогресс или застой по ключевым упражнениям (сравни с previousPeriod в сводке).
        3. Велозаезды (если были): дистанция и нагрузка.
        4. Вес (если есть записи): динамика.
        5. Две–три конкретные рекомендации на следующую неделю.

        Пиши по-русски, только на основе данных из tools.
        """;

    private static readonly Regex UserFieldPlaceholderRegex = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private readonly IKernestalAiService _kernestalAiService;
    private readonly ITrainerAgentService _trainerAgentService;
    private readonly TrainerDataQueryService _trainerDataQuery;
    private readonly AppDbContext _dbContext;
    private readonly TrainerMcpOptions _trainerMcpOptions;

    public AiUserService(
        IKernestalAiService kernestalAiService,
        ITrainerAgentService trainerAgentService,
        TrainerDataQueryService trainerDataQuery,
        AppDbContext dbContext,
        IOptions<TrainerMcpOptions> trainerMcpOptions)
    {
        _kernestalAiService = kernestalAiService;
        _trainerAgentService = trainerAgentService;
        _trainerDataQuery = trainerDataQuery;
        _dbContext = dbContext;
        _trainerMcpOptions = trainerMcpOptions.Value;
    }

    public async Task<IActionResult> ChatAsync(ClaimsPrincipal principal, AiChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new BadRequestObjectResult(new { message = "Prompt is required." });
        }

        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var dialog = await ResolveOrCreateDialogAsync(currentUser.Id, request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            DialogId = dialog.Id,
            Role = "user",
            Content = request.Prompt.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.ChatMessages.Add(userMessage);
        dialog.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await ClearStaleAssistantSelectionIfNeeded(currentUser, cancellationToken);

        Guid? assistantForChat;
        if (request.AssistantId is { } reqAssistantId && reqAssistantId != Guid.Empty)
        {
            var assistantOk = await _dbContext.AiAssistants.AnyAsync(
                x => x.Id == reqAssistantId && x.IsActive,
                cancellationToken);
            if (!assistantOk)
            {
                return new BadRequestObjectResult(new { message = "Assistant not found or inactive." });
            }

            assistantForChat = reqAssistantId;
        }
        else
        {
            assistantForChat = currentUser.SelectedAiAssistantId;
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
            currentUser.Id,
            assistantForChat,
            allMessages,
            cancellationToken);

        try
        {
            var useTrainerTools = await ShouldUseTrainerToolsAsync(assistantForChat, cancellationToken);
            string response;
            if (useTrainerTools)
            {
                try
                {
                    response = await _trainerAgentService.CompleteWithToolsAsync(
                        currentUser.Id,
                        assistantForChat!.Value,
                        messagesForLlm,
                        cancellationToken);
                }
                catch (InvalidOperationException ex) when (LooksLikeToolsUnsupportedByLlm(ex.Message))
                {
                    response = await _kernestalAiService.GetCompletionAsync(messagesForLlm, cancellationToken);
                }
            }
            else
            {
                response = await _kernestalAiService.GetCompletionAsync(messagesForLlm, cancellationToken);
            }

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
            currentUser.AiSummary = TruncateForSummary(response, 8000);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new OkObjectResult(new
            {
                dialogId = dialog.Id,
                dialogTitle = dialog.Title,
                response
            });
        }
        catch (InvalidOperationException ex)
        {
            return new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status502BadGateway };
        }
    }

    public async Task<IActionResult> WeeklyReviewAsync(
        ClaimsPrincipal principal,
        WeeklyTrainingReviewRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var trainerAssistantId = await ResolveTrainerAssistantIdAsync(request.AssistantId, cancellationToken);
        if (trainerAssistantId is null)
        {
            return new BadRequestObjectResult(new { message = "Trainer assistant not found or inactive." });
        }

        if (!_trainerMcpOptions.Enabled)
        {
            return new BadRequestObjectResult(new { message = "Trainer tools are disabled." });
        }

        var (periodFrom, periodTo, dayCount) = _trainerDataQuery.ResolveWeeklyPeriod(request.Days, request.EndingOn);
        var fingerprint = await _trainerDataQuery.ComputeTrainingDataFingerprintAsync(
            currentUser.Id,
            periodFrom,
            periodTo,
            cancellationToken);

        var existing = await _dbContext.UserWeeklyTrainingReviews
            .FirstOrDefaultAsync(
                x => x.UserId == currentUser.Id && x.PeriodFrom == periodFrom && x.PeriodTo == periodTo,
                cancellationToken);

        string responseText;
        var fromIso = periodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toIso = periodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cached = existing is not null && existing.DataFingerprint == fingerprint;
        var generated = false;

        if (cached)
        {
            responseText = existing!.Content;
        }
        else
        {
            var hasData = await _trainerDataQuery.HasAnyTrainingDataInPeriodAsync(
                currentUser.Id,
                periodFrom,
                periodTo,
                cancellationToken);

            if (!hasData)
            {
                responseText =
                    $"За период {fromIso} — {toIso} в журнале нет завершённых силовых тренировок, велозаездов и записей веса. " +
                    "Добавьте тренировки или заезды — после этого можно снова запросить обзор.";
            }
            else
            {
                try
                {
                    responseText = await GenerateWeeklyReviewContentAsync(
                        currentUser.Id,
                        trainerAssistantId.Value,
                        periodFrom,
                        periodTo,
                        dayCount,
                        cancellationToken);
                    generated = true;
                }
                catch (InvalidOperationException ex)
                {
                    return new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status502BadGateway };
                }
            }

            var now = DateTime.UtcNow;
            if (existing is null)
            {
                _dbContext.UserWeeklyTrainingReviews.Add(new UserWeeklyTrainingReview
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUser.Id,
                    AiAssistantId = trainerAssistantId.Value,
                    PeriodFrom = periodFrom,
                    PeriodTo = periodTo,
                    DataFingerprint = fingerprint,
                    Content = responseText,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existing.DataFingerprint = fingerprint;
                existing.Content = responseText;
                existing.AiAssistantId = trainerAssistantId.Value;
                existing.UpdatedAtUtc = now;
            }
        }

        var dialog = await ResolveOrCreateDialogAsync(currentUser.Id, request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        var userLabel = dayCount == 7
            ? WeeklyReviewChatUserLabel
            : $"{WeeklyReviewChatUserLabel} ({dayCount} дн.)";
        var nowUtc = DateTime.UtcNow;
        _dbContext.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            DialogId = dialog.Id,
            Role = "user",
            Content = userLabel,
            CreatedAtUtc = nowUtc
        });
        _dbContext.ChatMessages.Add(new ChatMessage
        {
            Id = Guid.NewGuid(),
            DialogId = dialog.Id,
            Role = "assistant",
            Content = responseText,
            CreatedAtUtc = nowUtc
        });
        dialog.AiAssistantId = trainerAssistantId;
        dialog.UpdatedAtUtc = nowUtc;
        if (generated)
        {
            currentUser.AiSummary = TruncateForSummary(responseText, 8000);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new
        {
            dialogId = dialog.Id,
            dialogTitle = dialog.Title,
            response = responseText,
            cached,
            generated,
            period = new { from = fromIso, to = toIso, days = dayCount }
        });
    }

    public async Task<IActionResult> GetWeeklyReviewsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var rows = await _dbContext.UserWeeklyTrainingReviews
            .AsNoTracking()
            .Where(x => x.UserId == currentUser.Id)
            .OrderByDescending(x => x.PeriodTo)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var reviews = rows.Select(x => new
        {
            id = x.Id,
            periodFrom = x.PeriodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            periodTo = x.PeriodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            days = x.PeriodTo.DayNumber - x.PeriodFrom.DayNumber + 1,
            updatedAtUtc = x.UpdatedAtUtc,
            createdAtUtc = x.CreatedAtUtc,
            preview = x.Content.Length > 280 ? x.Content.Substring(0, 280) + "…" : x.Content
        }).ToList();

        return new OkObjectResult(new { reviews });
    }

    public async Task<IActionResult> GetWeeklyReviewAsync(
        ClaimsPrincipal principal,
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var row = await _dbContext.UserWeeklyTrainingReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == reviewId && x.UserId == currentUser.Id, cancellationToken);

        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Review not found." });
        }

        return new OkObjectResult(new
        {
            id = row.Id,
            periodFrom = row.PeriodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            periodTo = row.PeriodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            days = row.PeriodTo.DayNumber - row.PeriodFrom.DayNumber + 1,
            content = row.Content,
            updatedAtUtc = row.UpdatedAtUtc,
            createdAtUtc = row.CreatedAtUtc
        });
    }

    public async Task<IActionResult> ImportWeeklyReviewAsync(
        ClaimsPrincipal principal,
        ImportWeeklyTrainingReviewRequest request,
        CancellationToken cancellationToken)
    {
        var content = (request.Content ?? string.Empty).Trim();
        if (content.Length < 80)
        {
            return new BadRequestObjectResult(new { message = "Content is too short to save as a review." });
        }

        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var trainerAssistantId = await ResolveTrainerAssistantIdAsync(null, cancellationToken);
        if (trainerAssistantId is null)
        {
            return new BadRequestObjectResult(new { message = "Trainer assistant not found or inactive." });
        }

        var (periodFrom, periodTo, dayCount) = _trainerDataQuery.ResolveWeeklyPeriod(request.Days, request.EndingOn);
        var fingerprint = await _trainerDataQuery.ComputeTrainingDataFingerprintAsync(
            currentUser.Id,
            periodFrom,
            periodTo,
            cancellationToken);

        var existing = await _dbContext.UserWeeklyTrainingReviews
            .FirstOrDefaultAsync(
                x => x.UserId == currentUser.Id && x.PeriodFrom == periodFrom && x.PeriodTo == periodTo,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (existing is null)
        {
            _dbContext.UserWeeklyTrainingReviews.Add(new UserWeeklyTrainingReview
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.Id,
                AiAssistantId = trainerAssistantId.Value,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                DataFingerprint = fingerprint,
                Content = content,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.Content = content;
            existing.DataFingerprint = fingerprint;
            existing.AiAssistantId = trainerAssistantId.Value;
            existing.UpdatedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var fromIso = periodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toIso = periodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new OkObjectResult(new
        {
            message = "Review saved.",
            period = new { from = fromIso, to = toIso, days = dayCount }
        });
    }

    public async Task<IActionResult> GetDialogsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var dialogs = await _dbContext.ChatDialogs
            .Where(x => x.UserId == currentUser.Id)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                title = x.Title,
                aiAssistantId = x.AiAssistantId,
                aiAssistantName = x.AiAssistant != null ? x.AiAssistant.Name : null,
                createdAtUtc = x.CreatedAtUtc,
                updatedAtUtc = x.UpdatedAtUtc,
                messagesCount = x.Messages.Count
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(dialogs);
    }

    public async Task<IActionResult> GetDialogMessagesAsync(ClaimsPrincipal principal, Guid dialogId, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var dialogExists = await _dbContext.ChatDialogs
            .AnyAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (!dialogExists)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        var messages = await _dbContext.ChatMessages
            .Where(x => x.DialogId == dialogId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                role = x.Role,
                content = x.Content,
                createdAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(messages);
    }

    public async Task<IActionResult> CreateDialogAsync(ClaimsPrincipal principal, CreateDialogRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var now = DateTime.UtcNow;
        var dialog = new ChatDialog
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Title = BuildTitleOrDefault(request.Title),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.ChatDialogs.Add(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new { id = dialog.Id, title = dialog.Title, createdAtUtc = dialog.CreatedAtUtc });
    }

    public async Task<IActionResult> RenameDialogAsync(ClaimsPrincipal principal, Guid dialogId, RenameDialogRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return new BadRequestObjectResult(new { message = "Title is required." });
        }

        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var dialog = await _dbContext.ChatDialogs
            .FirstOrDefaultAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        dialog.Title = BuildTitleOrDefault(request.Title);
        dialog.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new { id = dialog.Id, title = dialog.Title, updatedAtUtc = dialog.UpdatedAtUtc });
    }

    public async Task<IActionResult> DeleteDialogAsync(ClaimsPrincipal principal, Guid dialogId, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var dialog = await _dbContext.ChatDialogs
            .FirstOrDefaultAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        _dbContext.ChatDialogs.Remove(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Dialog deleted." });
    }

    public async Task<IActionResult> GetAssistantsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        await ClearStaleAssistantSelectionIfNeeded(currentUser, cancellationToken);

        var selectedId = currentUser.SelectedAiAssistantId;
        var items = await _dbContext.AiAssistants
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                id = x.Id,
                assistantCode = x.AssistantCode,
                name = x.Name,
                description = x.Description,
                sortOrder = x.SortOrder,
                selected = selectedId != null && selectedId == x.Id
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(new { assistants = items, selectedAssistantId = selectedId });
    }

    public async Task<IActionResult> SetAssistantSelectionAsync(ClaimsPrincipal principal, AiAssistantSelectionRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        if (request.AssistantId is null)
        {
            currentUser.SelectedAiAssistantId = null;
        }
        else
        {
            var exists = await _dbContext.AiAssistants.AnyAsync(
                x => x.Id == request.AssistantId && x.IsActive,
                cancellationToken);
            if (!exists)
            {
                return new BadRequestObjectResult(new { message = "Assistant not found or inactive." });
            }

            currentUser.SelectedAiAssistantId = request.AssistantId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { selectedAssistantId = currentUser.SelectedAiAssistantId });
    }

    public async Task<IActionResult> GetAssistantExtraFieldsAsync(ClaimsPrincipal principal, Guid assistantId, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var assistantOk = await _dbContext.AiAssistants
            .AsNoTracking()
            .AnyAsync(x => x.Id == assistantId && x.IsActive, cancellationToken);
        if (!assistantOk)
        {
            return new NotFoundObjectResult(new { message = "Assistant not found or inactive." });
        }

        var definitions = await _dbContext.AiAssistantFieldDefinitions
            .AsNoTracking()
            .Where(x => x.AiAssistantId == assistantId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .Select(x => new
            {
                id = x.Id,
                fieldKey = x.FieldKey,
                label = x.Label,
                fieldType = x.FieldType,
                sortOrder = x.SortOrder,
                isRequired = x.IsRequired
            })
            .ToListAsync(cancellationToken);

        var extrasRow = await _dbContext.UserAiAssistantExtras
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == currentUser.Id && x.AiAssistantId == assistantId,
                cancellationToken);

        Dictionary<string, string> values = new();
        if (extrasRow is not null && !string.IsNullOrWhiteSpace(extrasRow.ValuesJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(extrasRow.ValuesJson);
                if (parsed is not null)
                {
                    values = parsed;
                }
            }
            catch
            {
            }
        }

        var hasWeightField = definitions.Any(x => string.Equals(x.fieldKey, "weight", StringComparison.OrdinalIgnoreCase));
        if (hasWeightField && currentUser.WeightKg.HasValue)
        {
            values["weight"] = currentUser.WeightKg.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new OkObjectResult(new { definitions, values });
    }

    public async Task<IActionResult> PutAssistantExtraFieldsAsync(ClaimsPrincipal principal, UserAiAssistantExtrasPutRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var assistantOk = await _dbContext.AiAssistants
            .AnyAsync(x => x.Id == request.AssistantId && x.IsActive, cancellationToken);
        if (!assistantOk)
        {
            return new NotFoundObjectResult(new { message = "Assistant not found or inactive." });
        }

        var defs = await _dbContext.AiAssistantFieldDefinitions
            .Where(x => x.AiAssistantId == request.AssistantId)
            .ToListAsync(cancellationToken);

        var allowed = defs.Select(x => x.FieldKey).ToHashSet(StringComparer.Ordinal);
        var incoming = request.Values ?? new Dictionary<string, string>();

        var cleaned = new Dictionary<string, string>();
        foreach (var kv in incoming)
        {
            if (!allowed.Contains(kv.Key))
            {
                continue;
            }

            cleaned[kv.Key] = kv.Value ?? "";
        }

        foreach (var d in defs.Where(x => x.IsRequired))
        {
            if (!cleaned.TryGetValue(d.FieldKey, out var v) || string.IsNullOrWhiteSpace(v))
            {
                return new BadRequestObjectResult(new { message = $"Заполни обязательное поле: {d.Label}" });
            }
        }

        if (TryReadWeightFromValues(cleaned, out var parsedWeight))
        {
            currentUser.WeightKg = parsedWeight;
            if (parsedWeight.HasValue)
            {
                await UpsertWeightEntryAsync(currentUser.Id, DateOnly.FromDateTime(DateTime.UtcNow), parsedWeight.Value, cancellationToken);
            }
        }

        var json = JsonSerializer.Serialize(cleaned);
        var row = await _dbContext.UserAiAssistantExtras
            .FirstOrDefaultAsync(
                x => x.UserId == currentUser.Id && x.AiAssistantId == request.AssistantId,
                cancellationToken);

        if (row is null)
        {
            row = new UserAiAssistantExtras
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.Id,
                AiAssistantId = request.AssistantId,
                ValuesJson = json
            };
            _dbContext.UserAiAssistantExtras.Add(row);
        }
        else
        {
            row.ValuesJson = json;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Saved.", values = cleaned });
    }

    private async Task ClearStaleAssistantSelectionIfNeeded(AppUser user, CancellationToken cancellationToken)
    {
        if (user.SelectedAiAssistantId is null)
        {
            return;
        }

        var ok = await _dbContext.AiAssistants.AnyAsync(
            x => x.Id == user.SelectedAiAssistantId && x.IsActive,
            cancellationToken);
        if (!ok)
        {
            user.SelectedAiAssistantId = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
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

    private async Task<AppUser?> ResolveCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.Identity?.Name?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    private async Task<Guid?> ResolveTrainerAssistantIdAsync(Guid? requestedId, CancellationToken cancellationToken)
    {
        if (requestedId is { } id && id != Guid.Empty)
        {
            var ok = await _dbContext.AiAssistants.AsNoTracking().AnyAsync(
                x => x.Id == id && x.IsActive && x.AssistantCode == "trainer",
                cancellationToken);
            return ok ? id : null;
        }

        return await _dbContext.AiAssistants.AsNoTracking()
            .Where(x => x.IsActive && x.AssistantCode == "trainer")
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string> GenerateWeeklyReviewContentAsync(
        Guid userId,
        Guid trainerAssistantId,
        DateOnly periodFrom,
        DateOnly periodTo,
        int dayCount,
        CancellationToken cancellationToken)
    {
        var fromIso = periodFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toIso = periodTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var userPrompt = string.Format(
            CultureInfo.InvariantCulture,
            WeeklyReviewLlmPrompt,
            fromIso,
            toIso,
            dayCount);

        var messages = await BuildMessagesWithSystemPromptAsync(
            userId,
            trainerAssistantId,
            [new KernestalAiService.AiChatMessage("user", userPrompt)],
            cancellationToken);

        try
        {
            return await _trainerAgentService.CompleteWithToolsAsync(
                userId,
                trainerAssistantId,
                messages,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (LooksLikeToolsUnsupportedByLlm(ex.Message))
        {
            return await _kernestalAiService.GetCompletionAsync(messages, cancellationToken);
        }
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

    private static string BuildTitleOrDefault(string? title)
    {
        var normalized = (title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Новый диалог";
        }

        return normalized.Length > 200 ? normalized[..200] : normalized;
    }

    private static string? TruncateForSummary(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var t = text.Trim();
        return t.Length <= maxLength ? t : t[..maxLength];
    }

    private static bool TryReadWeightFromValues(IReadOnlyDictionary<string, string> values, out decimal? weightKg)
    {
        weightKg = null;
        if (!TryGetExtraValue(values, "weight", out var raw))
        {
            return false;
        }

        var normalized = (raw ?? string.Empty).Trim().Replace(',', '.');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            weightKg = null;
            return true;
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            && value > 0
            && value <= 700)
        {
            weightKg = value;
            return true;
        }

        return false;
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
}
