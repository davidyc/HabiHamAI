namespace HabiHamAIAPI.Models;

public sealed class UserBikeActivityTrackPoint
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public UserBikeActivity Activity { get; set; } = null!;

    public int OrderIndex { get; set; }
    public DateTime TimeUtc { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AltitudeMeters { get; set; }
    public int? HeartRateBpm { get; set; }
    public int? Cadence { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
}
