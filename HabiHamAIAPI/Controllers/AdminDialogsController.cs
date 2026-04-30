using HabiHamAIAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/dialogs")]
[Authorize(Roles = "Admin")]
public sealed class AdminDialogsController : ControllerBase
{
    public sealed class UpsertAdminDialogRequest
    {
        public Guid UserId { get; set; }
        public string? Title { get; set; }
    }

    private readonly AppDbContext _dbContext;

    public AdminDialogsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetDialogs([FromQuery] Guid? userId, CancellationToken cancellationToken)
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
                createdAtUtc = x.CreatedAtUtc,
                updatedAtUtc = x.UpdatedAtUtc,
                messagesCount = x.Messages.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(dialogs);
    }

    [HttpGet("{dialogId:guid}/messages")]
    public async Task<IActionResult> GetDialogMessages(Guid dialogId, CancellationToken cancellationToken)
    {
        var dialogExists = await _dbContext.ChatDialogs
            .AsNoTracking()
            .AnyAsync(x => x.Id == dialogId, cancellationToken);
        if (!dialogExists)
        {
            return NotFound(new { message = "Dialog not found." });
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

        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDialog([FromBody] UpsertAdminDialogRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new { message = "UserId is required." });
        }

        var userExists = await _dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return NotFound(new { message = "User not found." });
        }

        var now = DateTime.UtcNow;
        var dialog = new Models.ChatDialog
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Новый диалог" : request.Title.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.ChatDialogs.Add(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = dialog.Id,
            userId = dialog.UserId,
            title = dialog.Title,
            createdAtUtc = dialog.CreatedAtUtc,
            updatedAtUtc = dialog.UpdatedAtUtc
        });
    }

    [HttpPut("{dialogId:guid}")]
    public async Task<IActionResult> RenameDialog(Guid dialogId, [FromBody] UpsertAdminDialogRequest request, CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.ChatDialogs.FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
        if (dialog is null)
        {
            return NotFound(new { message = "Dialog not found." });
        }

        dialog.Title = string.IsNullOrWhiteSpace(request.Title) ? dialog.Title : request.Title.Trim();
        dialog.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = dialog.Id,
            userId = dialog.UserId,
            title = dialog.Title,
            createdAtUtc = dialog.CreatedAtUtc,
            updatedAtUtc = dialog.UpdatedAtUtc
        });
    }

    [HttpDelete("{dialogId:guid}")]
    public async Task<IActionResult> DeleteDialog(Guid dialogId, CancellationToken cancellationToken)
    {
        var dialog = await _dbContext.ChatDialogs.FirstOrDefaultAsync(x => x.Id == dialogId, cancellationToken);
        if (dialog is null)
        {
            return NotFound(new { message = "Dialog not found." });
        }

        _dbContext.ChatDialogs.Remove(dialog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Dialog deleted." });
    }
}
