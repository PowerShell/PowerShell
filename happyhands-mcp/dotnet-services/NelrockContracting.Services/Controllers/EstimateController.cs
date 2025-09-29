using Microsoft.AspNetCore.Mvc;
using NelrockContracting.Services.Models;
using NelrockContracting.Services.Services;

namespace NelrockContracting.Services.Controllers;

[ApiController]
[Route("api/estimate")]
public class EstimateController : ControllerBase
{
    private readonly IEstimatingService _estimatingService;
    private readonly ILogger<EstimateController> _logger;

    public EstimateController(IEstimatingService estimatingService, ILogger<EstimateController> logger)
    {
        _estimatingService = estimatingService;
        _logger = logger;
    }

    [HttpPost("build_scope")]
    public async Task<ActionResult<object>> BuildScope([FromBody] BuildScopeApiRequest request)
    {
        try
        {
            _logger.LogInformation("Building scope for property {PropertyId}", request.PropertyId);
            
            // Convert API request to internal format
            var internalRequest = new ScopeRequest
            {
                CaseId = request.PropertyId,
                Measurements = new RoofMeasurements
                {
                    Facets = new[]
                    {
                        new RoofFacet
                        {
                            FacetId = "main",
                            SquareFootage = 2000, // Default - would come from measurements
                            Pitch = 6,
                            Orientation = "South",
                            Layers = 1,
                            Material = "asphalt"
                        }
                    },
                    TotalSquareFootage = 2000,
                    Stories = 1,
                    PrimaryMaterial = "asphalt",
                    AccessType = "ladder"
                },
                Damages = request.Damages.Select(d => new DamageAssessment
                {
                    DamageType = d.TryGetProperty("type", out var typeElement) ? typeElement.ToString() : "hail",
                    Location = d.TryGetProperty("location", out var locationElement) ? locationElement.ToString() : "roof",
                    Severity = d.TryGetProperty("severity", out var severityElement) ? severityElement.ToString() : "moderate",
                    Description = d.TryGetProperty("description", out var descElement) ? descElement.ToString() : ""
                }).ToArray(),
                BuildingCodes = new[] { "IRC2021" }
            };
            
            var result = await _estimatingService.BuildScopeAsync(internalRequest);
            
            return Ok(new
            {
                scopeId = result.CaseId,
                estimate = new
                {
                    lineItems = result.LineItems,
                    subtotal = result.SubTotal,
                    tax = result.Tax,
                    total = result.Total,
                    notes = result.Notes
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building scope for property {PropertyId}", request.PropertyId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("export_xactimate")]
    public async Task<ActionResult<object>> ExportXactimate([FromQuery] string scopeId)
    {
        try
        {
            _logger.LogInformation("Exporting scope {ScopeId} to Xactimate", scopeId);
            
            var request = new XactimateExportRequest
            {
                CaseId = scopeId,
                LineItems = Array.Empty<LineItem>(), // Would load from database
                Options = new ExportOptions
                {
                    IncludePhotos = true,
                    IncludeNotes = true,
                    Template = "standard"
                }
            };
            
            var result = await _estimatingService.ExportXactimateAsync(request);
            
            return Ok(new
            {
                fileUrl = result.PdfUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Xactimate for scope {ScopeId}", scopeId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("calculate_material_costs")]
    public async Task<ActionResult<object>> CalculateMaterialCosts([FromQuery] string scopeId)
    {
        try
        {
            _logger.LogInformation("Calculating material costs for scope {ScopeId}", scopeId);
            
            // Mock materials for the scope - would come from database
            var request = new MaterialCostRequest
            {
                Market = "Springfield, IL",
                Materials = new[]
                {
                    new MaterialSpec
                    {
                        MaterialCode = "R-SHG-LAM30",
                        Description = "Laminated asphalt shingles, 30-year",
                        Quantity = 25,
                        Unit = "SQ",
                        Brand = "Owens Corning",
                        Grade = "Duration"
                    },
                    new MaterialSpec
                    {
                        MaterialCode = "R-UND-SYNTH",
                        Description = "Synthetic underlayment",
                        Quantity = 27,
                        Unit = "SQ",
                        Brand = "GAF",
                        Grade = "TigerPaw"
                    }
                },
                PricingDate = DateTime.UtcNow
            };
            
            var result = await _estimatingService.CalculateMaterialCostsAsync(request);
            
            return Ok(new
            {
                costs = new
                {
                    materials = result.Costs,
                    totalCost = result.TotalCost,
                    market = result.Market,
                    pricingDate = result.PricingDate,
                    source = result.Source
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating material costs for scope {ScopeId}", scopeId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request models for the new API format
public record BuildScopeApiRequest
{
    public required string PropertyId { get; init; }
    public System.Text.Json.JsonElement[] Damages { get; init; } = Array.Empty<System.Text.Json.JsonElement>();
}
