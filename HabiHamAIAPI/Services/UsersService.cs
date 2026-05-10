using System.Security.Claims;
using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Services;

public sealed class UsersService : IUsersService
{
    private readonly AppDbContext _dbContext;

    public UsersService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> GetMyProfileAsync(ClaimsPrincipal principal)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        return new OkObjectResult(MapToProfileResponse(user));
    }

    public async Task<IActionResult> UpdateMyProfileAsync(ClaimsPrincipal principal, UpdateUserProfileRequest request)
    {
        var user = await GetCurrentUserAsync(principal);
        if (user is null)
        {
            return new UnauthorizedObjectResult(new { message = "User is not authorized." });
        }

        if (request.HeightCm is < 0 or > 300)
        {
            return new BadRequestObjectResult(new { message = "Height must be between 0 and 300 cm." });
        }

        if (request.WeightKg is < 0 or > 700)
        {
            return new BadRequestObjectResult(new { message = "Weight must be between 0 and 700 kg." });
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
        return new OkObjectResult(MapToProfileResponse(user));
    }

    private async Task<AppUser?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
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
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
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
