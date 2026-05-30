using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAppPermissionService _permissionService;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        ITokenService tokenService,
        IAppPermissionService permissionService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Username and password are required." });
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .Include(x => x.RoleAssignments)
            .FirstOrDefaultAsync(x => x.Username == normalizedUsername);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "Invalid credentials." });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new UnauthorizedObjectResult(new { message = "Invalid credentials." });
        }

        var permissions = await _permissionService.ResolvePermissionsForUserAsync(user);
        var token = _tokenService.GenerateToken(
            user.Username,
            AppUserRoleHelper.GetRoles(user),
            permissions);
        return new OkObjectResult(new
        {
            accessToken = token,
            tokenType = "Bearer",
            permissions,
        });
    }

    public async Task<IActionResult> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Username and password are required." });
        }

        if (request.Password.Length < 6)
        {
            return new BadRequestObjectResult(new { message = "Password must be at least 6 characters." });
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
            CreatedAtUtc = DateTime.UtcNow
        };
        AppUserRoleHelper.SetRoles(user, [AppUserRoleHelper.DefaultRoleName]);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new OkObjectResult(new
        {
            message = "User created.",
            roles = AppUserRoleHelper.GetRoleNames(user),
        });
    }
}
