namespace HabiHamAIAPI.Models;

public sealed class CreateDialogRequest
{
    public string? Title { get; set; }
}

public sealed class RenameDialogRequest
{
    public string Title { get; set; } = string.Empty;
}

public sealed class AdminUpsertDialogRequest
{
    public Guid UserId { get; set; }
    public string? Title { get; set; }
}
