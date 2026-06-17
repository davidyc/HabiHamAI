using HabiHamAIAPI.Data;
using HabiHamAIAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Ai;

public sealed class LlmModelService : ILlmModelService
{
    private readonly AppDbContext _dbContext;
    private readonly KernestalOptions _kernestalOptions;

    public LlmModelService(AppDbContext dbContext, IOptions<KernestalOptions> kernestalOptions)
    {
        _dbContext = dbContext;
        _kernestalOptions = kernestalOptions.Value;
    }

    public async Task<IReadOnlyList<string>> GetActiveModelNamesAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.LlmModels
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (models.Count > 0)
        {
            return models;
        }

        return string.IsNullOrWhiteSpace(_kernestalOptions.Model)
            ? []
            : [_kernestalOptions.Model.Trim()];
    }

    public async Task<string?> GetDefaultModelNameAsync(CancellationToken cancellationToken = default)
    {
        var fromDb = await _dbContext.LlmModels
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsDefault)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(fromDb))
        {
            return fromDb;
        }

        var firstActive = await _dbContext.LlmModels
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(firstActive))
        {
            return firstActive;
        }

        return string.IsNullOrWhiteSpace(_kernestalOptions.Model)
            ? null
            : _kernestalOptions.Model.Trim();
    }

    public async Task<bool> IsAllowedModelAsync(string model, CancellationToken cancellationToken = default)
    {
        var normalized = ILlmModelService.NormalizeModelOrNull(model);
        if (normalized is null)
        {
            return false;
        }

        var hasCatalog = await _dbContext.LlmModels.AsNoTracking().AnyAsync(cancellationToken);
        if (!hasCatalog)
        {
            return string.Equals(normalized, _kernestalOptions.Model?.Trim(), StringComparison.Ordinal);
        }

        return await _dbContext.LlmModels
            .AsNoTracking()
            .AnyAsync(x => x.IsActive && x.Name == normalized, cancellationToken);
    }
}
