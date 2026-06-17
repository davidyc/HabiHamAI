namespace HabiHamAIAPI.Services.Ai;

public interface ILlmModelService
{
    Task<IReadOnlyList<string>> GetActiveModelNamesAsync(CancellationToken cancellationToken = default);
    Task<string?> GetDefaultModelNameAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAllowedModelAsync(string model, CancellationToken cancellationToken = default);
    static string? NormalizeModelOrNull(string? model)
    {
        var normalized = (model ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
