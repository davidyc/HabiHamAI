namespace HabiHamAIAPI.Models;

public sealed class UserBikeActivity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public string Sport { get; set; } = string.Empty;
    public string? Notes { get; set; }

    /// <summary>Activity Id from TCX (typically session start in UTC).</summary>
    public DateTime StartTimeUtc { get; set; }

    public double? TotalSeconds { get; set; }
    public double? DistanceMeters { get; set; }
    public double? Calories { get; set; }
    public int? AverageHeartRateBpm { get; set; }
    public int? MaxHeartRateBpm { get; set; }
    public string? Intensity { get; set; }
    public string? TriggerMethod { get; set; }

    public DateTime ImportedAtUtc { get; set; }
    public int TrackpointCount { get; set; }

    public List<UserBikeActivityTrackPoint> TrackPoints { get; set; } = [];
}
