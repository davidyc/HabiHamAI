using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Services;

public sealed class PingService : IPingService
{
    public IActionResult Ping()
    {
        return new OkObjectResult(new { status = "ok", message = "pong" });
    }
}
