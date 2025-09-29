using Microsoft.AspNetCore.Mvc;
using NelrockContracting.Services.Models;
using NelrockContracting.Services.Services;

namespace NelrockContracting.Services.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstimatingController : ControllerBase
{
    private readonly IEstimatingService _estimatingService;

    public EstimatingController(IEstimatingService estimatingService)
    {
        _estimatingService = estimatingService;
    }

    [HttpPost("build-scope")]
    public async Task<ActionResult<ScopeResponse>> BuildScope([FromBody] ScopeRequest request)
    {
        try
        {
            var result = await _estimatingService.BuildScopeAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("export-xactimate")]
    public async Task<ActionResult<XactimateExportResponse>> ExportXactimate([FromBody] XactimateExportRequest request)
    {
        try
        {
            var result = await _estimatingService.ExportXactimateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("import-adjuster-scope")]
    public async Task<ActionResult<AdjusterScopeImportResponse>> ImportAdjusterScope([FromForm] IFormFile file, [FromForm] string caseId)
    {
        try
        {
            var result = await _estimatingService.ImportAdjusterScopeAsync(file, caseId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("calculate-material-costs")]
    public async Task<ActionResult<MaterialCostResponse>> CalculateMaterialCosts([FromBody] MaterialCostRequest request)
    {
        try
        {
            var result = await _estimatingService.CalculateMaterialCostsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
