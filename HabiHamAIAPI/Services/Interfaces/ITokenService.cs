using HabiHamAIAPI.Models;

namespace HabiHamAIAPI.Services;

public interface ITokenService
{
    string GenerateToken(string username, IReadOnlyList<string> roles, IReadOnlyList<string> permissions);
}
