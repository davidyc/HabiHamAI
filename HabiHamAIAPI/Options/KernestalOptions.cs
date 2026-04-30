namespace HabiHamAIAPI.Options;

public sealed class KernestalOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public string ChatCompletionsPath { get; set; } = "/v1/chat/completions";
}
