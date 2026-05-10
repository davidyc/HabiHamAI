using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services.Ai;

public sealed class AiUserService : IAiUserService
{
    private static readonly Regex UserFieldPlaceholderRegex = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    private readonly IKernestalAiService _kernestalAiService;
    private readonly AppDbContext _dbContext;

    public AiUserService(IKernestalAiService kernestalAiService, AppDbContext dbContext)
    {
        _kernestalAiService = kernestalAiService;
        _dbContext = dbContext;
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

        var allMessages = await _dbContext.ChatMessages
            .Where(x => x.DialogId == dialog.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new KernestalAiService.AiChatMessage(x.Role, x.Content))
            .ToListAsync(cancellationToken);

        var messagesForLlm = await BuildMessagesWithSystemPromptAsync(
            currentUser.Id,
            assistantForChat,
            allMessages,
            cancellationToken);

        try
        {
            var response = await _kernestalAiService.GetCompletionAsync(messagesForLlm, cancellationToken);

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

    private async Task<AppUser?> ResolveCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var username = principal.Identity?.Name?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
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
}
