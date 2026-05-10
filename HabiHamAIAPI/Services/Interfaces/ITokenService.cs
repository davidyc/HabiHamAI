using HabiHamAIAPI.Models;

namespace HabiHamAIAPI.Services;

public interface ITokenService
{
    string GenerateToken(string username, AppUserRole role);
}
