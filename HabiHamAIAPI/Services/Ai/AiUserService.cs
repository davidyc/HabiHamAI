using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Ai;

public sealed class AiUserService : IAiUserService
{
    private const string TrainingSummaryGenerationUserPrompt = """
        Составь подробное саммари тренировочного прогресса пользователя для сохранения в его профиле.
        Сначала вызови get_weekly_training_summary (14 дней), get_strength_workout_history за последние 90 дней, get_bike_activities, get_current_weight и get_weight_entries — по наличию данных.
        Включи: регулярность, ключевые упражнения и личные рекорды (вес × повторы, даты), динамику веса, вело-нагрузку, сильные стороны и зоны роста.
        Формат: структурированный текст на русском, до 4000 символов. Только факты из tools, без выдумок. Без приветствий и вопросов в конце — только саммари.
        """;

    private readonly IUserAiChatService _userAiChatService;
    private readonly AppDbContext _dbContext;
    private readonly TrainerMcpOptions _trainerMcpOptions;

    public AiUserService(
        IUserAiChatService userAiChatService,
        AppDbContext dbContext,
        IOptions<TrainerMcpOptions> trainerMcpOptions)
    {
        _userAiChatService = userAiChatService;
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

        var dialog = await ResolveExistingDialogAsync(currentUser.Id, request.DialogId, cancellationToken);
        if (request.DialogId is { } requestedDialogId && requestedDialogId != Guid.Empty && dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

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

        try
        {
            var result = await _userAiChatService.SendMessageAsync(
                currentUser.Id,
                request.DialogId,
                request.Prompt,
                assistantForChat,
                cancellationToken,
                request.Model);

            return new OkObjectResult(new
            {
                dialogId = result.DialogId,
                dialogTitle = result.DialogTitle,
                response = result.Response
            });
        }
        catch (InvalidOperationException ex)
        {
            return new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status502BadGateway };
        }
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
                model = x.Model,
                selected = selectedId != null && selectedId == x.Id
            })
            .ToListAsync(cancellationToken);

        return new OkObjectResult(new
        {
            assistants = items,
            selectedAssistantId = selectedId
        });
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

    public async Task<IActionResult> GenerateTrainingSummaryAsync(
        ClaimsPrincipal principal,
        GenerateTrainingSummaryRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_trainerMcpOptions.Enabled)
        {
            return new BadRequestObjectResult(new { message = "Trainer tools are disabled." });
        }

        var currentUser = await ResolveCurrentUserAsync(principal, cancellationToken);
        if (currentUser is null)
        {
            return new UnauthorizedObjectResult(new { message = "User not found." });
        }

        var trainerId = await _userAiChatService.ResolveTrainerAssistantIdAsync(cancellationToken);
        if (trainerId is null)
        {
            return new NotFoundObjectResult(new { message = "Trainer assistant not found." });
        }

        try
        {
            var response = await _userAiChatService.CompleteTrainerPromptAsync(
                currentUser.Id,
                [new KernestalAiService.AiChatMessage("user", TrainingSummaryGenerationUserPrompt)],
                cancellationToken,
                request?.Model);

            var summary = TruncateForSummary(response, 8000);
            currentUser.AiSummary = summary;
            var updatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new OkObjectResult(new { summary, updatedAtUtc });
        }
        catch (InvalidOperationException ex)
        {
            return new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status502BadGateway };
        }
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

    private async Task<AppUser?> ResolveCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.Identity?.Name?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    private async Task<ChatDialog?> ResolveExistingDialogAsync(Guid userId, Guid? dialogId, CancellationToken cancellationToken)
    {
        if (dialogId is null || dialogId == Guid.Empty)
        {
            return null;
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
        if (!values.TryGetValue("weight", out var raw))
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
