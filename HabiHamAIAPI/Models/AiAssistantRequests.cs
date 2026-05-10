namespace HabiHamAIAPI.Models;

public sealed class AiAssistantSelectionRequest
{
    public Guid? AssistantId { get; set; }
}

public sealed class AdminCreateAiAssistantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AdminUpdateAiAssistantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AdminAiAssistantResponse
{
    public Guid Id { get; set; }
    public string? AssistantCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string? SettingsJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AdminUpsertAiAssistantExtraFieldRequest
{
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class AdminAiAssistantExtraFieldResponse
{
    public Guid Id { get; set; }
    public Guid AiAssistantId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class UserAiAssistantExtrasPutRequest
{
    public Guid AssistantId { get; set; }
    public Dictionary<string, string>? Values { get; set; }
}
