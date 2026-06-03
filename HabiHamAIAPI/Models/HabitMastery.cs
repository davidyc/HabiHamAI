namespace HabiHamAIAPI.Models;

internal static class HabitMastery
{
    public const int DefaultDaysToMaster = 21;
    public const int MinDaysToMaster = 0;
    public const int MaxDaysToMaster = 999;

    public static bool TryValidateDaysToMaster(int days, out string? error)
    {
        if (days is < MinDaysToMaster or > MaxDaysToMaster)
        {
            error = $"Days to master must be between {MinDaysToMaster} and {MaxDaysToMaster}.";
            return false;
        }

        error = null;
        return true;
    }

    public static int ComputeCurrentStreakDays(IEnumerable<DateOnly> doneDates, DateOnly asOfDate)
    {
        var doneSet = doneDates as HashSet<DateOnly> ?? new HashSet<DateOnly>(doneDates);
        var current = 0;
        var d = asOfDate;
        while (doneSet.Contains(d))
        {
            current++;
            d = d.AddDays(-1);
        }

        return current;
    }

    public static bool ShouldMarkMastered(bool isMastered, int daysToMaster, int currentStreakDays) =>
        !isMastered && daysToMaster > 0 && currentStreakDays >= daysToMaster;
}
