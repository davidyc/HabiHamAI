namespace HabiHamAIAPI.Options;

public sealed class TradernetOptions
{
    public string Domain { get; set; } = "freedom24.com";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
