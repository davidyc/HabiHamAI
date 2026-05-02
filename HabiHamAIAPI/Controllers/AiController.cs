using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HabiHamAIAPI.Data;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("ai")]
[Authorize(Roles = "Admin,AiUser")]
public sealed class AiController : ControllerBase
{
    private readonly KernestalAiService _kernestalAiService;
    private readonly AppDbContext _dbContext;

    public AiController(KernestalAiService kernestalAiService, AppDbContext dbContext)
    {
        _kernestalAiService = kernestalAiService;
        _dbContext = dbContext;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { message = "Prompt is required." });
        }

        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var dialog = await ResolveOrCreateDialogAsync(currentUser.Id, request.DialogId, cancellationToken);
        if (dialog is null)
        {
            return NotFound(new { message = "Dialog not found." });
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

        var allMessages = await _dbContext.ChatMessages
            .Where(x => x.DialogId == dialog.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new KernestalAiService.AiChatMessage(x.Role, x.Content))
            .ToListAsync(cancellationToken);

        try
        {
            var response = await _kernestalAiService.GetCompletionAsync(allMessages, cancellationToken);

            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                DialogId = dialog.Id,
                Role = "assistant",
                Content = response,
                CreatedAtUtc = DateTime.UtcNow
            };
            _dbContext.ChatMessages.Add(assistantMessage);
            dialog.UpdatedAtUtc = DateTime.UtcNow;
            currentUser.AiSummary = TruncateForSummary(response, 8000);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                dialogId = dialog.Id,
                dialogTitle = dialog.Title,
                response
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("dialogs")]
    public async Task<IActionResult> GetDialogs(CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var dialogs = await _dbContext.ChatDialogs
            .Where(x => x.UserId == currentUser.Id)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                title = x.Title,
                createdAtUtc = x.CreatedAtUtc,
                updatedAtUtc = x.UpdatedAtUtc,
                messagesCount = x.Messages.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(dialogs);
    }

    [HttpGet("dialogs/{dialogId:guid}/messages")]
    public async Task<IActionResult> GetDialogMessages(Guid dialogId, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var dialogExists = await _dbContext.ChatDialogs
            .AnyAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (!dialogExists)
        {
            return NotFound(new { message = "Dialog not found." });
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

        return Ok(messages);
    }

    [HttpPost("dialogs")]
    public async Task<IActionResult> CreateDialog([FromBody] CreateDialogRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
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

        return Ok(new { id = dialog.Id, title = dialog.Title, createdAtUtc = dialog.CreatedAtUtc });
    }

    [HttpPut("dialogs/{dialogId:guid}")]
    public async Task<IActionResult> RenameDialog(Guid dialogId, [FromBody] RenameDialogRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title is required." });
        }

        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var dialog = await _dbContext.ChatDialogs
            .FirstOrDefaultAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (dialog is null)
        {
            return NotFound(new { message = "Dialog not found." });
        }

        dialog.Title = BuildTitleOrDefault(request.Title);
        dialog.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { id = dialog.Id, title = dialog.Title, updatedAtUtc = dialog.UpdatedAtUtc });
    }

    [HttpDelete("dialogs/{dialogId:guid}")]
    public async Task<IActionResult> DeleteDialog(Guid dialogId, CancellationToken cancellationToken)
    {
        var currentUser = await ResolveCurrentUserAsync(cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized(new { message = "User not found." });
        }

        var dialog = await _dbContext.ChatDialogs
            .FirstOrDefaultAsync(x => x.Id == dialogId && x.UserId == currentUser.Id, cancellationToken);
        if (dialog is null)
        {
            return NotFound(new { message = "Dialog not found." });
        }

        _dbContext.ChatDialogs.Remove(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Dialog deleted." });
    }

    private async Task<AppUser?> ResolveCurrentUserAsync(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name?.Trim().ToLowerInvariant();
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
        if (t.Length <= maxLength)
        {
            return t;
        }

        return t[..maxLength];
    }
}
