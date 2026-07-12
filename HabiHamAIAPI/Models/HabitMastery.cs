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

    /// <summary>Longest consecutive run of done dates (any historical window in the set).</summary>
    public static int ComputeMaxStreakDays(IEnumerable<DateOnly> doneDates)
    {
        var ordered = doneDates.Distinct().OrderBy(d => d).ToList();
        if (ordered.Count == 0)
        {
            return 0;
        }

        var max = 1;
        var run = 1;
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i] == ordered[i - 1].AddDays(1))
            {
                run++;
                if (run > max)
                {
                    max = run;
                }
            }
            else
            {
                run = 1;
            }
        }

        return max;
    }

    public static bool ShouldMarkMastered(bool isMastered, int daysToMaster, int currentStreakDays) =>
        !isMastered && daysToMaster > 0 && currentStreakDays >= daysToMaster;
}
