using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services.Ai;

public sealed class AdminLlmModelsService : IAdminLlmModelsService
{
    private readonly AppDbContext _dbContext;

    public AdminLlmModelsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.LlmModels
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows.ConvertAll(MapToResponse));
    }

    public async Task<IActionResult> CreateAsync(
        AdminCreateLlmModelRequest request,
        string getActionName,
        CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BadRequestObjectResult(new { message = "Name is required." });
        }

        if (name.Length > 128)
        {
            return new BadRequestObjectResult(new { message = "Name is too long." });
        }

        var exists = await _dbContext.LlmModels
            .AnyAsync(x => x.Name == name, cancellationToken);
        if (exists)
        {
            return new BadRequestObjectResult(new { message = "Model with this name already exists." });
        }

        var now = DateTime.UtcNow;
        var entity = new LlmModel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Label = NormalizeLabel(request.Label),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            IsDefault = request.IsDefault,
            CreatedAtUtc = now
        };

        if (entity.IsDefault)
        {
            await ClearDefaultFlagsAsync(cancellationToken);
        }
        else if (!await _dbContext.LlmModels.AnyAsync(cancellationToken))
        {
            entity.IsDefault = true;
        }

        _dbContext.LlmModels.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedAtActionResult(getActionName, "AdminLlmModels", new { id = entity.Id }, MapToResponse(entity));
    }

    public async Task<IActionResult> UpdateAsync(
        Guid id,
        AdminUpdateLlmModelRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LlmModels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return new NotFoundObjectResult(new { message = "Model not found." });
        }

        if (!request.IsActive && entity.IsDefault)
        {
            return new BadRequestObjectResult(new { message = "Нельзя деактивировать модель по умолчанию. Сначала назначьте другую." });
        }

        if (!request.IsActive)
        {
            var inUse = await _dbContext.AiAssistants
                .AnyAsync(x => x.Model == entity.Name, cancellationToken);
            if (inUse)
            {
                return new BadRequestObjectResult(new { message = "Модель используется помощниками. Сначала смените модель у них." });
            }
        }

        entity.Label = NormalizeLabel(request.Label);
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        if (request.IsDefault)
        {
            await ClearDefaultFlagsAsync(cancellationToken);
            entity.IsDefault = true;
            entity.IsActive = true;
        }
        else if (entity.IsDefault)
        {
            var otherActive = await _dbContext.LlmModels
                .AnyAsync(x => x.Id != id && x.IsActive, cancellationToken);
            if (!otherActive)
            {
                return new BadRequestObjectResult(new { message = "Должна остаться хотя бы одна активная модель по умолчанию." });
            }

            entity.IsDefault = false;
            var nextDefault = await _dbContext.LlmModels
                .Where(x => x.Id != id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextDefault is not null)
            {
                nextDefault.IsDefault = true;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(MapToResponse(entity));
    }

    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LlmModels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return new NotFoundObjectResult(new { message = "Model not found." });
        }

        if (entity.IsDefault)
        {
            return new BadRequestObjectResult(new { message = "Нельзя удалить модель по умолчанию. Сначала назначьте другую." });
        }

        var total = await _dbContext.LlmModels.CountAsync(cancellationToken);
        if (total <= 1)
        {
            return new BadRequestObjectResult(new { message = "Нельзя удалить единственную модель." });
        }

        var inUse = await _dbContext.AiAssistants
            .AnyAsync(x => x.Model == entity.Name, cancellationToken);
        if (inUse)
        {
            return new BadRequestObjectResult(new { message = "Модель используется помощниками. Сначала смените модель у них." });
        }

        _dbContext.LlmModels.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Model deleted." });
    }

    private async Task ClearDefaultFlagsAsync(CancellationToken cancellationToken) =>
        await _dbContext.LlmModels
            .Where(x => x.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);

    private static string? NormalizeLabel(string? label)
    {
        var normalized = (label ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static AdminLlmModelResponse MapToResponse(LlmModel x) =>
        new()
        {
            Id = x.Id,
            Name = x.Name,
            Label = x.Label,
            IsDefault = x.IsDefault,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreatedAtUtc = x.CreatedAtUtc
        };
}
