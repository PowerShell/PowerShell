using Microsoft.AspNetCore.Mvc;
using NelrockContracting.Services.Models;
using NelrockContracting.Services.Services;

namespace NelrockContracting.Services.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StormIntelController : ControllerBase
{
    private readonly IStormIntelligenceService _stormService;

    public StormIntelController(IStormIntelligenceService stormService)
    {
        _stormService = stormService;
    }

    [HttpPost("fetch-storm-swath")]
    public async Task<ActionResult<StormSwathResponse>> FetchStormSwath([FromBody] StormSwathRequest request)
    {
        try
        {
            var result = await _stormService.FetchStormSwathAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("hail-stats")]
    public async Task<ActionResult<HailStatsResponse>> GetHailStats([FromBody] HailStatsRequest request)
    {
        try
        {
            var result = await _stormService.GetHailStatsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("intersect-service-area")]
    public async Task<ActionResult<ServiceAreaIntersectionResponse>> IntersectServiceArea([FromBody] ServiceAreaIntersectionRequest request)
    {
        try
        {
            var result = await _stormService.IntersectServiceAreaAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("event-summary/{eventId}")]
    public async Task<ActionResult<EventSummaryResponse>> GetEventSummary(string eventId)
    {
        try
        {
            var result = await _stormService.GetEventSummaryAsync(eventId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
