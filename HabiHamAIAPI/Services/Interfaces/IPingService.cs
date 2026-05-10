using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public interface IPingService
{
    IActionResult Ping();
}
