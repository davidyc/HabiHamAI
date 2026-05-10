using System.Text.RegularExpressions;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("admin/ai-assistants/{assistantId:guid}/extra-fields")]
[Authorize(Roles = "Admin")]
public sealed class AdminAiAssistantExtraFieldsController : ControllerBase
{
    private static readonly Regex FieldKeyRegex = new("^[a-z0-9_]{1,80}$", RegexOptions.Compiled);

    private readonly AppDbContext _dbContext;

    public AdminAiAssistantExtraFieldsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid assistantId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.AiAssistants.AnyAsync(x => x.Id == assistantId, cancellationToken);
        if (!exists)
        {
            return NotFound(new { message = "Assistant not found." });
        }

        var rows = await _dbContext.AiAssistantFieldDefinitions
            .AsNoTracking()
            .Where(x => x.AiAssistantId == assistantId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .ToListAsync(cancellationToken);

        return Ok(rows.ConvertAll(MapToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid assistantId, [FromBody] AdminUpsertAiAssistantExtraFieldRequest request, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.AiAssistants.AnyAsync(x => x.Id == assistantId, cancellationToken);
        if (!exists)
        {
            return NotFound(new { message = "Assistant not found." });
        }

        var key = NormalizeFieldKey(request.FieldKey);
        if (key is null)
        {
            return BadRequest(new { message = "FieldKey must be 1–80 chars: lowercase letters, digits, underscore." });
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest(new { message = "Label is required." });
        }

        var type = NormalizeFieldType(request.FieldType);
        if (type is null)
        {
            return BadRequest(new { message = "FieldType must be text, textarea, or number." });
        }

        var duplicate = await _dbContext.AiAssistantFieldDefinitions
            .AnyAsync(x => x.AiAssistantId == assistantId && x.FieldKey == key, cancellationToken);
        if (duplicate)
        {
            return Conflict(new { message = "FieldKey already exists for this assistant." });
        }

        var entity = new AiAssistantFieldDefinition
        {
            Id = Guid.NewGuid(),
            AiAssistantId = assistantId,
            FieldKey = key,
            Label = request.Label.Trim(),
            FieldType = type,
            SortOrder = request.SortOrder,
            IsRequired = request.IsRequired,
            IsSystem = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AiAssistantFieldDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), new { assistantId }, MapToResponse(entity));
    }

    [HttpPut("{fieldId:guid}")]
    public async Task<IActionResult> Update(Guid assistantId, Guid fieldId, [FromBody] AdminUpsertAiAssistantExtraFieldRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AiAssistantFieldDefinitions
            .FirstOrDefaultAsync(x => x.Id == fieldId && x.AiAssistantId == assistantId, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "Field not found." });
        }

        var key = NormalizeFieldKey(request.FieldKey);
        if (key is null)
        {
            return BadRequest(new { message = "FieldKey must be 1–80 chars: lowercase letters, digits, underscore." });
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest(new { message = "Label is required." });
        }

        var type = NormalizeFieldType(request.FieldType);
        if (type is null)
        {
            return BadRequest(new { message = "FieldType must be text, textarea, or number." });
        }

        if (key != entity.FieldKey)
        {
            var taken = await _dbContext.AiAssistantFieldDefinitions
                .AnyAsync(x => x.AiAssistantId == assistantId && x.FieldKey == key && x.Id != fieldId, cancellationToken);
            if (taken)
            {
                return Conflict(new { message = "FieldKey already exists for this assistant." });
            }
        }

        entity.FieldKey = key;
        entity.Label = request.Label.Trim();
        entity.FieldType = type;
        entity.SortOrder = request.SortOrder;
        entity.IsRequired = request.IsRequired;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(entity));
    }

    [HttpDelete("{fieldId:guid}")]
    public async Task<IActionResult> Delete(Guid assistantId, Guid fieldId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.AiAssistantFieldDefinitions
            .FirstOrDefaultAsync(x => x.Id == fieldId && x.AiAssistantId == assistantId, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "Field not found." });
        }

        if (entity.IsSystem)
        {
            return BadRequest(new { message = "Нельзя удалить поле встроенного помощника." });
        }

        _dbContext.AiAssistantFieldDefinitions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Field deleted." });
    }

    private static AdminAiAssistantExtraFieldResponse MapToResponse(AiAssistantFieldDefinition x) =>
        new()
        {
            Id = x.Id,
            AiAssistantId = x.AiAssistantId,
            FieldKey = x.FieldKey,
            Label = x.Label,
            FieldType = x.FieldType,
            SortOrder = x.SortOrder,
            IsRequired = x.IsRequired,
            IsSystem = x.IsSystem,
            CreatedAtUtc = x.CreatedAtUtc
        };

    private static string? NormalizeFieldKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var s = raw.Trim().ToLowerInvariant().Replace(' ', '_');
        return FieldKeyRegex.IsMatch(s) ? s : null;
    }

    private static string? NormalizeFieldType(string? raw)
    {
        var t = (raw ?? "text").Trim().ToLowerInvariant();
        return t switch
        {
            "text" => "text",
            "textarea" => "textarea",
            "number" => "number",
            _ => null
        };
    }
}
