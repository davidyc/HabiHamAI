namespace HabiHamAIAPI.Models;

public sealed class AiAssistantFieldDefinition
{
    public Guid Id { get; set; }
    public Guid AiAssistantId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public AiAssistant? AiAssistant { get; set; }
}
