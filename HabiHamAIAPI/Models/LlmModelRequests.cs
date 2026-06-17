namespace HabiHamAIAPI.Models;

public sealed class AdminCreateLlmModelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class AdminUpdateLlmModelRequest
{
    public string? Label { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public sealed class AdminLlmModelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
