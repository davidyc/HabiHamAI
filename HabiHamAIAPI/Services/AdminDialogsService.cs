using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class AdminDialogsService : IAdminDialogsService
{
    private readonly AppDbContext _dbContext;

    public AdminDialogsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> GetDialogsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var query = _dbContext.ChatDialogs
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        var dialogs = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                userId = x.UserId,
                username = x.User != null ? x.User.Username : string.Empty,
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

    public async Task<IActionResult> GetDialogMessagesAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var dialogExists = await _dbContext.ChatDialogs
            .AsNoTracking()
            .AnyAsync(x => x.Id == dialogId, cancellationToken);
        if (!dialogExists)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        var messages = await _dbContext.ChatMessages
            .AsNoTracking()
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

    public async Task<IActionResult> CreateDialogAsync(AdminUpsertDialogRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return new BadRequestObjectResult(new { message = "UserId is required." });
        }

        var userExists = await _dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return new NotFoundObjectResult(new { message = "User not found." });
        }

        var now = DateTime.UtcNow;
        var dialog = new ChatDialog
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Новый диалог" : request.Title.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.ChatDialogs.Add(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new
        {
            id = dialog.Id,
            userId = dialog.UserId,
            title = dialog.Title,
            createdAtUtc = dialog.CreatedAtUtc,
            updatedAtUtc = dialog.UpdatedAtUtc
        });
    }

    public async Task<IActionResult> RenameDialogAsync(Guid dialogId, AdminUpsertDialogRequest request, CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.ChatDialogs.FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        dialog.Title = string.IsNullOrWhiteSpace(request.Title) ? dialog.Title : request.Title.Trim();
        dialog.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new
        {
            id = dialog.Id,
            userId = dialog.UserId,
            title = dialog.Title,
            createdAtUtc = dialog.CreatedAtUtc,
            updatedAtUtc = dialog.UpdatedAtUtc
        });
    }

    public async Task<IActionResult> DeleteDialogAsync(Guid dialogId, CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.ChatDialogs.FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
        if (dialog is null)
        {
            return new NotFoundObjectResult(new { message = "Dialog not found." });
        }

        _dbContext.ChatDialogs.Remove(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OkObjectResult(new { message = "Dialog deleted." });
    }
}
