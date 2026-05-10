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
            .OrderBy(x => x.CreatedAtUtc)
            .Select(MapToResponseExpression())
            .ToListAsync();

        return new OkObjectResult(users);
    }

    public async Task<IActionResult> GetUserAsync(Guid id)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
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

        if (!Enum.TryParse<AppUserRole>(request.Role, true, out var role))
        {
            return new BadRequestObjectResult(new { message = "Invalid role. Use Admin, User or AiUser." });
        }

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
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };
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

        if (!Enum.TryParse<AppUserRole>(request.Role, true, out var role))
        {
            return new BadRequestObjectResult(new { message = "Invalid role. Use Admin, User or AiUser." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
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
        user.Role = role;
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

    private static System.Linq.Expressions.Expression<Func<AppUser, AdminUserResponse>> MapToResponseExpression()
    {
        return user => new AdminUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
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
            Role = user.Role.ToString(),
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
