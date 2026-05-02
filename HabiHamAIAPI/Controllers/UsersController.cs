using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await GetCurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        return Ok(MapToProfileResponse(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
    {
        var user = await GetCurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "User is not authorized." });
        }

        if (request.HeightCm is < 0 or > 300)
        {
            return BadRequest(new { message = "Height must be between 0 and 300 cm." });
        }

        if (request.WeightKg is < 0 or > 700)
        {
            return BadRequest(new { message = "Weight must be between 0 and 700 kg." });
        }

        user.BirthDate = request.BirthDate;
        user.HeightCm = request.HeightCm;
        user.WeightKg = request.WeightKg;
        user.Phone = NormalizeOrNull(request.Phone, 30);
        user.City = NormalizeOrNull(request.City, 120);
        user.About = NormalizeOrNull(request.About, 500);
        user.FirstName = NormalizeOrNull(request.FirstName, 100);
        user.LastName = NormalizeOrNull(request.LastName, 100);

        await _dbContext.SaveChangesAsync();
        return Ok(MapToProfileResponse(user));
    }

    private async Task<AppUser?> GetCurrentUser()
    {
        var username = User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var normalizedUsername = username.Trim().ToLowerInvariant();
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Username == normalizedUsername);
    }

    private static string? NormalizeOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength];
    }

    private static UserProfileResponse MapToProfileResponse(AppUser user)
    {
        return new UserProfileResponse
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
