namespace HabiHamAIAPI.Options;

public sealed class KernestalOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatCompletionsPath { get; set; } = "/v1/chat/completions";
    /// <summary>Запасная модель, если в БД нет каталога (только для dev/bootstrap).</summary>
    public string Model { get; set; } = "gpt-4";
}
