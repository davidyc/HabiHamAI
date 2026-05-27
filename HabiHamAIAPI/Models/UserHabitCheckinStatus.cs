namespace HabiHamAIAPI.Models;

public static class UserHabitCheckinStatus
{
    public const string Partial = "partial";
    public const string Done = "done";
    public const string Failed = "failed";

    public static bool IsValid(string? status) =>
        status is Partial or Done or Failed;
}
