using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly TokenService _tokenService;

    public AuthController(
        AppDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        TokenService tokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = _tokenService.GenerateToken(user.Username, user.Role);
        return Ok(new { accessToken = token, tokenType = "Bearer" });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters." });
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var exists = await _dbContext.Users.AnyAsync(x => x.Username == normalizedUsername);
        if (exists)
        {
            return Conflict(new { message = "User already exists." });
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

        return Ok(new { message = "User created.", role = user.Role.ToString() });
    }
}
