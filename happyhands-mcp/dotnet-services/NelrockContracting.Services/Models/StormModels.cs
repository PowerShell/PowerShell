using System.Text.Json.Serialization;

namespace NelrockContracting.Services.Models;

// Storm Intelligence Models
public record StormSwathRequest
{
    public required BoundingBox BBox { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public string[] Hazards { get; init; } = Array.Empty<string>();
}

public record BoundingBox
{
    public double North { get; init; }
    public double South { get; init; }
    public double East { get; init; }
    public double West { get; init; }
}

public record StormSwathResponse
{
    public required GeoJsonFeatureCollection GeoJson { get; init; }
    public StormMetadata? Metadata { get; init; }
}

public record GeoJsonFeatureCollection
{
    public string Type { get; init; } = "FeatureCollection";
    public GeoJsonFeature[] Features { get; init; } = Array.Empty<GeoJsonFeature>();
}

public record GeoJsonFeature
{
    public string Type { get; init; } = "Feature";
    public required object Geometry { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
}

public record StormMetadata
{
    public string? EventId { get; init; }
    public double MaxHailSizeInches { get; init; }
    public double MaxWindSpeedMph { get; init; }
    public TimeSpan Duration { get; init; }
}

public record HailStatsRequest
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime Date { get; init; }
}

public record HailStatsResponse
{
    public double MaxSizeInches { get; init; }
    public int DurationMinutes { get; init; }
    public double HailProbability { get; init; }
    public string EventId { get; init; } = string.Empty;
}

public record ServiceAreaIntersectionRequest
{
    public required GeoJsonFeatureCollection ServiceAreaGeoJson { get; init; }
    public required GeoJsonFeatureCollection SwathGeoJson { get; init; }
}

public record ServiceAreaIntersectionResponse
{
    public HotZone[] HotZones { get; init; } = Array.Empty<HotZone>();
    public PropertyAddress[] Addresses { get; init; } = Array.Empty<PropertyAddress>();
}

public record HotZone
{
    public required string ZoneId { get; init; }
    public required GeoJsonFeature Geometry { get; init; }
    public double Priority { get; init; }
    public int EstimatedProperties { get; init; }
}

public record PropertyAddress
{
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double DamageRisk { get; init; }
    public string ZoneId { get; init; } = string.Empty;
}

public record EventSummaryResponse
{
    public required string EventId { get; init; }
    public required string MarkdownSummary { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string[] AffectedAreas { get; init; } = Array.Empty<string>();
}
