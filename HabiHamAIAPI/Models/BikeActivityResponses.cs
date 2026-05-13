namespace HabiHamAIAPI.Models;

public class BikeActivitySummaryResponse
{
    public Guid Id { get; set; }
    public string Sport { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public double? TotalSeconds { get; set; }
    public double? DistanceMeters { get; set; }
    public double? Calories { get; set; }
    public int? AverageHeartRateBpm { get; set; }
    public int? MaxHeartRateBpm { get; set; }
    public int TrackpointCount { get; set; }
    public DateTime ImportedAtUtc { get; set; }
}

public sealed class BikeActivityDetailResponse : BikeActivitySummaryResponse
{
    public string? Intensity { get; set; }
    public string? TriggerMethod { get; set; }
    public IReadOnlyList<BikeActivityTrackPointResponse> Trackpoints { get; set; } = [];
}

public sealed class BikeActivityTrackPointResponse
{
    public DateTime TimeUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AltitudeMeters { get; set; }
    public int? HeartRateBpm { get; set; }
    public int? Cadence { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
}
