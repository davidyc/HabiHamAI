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

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new BadRequestObjectResult(new { message = "Username and password are required." });
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "Invalid credentials." });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return new UnauthorizedObjectResult(new { message = "Invalid credentials." });
        }

        var token = _tokenService.GenerateToken(user.Username, user.Role);
        return new OkObjectResult(new { accessToken = token, tokenType = "Bearer" });
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
            Role = AppUserRole.User,
            CreatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new OkObjectResult(new { message = "User created.", role = user.Role.ToString() });
    }
}
