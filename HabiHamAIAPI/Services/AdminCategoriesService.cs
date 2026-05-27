using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class AdminCategoriesService : IAdminCategoriesService
{
    private readonly AppDbContext _dbContext;

    public AdminCategoriesService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.UserCategories
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(MapToResponseExpression())
            .ToListAsync(cancellationToken);

        return new OkObjectResult(rows);
    }

    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.UserCategories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapToResponseExpression())
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? new NotFoundObjectResult(new { message = "Category not found." })
            : new OkObjectResult(row);
    }

    public async Task<IActionResult> CreateAsync(
        AdminCreateUserCategoryRequest request,
        string getActionName,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return new BadRequestObjectResult(new { message = "Category name is invalid." });
        }

        var exists = await _dbContext.UserCategories
            .AsNoTracking()
            .AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return new ConflictObjectResult(new { message = "Category with this name already exists." });
        }

        var now = DateTime.UtcNow;
        var maxSortOrder = await _dbContext.UserCategories
            .AsNoTracking()
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var row = new UserCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder ?? (maxSortOrder + 1),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.UserCategories.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedAtActionResult(getActionName, "AdminCategories", new { id = row.Id }, MapToResponse(row));
    }

    public async Task<IActionResult> UpdateAsync(Guid id, AdminUpdateUserCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return new BadRequestObjectResult(new { message = "Category name is invalid." });
        }

        var row = await _dbContext.UserCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Category not found." });
        }

        var nameTaken = await _dbContext.UserCategories
            .AsNoTracking()
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (nameTaken)
        {
            return new ConflictObjectResult(new { message = "Category with this name already exists." });
        }

        row.Name = name;
        row.Description = NormalizeDescription(request.Description);
        row.IsActive = request.IsActive;
        row.SortOrder = request.SortOrder;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(MapToResponse(row));
    }

    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.UserCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return new NotFoundObjectResult(new { message = "Category not found." });
        }

        _dbContext.UserCategories.Remove(row);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(new { message = "Category deleted." });
    }

    private static System.Linq.Expressions.Expression<Func<UserCategory, UserCategoryResponse>> MapToResponseExpression()
    {
        return x => new UserCategoryResponse
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
    }

    private static UserCategoryResponse MapToResponse(UserCategory x)
    {
        return new UserCategoryResponse
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive,
            SortOrder = x.SortOrder,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
    }

    private static string? NormalizeName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 100 ? null : value;
    }

    private static string? NormalizeDescription(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= 300 ? normalized : normalized[..300];
    }
}
