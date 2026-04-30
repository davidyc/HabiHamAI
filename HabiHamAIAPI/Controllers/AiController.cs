using HabiHamAIAPI.Models;
using HabiHamAIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabiHamAIAPI.Controllers;

[ApiController]
[Route("ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly KernestalAiService _kernestalAiService;

    public AiController(KernestalAiService kernestalAiService)
    {
        _kernestalAiService = kernestalAiService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        var hasAiAccess = User.IsInRole("Admin") || User.IsInRole("AiUser");
        if (!hasAiAccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "К сожалению ты не иммешь прав к лрступа к ии асистениу."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { message = "Prompt is required." });
        }

        try
        {
            var response = await _kernestalAiService.GetCompletionAsync(request.Prompt, cancellationToken);
            return Ok(new { response });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }
}
