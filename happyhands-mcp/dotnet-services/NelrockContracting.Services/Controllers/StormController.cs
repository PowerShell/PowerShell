using Microsoft.AspNetCore.Mvc;
using NelrockContracting.Services.Models;
using NelrockContracting.Services.Services;

namespace NelrockContracting.Services.Controllers;

[ApiController]
[Route("api/storm")]
public class StormController : ControllerBase
{
    private readonly IStormIntelligenceService _stormService;
    private readonly ILogger<StormController> _logger;

    public StormController(IStormIntelligenceService stormService, ILogger<StormController> logger)
    {
        _stormService = stormService;
        _logger = logger;
    }

    [HttpGet("fetch_storm_swath")]
    public async Task<ActionResult<object>> FetchStormSwath([FromQuery] string eventId, [FromQuery] string? format = "json")
    {
        try
        {
            _logger.LogInformation("Fetching storm swath for event {EventId} in format {Format}", eventId, format);
            
            // Mock bbox for the event - in real implementation, this would come from event data
            var request = new StormSwathRequest
            {
                BBox = new BoundingBox { North = 40.0, South = 39.0, East = -88.0, West = -90.0 },
                StartUtc = DateTime.UtcNow.AddDays(-1),
                EndUtc = DateTime.UtcNow,
                Hazards = new[] { "hail", "wind" }
            };
            
            var result = await _stormService.FetchStormSwathAsync(request);
            
            return Ok(new
            {
                swath = result.GeoJson,
                metadata = result.Metadata
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching storm swath for event {EventId}", eventId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("hail_stats_at")]
    public async Task<ActionResult<object>> HailStatsAt([FromQuery] double lat, [FromQuery] double lon, [FromQuery] string date)
    {
        try
        {
            _logger.LogInformation("Getting hail stats at {Lat}, {Lon} for date {Date}", lat, lon, date);
            
            var request = new HailStatsRequest
            {
                Latitude = lat,
                Longitude = lon,
                Date = DateTime.Parse(date)
            };
            
            var result = await _stormService.GetHailStatsAsync(request);
            
            return Ok(new
            {
                hailSize = result.MaxSizeInches,
                probability = result.HailProbability
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hail stats");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("intersect_service_area")]
    public async Task<ActionResult<object>> IntersectServiceArea([FromBody] ServiceAreaRequest request)
    {
        try
        {
            _logger.LogInformation("Intersecting service area for event {EventId}", request.EventId);
            
            // Convert the polygon to our internal format
            var intersectionRequest = new ServiceAreaIntersectionRequest
            {
                ServiceAreaGeoJson = new GeoJsonFeatureCollection
                {
                    Features = new[]
                    {
                        new GeoJsonFeature
                        {
                            Geometry = request.Polygon,
                            Properties = new Dictionary<string, object> { ["eventId"] = request.EventId ?? "" }
                        }
                    }
                },
                SwathGeoJson = new GeoJsonFeatureCollection
                {
                    Features = new[]
                    {
                        new GeoJsonFeature
                        {
                            Geometry = request.Polygon,
                            Properties = new Dictionary<string, object>()
                        }
                    }
                }
            };
            
            var result = await _stormService.IntersectServiceAreaAsync(intersectionRequest);
            
            return Ok(new
            {
                affectedProperties = result.Addresses.Select(addr => new
                {
                    address = addr.Address,
                    latitude = addr.Latitude,
                    longitude = addr.Longitude,
                    damageRisk = addr.DamageRisk
                }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error intersecting service area");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("event_summary")]
    public async Task<ActionResult<object>> EventSummary([FromQuery] string eventId)
    {
        try
        {
            _logger.LogInformation("Generating event summary for {EventId}", eventId);
            
            var result = await _stormService.GetEventSummaryAsync(eventId);
            
            return Ok(new
            {
                summary = new
                {
                    eventId = result.EventId,
                    markdown = result.MarkdownSummary,
                    startTime = result.StartTime,
                    endTime = result.EndTime,
                    affectedAreas = result.AffectedAreas
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating event summary for {EventId}", eventId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request models for the new API format
public record ServiceAreaRequest
{
    public required object Polygon { get; init; }
    public string? EventId { get; init; }
}
