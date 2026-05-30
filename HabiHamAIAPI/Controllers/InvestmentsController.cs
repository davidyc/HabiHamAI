using HabiHamAIAPI.Authorization;
using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("users/me/investments")]
[RequirePermission(AppPermissionCatalog.Investments)]
public sealed class InvestmentsController : ControllerBase
{
    private readonly IInvestmentsService _service;

    public InvestmentsController(IInvestmentsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _service.ListAsync(User, cancellationToken);

    [HttpGet("summary")]
    public Task<IActionResult> GetSummary(CancellationToken cancellationToken) =>
        _service.GetSummaryAsync(User, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _service.GetAsync(User, id, cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateUserInvestmentRequest request, CancellationToken cancellationToken) =>
        _service.CreateAsync(User, request, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateUserInvestmentRequest request, CancellationToken cancellationToken) =>
        _service.UpdateAsync(User, id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(User, id, cancellationToken);
}
