using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users/me/bike-activities")]
[Authorize]
public sealed class BikeActivitiesController : ControllerBase
{
    private readonly IBikeActivitiesService _service;

    public BikeActivitiesController(IBikeActivitiesService service)
    {
        _service = service;
    }

    [HttpPost("import")]
    [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024)]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> ImportTcx(IFormFile? file, CancellationToken cancellationToken) =>
        _service.ImportTcxAsync(User, file, cancellationToken);

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sport,
        CancellationToken cancellationToken) =>
        _service.ListAsync(User, from, to, sport, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(
        Guid id,
        [FromQuery] int? trackpointLimit,
        CancellationToken cancellationToken) =>
        _service.GetByIdAsync(User, id, trackpointLimit, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(User, id, cancellationToken);
}
