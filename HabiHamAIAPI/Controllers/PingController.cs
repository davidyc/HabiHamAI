using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class PingController : ControllerBase
{
    private readonly IPingService _service;

    public PingController(IPingService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Ping() => _service.Ping();
}
