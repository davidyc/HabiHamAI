using System.Globalization;
using System.Xml.Linq;

namespace HabiHamAIAPI.Services.BikeActivities;

internal static class TcxActivityParser
{
    private static readonly XNamespace Tcd = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
    private static readonly XNamespace ActExt = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";

    internal sealed record ParsedTrackPoint(
        DateTime TimeUtc,
        double? Latitude,
        double? Longitude,
        double? AltitudeMeters,
        int? HeartRateBpm,
        int? Cadence,
        double? SpeedMetersPerSecond);

    internal sealed record ParsedActivity(
        string Sport,
        string? Notes,
        DateTime ActivityIdUtc,
        double? TotalTimeSeconds,
        double? DistanceMeters,
        double? Calories,
        int? AverageHeartRateBpm,
        int? MaxHeartRateBpm,
        string? Intensity,
        string? TriggerMethod,
        IReadOnlyList<ParsedTrackPoint> Trackpoints);

    internal static ParsedActivity Parse(Stream stream)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Не удалось прочитать XML (ожидается TCX).", ex);
        }

        var root = doc.Root;
        if (root is null)
        {
            throw new InvalidOperationException("Пустой TCX файл.");
        }

        var activity = root.Element(Tcd + "Activities")?.Elements(Tcd + "Activity").FirstOrDefault();
        if (activity is null)
        {
            throw new InvalidOperationException("В TCX не найден элемент Activity.");
        }

        var sport = activity.Attribute("Sport")?.Value?.Trim() ?? string.Empty;
        var notes = activity.Element(Tcd + "Notes")?.Value?.Trim();
        var idEl = activity.Element(Tcd + "Id")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(idEl) || !DateTime.TryParse(idEl, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var activityIdUtc))
        {
            throw new InvalidOperationException("В TCX отсутствует или некорректен Activity/Id.");
        }

        if (activityIdUtc.Kind == DateTimeKind.Unspecified)
        {
            activityIdUtc = DateTime.SpecifyKind(activityIdUtc, DateTimeKind.Utc);
        }
        else
        {
            activityIdUtc = activityIdUtc.ToUniversalTime();
        }

        var laps = activity.Elements(Tcd + "Lap").ToList();
        if (laps.Count == 0)
        {
            throw new InvalidOperationException("В TCX нет секций Lap.");
        }

        double? totalTime = null;
        double? distance = null;
        double? calories = null;
        int? maxHr = null;
        string? intensity = null;
        string? trigger = null;

        double weightedHrSum = 0;
        double weightedHrWeight = 0;

        foreach (var lap in laps)
        {
            totalTime = AddNullableDouble(totalTime, ParseDouble(lap.Element(Tcd + "TotalTimeSeconds")?.Value));
            distance = AddNullableDouble(distance, ParseDouble(lap.Element(Tcd + "DistanceMeters")?.Value));
            calories = AddNullableDouble(calories, ParseDouble(lap.Element(Tcd + "Calories")?.Value));
            intensity ??= lap.Element(Tcd + "Intensity")?.Value?.Trim();
            trigger ??= lap.Element(Tcd + "TriggerMethod")?.Value?.Trim();

            var lapAvg = ParseInt(lap.Element(Tcd + "AverageHeartRateBpm")?.Element(Tcd + "Value")?.Value);
            var lapMax = ParseInt(lap.Element(Tcd + "MaximumHeartRateBpm")?.Element(Tcd + "Value")?.Value);
            maxHr = MaxNullable(maxHr, lapMax);

            var lapSec = ParseDouble(lap.Element(Tcd + "TotalTimeSeconds")?.Value);
            if (lapAvg.HasValue && lapSec is > 0)
            {
                weightedHrSum += lapAvg.Value * lapSec.Value;
                weightedHrWeight += lapSec.Value;
            }
        }

        int? avgHr = weightedHrWeight > 0
            ? (int)Math.Round(weightedHrSum / weightedHrWeight, MidpointRounding.AwayFromZero)
            : ParseInt(laps[0].Element(Tcd + "AverageHeartRateBpm")?.Element(Tcd + "Value")?.Value);

        if (!maxHr.HasValue)
        {
            maxHr = ParseInt(laps[0].Element(Tcd + "MaximumHeartRateBpm")?.Element(Tcd + "Value")?.Value);
        }

        var points = new List<ParsedTrackPoint>(4096);
        foreach (var lap in laps)
        {
            var track = lap.Element(Tcd + "Track");
            if (track is null)
            {
                continue;
            }

            foreach (var tp in track.Elements(Tcd + "Trackpoint"))
            {
                points.Add(ParseTrackpoint(tp));
            }
        }

        return new ParsedActivity(
            sport,
            notes,
            activityIdUtc,
            totalTime,
            distance,
            calories,
            avgHr,
            maxHr,
            intensity,
            trigger,
            points);
    }

    private static ParsedTrackPoint ParseTrackpoint(XElement tp)
    {
        var timeStr = tp.Element(Tcd + "Time")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(timeStr) || !DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t))
        {
            throw new InvalidOperationException("Некорректный Trackpoint/Time в TCX.");
        }

        if (t.Kind == DateTimeKind.Unspecified)
        {
            t = DateTime.SpecifyKind(t, DateTimeKind.Utc);
        }
        else
        {
            t = t.ToUniversalTime();
        }

        double? lat = null;
        double? lon = null;
        var pos = tp.Element(Tcd + "Position");
        if (pos is not null)
        {
            lat = ParseDouble(pos.Element(Tcd + "LatitudeDegrees")?.Value);
            lon = ParseDouble(pos.Element(Tcd + "LongitudeDegrees")?.Value);
        }

        var alt = ParseDouble(tp.Element(Tcd + "AltitudeMeters")?.Value);
        var cad = ParseInt(tp.Element(Tcd + "Cadence")?.Value);
        var hr = ParseInt(tp.Element(Tcd + "HeartRateBpm")?.Element(Tcd + "Value")?.Value);

        double? speed = null;
        var ext = tp.Element(Tcd + "Extensions");
        if (ext is not null)
        {
            var tpx = ext.Element(ActExt + "TPX");
            speed = ParseDouble(tpx?.Element(ActExt + "Speed")?.Value);
        }

        return new ParsedTrackPoint(t, lat, lon, alt, hr, cad, speed);
    }

    private static double? ParseDouble(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static int? ParseInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static double? AddNullableDouble(double? a, double? b)
    {
        if (!a.HasValue)
        {
            return b;
        }

        if (!b.HasValue)
        {
            return a;
        }

        return a.Value + b.Value;
    }

    private static int? MaxNullable(int? a, int? b)
    {
        if (!a.HasValue)
        {
            return b;
        }

        if (!b.HasValue)
        {
            return a;
        }

        return Math.Max(a.Value, b.Value);
    }
}
