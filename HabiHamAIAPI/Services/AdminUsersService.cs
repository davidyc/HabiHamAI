using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class AdminUsersService : IAdminUsersService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AdminUsersService(AppDbContext dbContext, IPasswordHasher<AppUser> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<IActionResult> GetUsersAsync()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.RoleAssignments)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(MapToResponseExpression())
            .ToListAsync();

        return new OkObjectResult(users);
    }

    public async Task<IActionResult> GetUserAsync(Guid id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.RoleAssignments)
            .Where(x => x.Id == id)
            .Select(MapToResponseExpression())
            .FirstOrDefaultAsync();

        return user is null
            ? new NotFoundObjectResult(new { message = "User not found." })
            : new OkObjectResult(user);
    }

    public async Task<IActionResult> CreateUserAsync(AdminCreateUserRequest request, string getUserActionName)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Username and password are required." });
        }

        if (request.Password.Length < 6)
        {
            return new BadRequestObjectResult(new { message = "Password must be at least 6 characters." });
        }

        var roleNames = AppUserRoleHelper.ResolveRoleNamesFromRequest(request.Roles, request.Role).ToList();
        var validationError = await ValidateRoleNamesAsync(roleNames);
        if (validationError is not null)
        {
            return validationError;
        }

        var roles = AppUserRoleHelper.ParseRoleNames(roleNames);

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var exists = await _dbContext.Users.AnyAsync(x => x.Username == normalizedUsername);
        if (exists)
        {
            return new ConflictObjectResult(new { message = "User already exists." });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            CreatedAtUtc = DateTime.UtcNow
        };
        AppUserRoleHelper.SetRoles(user, roles);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new CreatedAtActionResult(getUserActionName, "AdminUsers", new { id = user.Id }, MapToResponse(user));
    }

    public async Task<IActionResult> UpdateUserAsync(Guid id, AdminUpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return new BadRequestObjectResult(new { message = "Username is required." });
        }

        var roleNames = AppUserRoleHelper.ResolveRoleNamesFromRequest(request.Roles, request.Role).ToList();
        var validationError = await ValidateRoleNamesAsync(roleNames);
        if (validationError is not null)
        {
            return validationError;
        }

        var roles = AppUserRoleHelper.ParseRoleNames(roleNames);

        var user = await _dbContext.Users
            .Include(x => x.RoleAssignments)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundObjectResult(new { message = "User not found." });
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var usernameTaken = await _dbContext.Users.AnyAsync(x => x.Username == normalizedUsername && x.Id != id);
        if (usernameTaken)
        {
            return new ConflictObjectResult(new { message = "Username already exists." });
        }

        user.Username = normalizedUsername;
        AppUserRoleHelper.SetRoles(user, roles);
        await _dbContext.SaveChangesAsync();

        return new OkObjectResult(MapToResponse(user));
    }

    public async Task<IActionResult> UpdateUserPasswordAsync(Guid id, AdminUpdateUserPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Password is required." });
        }

        if (request.Password.Length < 6)
        {
            return new BadRequestObjectResult(new { message = "Password must be at least 6 characters." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundObjectResult(new { message = "User not found." });
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _dbContext.SaveChangesAsync();

        return new OkObjectResult(new { message = "Password updated." });
    }

    public async Task<IActionResult> DeleteUserAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return new NotFoundObjectResult(new { message = "User not found." });
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return new OkObjectResult(new { message = "User deleted." });
    }

    private async Task<IActionResult?> ValidateRoleNamesAsync(IReadOnlyList<string> roleNames)
    {
        var activeRoles = await _dbContext.AppRoles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Name)
            .ToListAsync();

        var activeSet = new HashSet<string>(activeRoles, StringComparer.OrdinalIgnoreCase);
        if (roleNames.Any(name => !activeSet.Contains(name)))
        {
            return new BadRequestObjectResult(new { message = "One or more roles are invalid or inactive." });
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<AppUser, AdminUserResponse>> MapToResponseExpression()
    {
        return user => new AdminUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Roles = user.RoleAssignments.Select(r => r.RoleName).ToList(),
            CreatedAtUtc = user.CreatedAtUtc,
            BirthDate = user.BirthDate,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            Phone = user.Phone,
            City = user.City,
            About = user.About,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AiSummary = user.AiSummary
        };
    }

    private static AdminUserResponse MapToResponse(AppUser user)
    {
        return new AdminUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Roles = AppUserRoleHelper.GetRoleNames(user).ToList(),
            CreatedAtUtc = user.CreatedAtUtc,
            BirthDate = user.BirthDate,
            HeightCm = user.HeightCm,
            WeightKg = user.WeightKg,
            Phone = user.Phone,
            City = user.City,
            About = user.About,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AiSummary = user.AiSummary
        };
    }
}
