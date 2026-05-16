using HabiHamAIAPI.Services;

namespace HabiHamAIAPI.Services.Mcp;

public interface ITrainerToolInvoker
{
    IReadOnlyList<KernestalAiService.AiToolDefinition> GetToolDefinitions();

    Task<string> InvokeAsync(string toolName, string argumentsJson, CancellationToken cancellationToken);
}
