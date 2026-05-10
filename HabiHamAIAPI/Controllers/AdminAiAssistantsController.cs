using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/ai-assistants")]
[Authorize(Roles = "Admin")]
public sealed class AdminAiAssistantsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AdminAiAssistantsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.AiAssistants
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(rows.ConvertAll(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.AiAssistants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return row is null ? NotFound(new { message = "Assistant not found." }) : Ok(MapToResponse(row));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCreateAiAssistantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            return BadRequest(new { message = "SystemPrompt is required." });
        }

        var now = DateTime.UtcNow;
        var entity = new AiAssistant
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SystemPrompt = request.SystemPrompt.Trim(),
            SettingsJson = string.IsNullOrWhiteSpace(request.SettingsJson) ? null : request.SettingsJson.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            IsSystem = false,
            CreatedAtUtc = now
        };

        _dbContext.AiAssistants.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, MapToResponse(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateAiAssistantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            return BadRequest(new { message = "SystemPrompt is required." });
        }

        var entity = await _dbContext.AiAssistants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "Assistant not found." });
        }

        var wasActive = entity.IsActive;
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.SystemPrompt = request.SystemPrompt.Trim();
        entity.SettingsJson = string.IsNullOrWhiteSpace(request.SettingsJson) ? null : request.SettingsJson.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        if (wasActive && !entity.IsActive)
        {
            await _dbContext.Users
                .Where(u => u.SelectedAiAssistantId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SelectedAiAssistantId, (Guid?)null), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AiAssistants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "Assistant not found." });
        }

        if (entity.IsSystem)
        {
            return BadRequest(new { message = "Нельзя удалить встроенного помощника." });
        }

        _dbContext.AiAssistants.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Assistant deleted." });
    }

    private static AdminAiAssistantResponse MapToResponse(AiAssistant x) =>
        new()
        {
            Id = x.Id,
            AssistantCode = x.AssistantCode,
            Name = x.Name,
            Description = x.Description,
            SystemPrompt = x.SystemPrompt,
            SettingsJson = x.SettingsJson,
            SortOrder = x.SortOrder,
            IsActive = x.IsActive,
            IsSystem = x.IsSystem,
            CreatedAtUtc = x.CreatedAtUtc
        };
}
