using HabiHamAIAPI.Options;
using Microsoft.Extensions.Options;

namespace HabiHamAIAPI.Services.Mcp;

public interface ITrainerAgentService
{
    Task<string> CompleteWithToolsAsync(
        Guid userId,
        Guid trainerAssistantId,
        IReadOnlyList<KernestalAiService.AiChatMessage> messages,
        CancellationToken cancellationToken);
}

public sealed class TrainerAgentService : ITrainerAgentService
{
    private readonly IKernestalAiService _kernestalAiService;
    private readonly ITrainerToolInvoker _toolInvoker;
    private readonly TrainerToolExecutionContext _context;
    private readonly TrainerMcpOptions _options;

    public TrainerAgentService(
        IKernestalAiService kernestalAiService,
        ITrainerToolInvoker toolInvoker,
        TrainerToolExecutionContext context,
        IOptions<TrainerMcpOptions> options)
    {
        _kernestalAiService = kernestalAiService;
        _toolInvoker = toolInvoker;
        _context = context;
        _options = options.Value;
    }

    public async Task<string> CompleteWithToolsAsync(
        Guid userId,
        Guid trainerAssistantId,
        IReadOnlyList<KernestalAiService.AiChatMessage> messages,
        CancellationToken cancellationToken)
    {
        _context.UserId = userId;
        _context.TrainerAssistantId = trainerAssistantId;

        var tools = _toolInvoker.GetToolDefinitions();
        var transcript = messages.ToList();
        var totalToolCalls = 0;

        for (var round = 0; round < _options.MaxAgentRounds; round++)
        {
            var result = await _kernestalAiService.GetCompletionWithToolsAsync(transcript, tools, cancellationToken);

            if (!result.HasToolCalls)
            {
                if (string.IsNullOrWhiteSpace(result.Content))
                {
                    throw new InvalidOperationException("LLM returned an empty response.");
                }

                return result.Content.Trim();
            }

            transcript.Add(new KernestalAiService.AiChatMessage("assistant", result.Content ?? string.Empty, result.ToolCalls));

            foreach (var call in result.ToolCalls)
            {
                if (totalToolCalls >= _options.MaxToolCallsPerChat)
                {
                    transcript.Add(new KernestalAiService.AiChatMessage(
                        "tool",
                        "{\"error\":\"Tool call limit reached for this chat turn.\"}",
                        null,
                        call.Id));
                    continue;
                }

                totalToolCalls++;
                var toolResult = await _toolInvoker.InvokeAsync(call.Name, call.ArgumentsJson, cancellationToken);
                transcript.Add(new KernestalAiService.AiChatMessage("tool", toolResult, null, call.Id));
            }
        }

        throw new InvalidOperationException("Trainer agent exceeded maximum tool rounds. Try a simpler question.");
    }
}
