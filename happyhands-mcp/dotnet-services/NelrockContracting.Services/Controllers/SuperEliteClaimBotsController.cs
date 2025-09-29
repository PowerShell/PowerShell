using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NelrockContracting.Services.Models;
using NelrockContracting.Services.Services;

namespace NelrockContracting.Services.Controllers;

[ApiController]
[Route("api/v1/super-elite")]
[Authorize]
public class SuperEliteClaimBotsController : ControllerBase
{
    private readonly ISuperEliteService _superEliteService;
    private readonly ILogger<SuperEliteClaimBotsController> _logger;

    public SuperEliteClaimBotsController(
        ISuperEliteService superEliteService,
        ILogger<SuperEliteClaimBotsController> logger)
    {
        _superEliteService = superEliteService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint for monitoring service status
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<HealthCheckResponse>> GetHealthAsync()
    {
        try
        {
            var health = await _superEliteService.GetHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { error = "Health check failed" });
        }
    }

    /// <summary>
    /// Analyze estimates for discrepancies and generate reports
    /// </summary>
    [HttpPost("analyze-estimate")]
    public async Task<ActionResult<EstimateAnalysisResponse>> AnalyzeEstimateAsync([FromForm] EstimateAnalysisRequest request)
    {
        try
        {
            var result = await _superEliteService.AnalyzeEstimateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Estimate analysis failed for claim {ClaimNumber}", request.ClaimNumber);
            return StatusCode(500, new ApiError { Message = "Estimate analysis failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Generate blueprint supplements with AI-powered damage mapping
    /// </summary>
    [HttpPost("supplement-blueprint")]
    public async Task<ActionResult<BlueprintSupplementResponse>> SupplementBlueprintAsync([FromForm] BlueprintSupplementRequest request)
    {
        try
        {
            var result = await _superEliteService.SupplementBlueprintAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blueprint supplement failed");
            return StatusCode(500, new ApiError { Message = "Blueprint supplement failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Generate legal documents and support materials
    /// </summary>
    [HttpPost("legal-support")]
    public async Task<ActionResult<LegalSupportResponse>> GenerateLegalSupportAsync([FromBody] LegalSupportRequest request)
    {
        try
        {
            var result = await _superEliteService.GenerateLegalSupportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Legal support generation failed for claim {ClaimNumber}", request.ClaimNumber);
            return StatusCode(500, new ApiError { Message = "Legal support generation failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Detect fraud patterns and risk factors
    /// </summary>
    [HttpPost("fraud-detection")]
    public async Task<ActionResult<FraudDetectionResponse>> DetectFraudAsync([FromBody] FraudDetectionRequest request)
    {
        try
        {
            var result = await _superEliteService.DetectFraudAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fraud detection failed for claim {ClaimNumber}", request.ClaimNumber);
            return StatusCode(500, new ApiError { Message = "Fraud detection failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get comprehensive market analysis and pricing data
    /// </summary>
    [HttpGet("market-analysis")]
    public async Task<ActionResult<MarketAnalysisResponse>> GetMarketAnalysisAsync(
        [FromQuery] string zipCode,
        [FromQuery] string? tradeType = null,
        [FromQuery] DateTime? effectiveDate = null,
        [FromQuery] string[]? materialTypes = null)
    {
        try
        {
            var request = new MarketAnalysisRequest
            {
                ZipCode = zipCode,
                TradeType = tradeType ?? "general",
                EffectiveDate = effectiveDate ?? DateTime.UtcNow,
                MaterialTypes = materialTypes?.ToList() ?? new List<string>()
            };

            var result = await _superEliteService.GetMarketAnalysisAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market analysis failed for zip code {ZipCode}", zipCode);
            return StatusCode(500, new ApiError { Message = "Market analysis failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Check building codes and compliance requirements
    /// </summary>
    [HttpPost("compliance-check")]
    public async Task<ActionResult<ComplianceCheckResponse>> CheckComplianceAsync([FromBody] ComplianceCheckRequest request)
    {
        try
        {
            var result = await _superEliteService.CheckComplianceAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compliance check failed");
            return StatusCode(500, new ApiError { Message = "Compliance check failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Analyze photos using computer vision AI
    /// </summary>
    [HttpPost("analyze-photos")]
    public async Task<ActionResult<PhotoAnalysisResponse>> AnalyzePhotosAsync([FromForm] PhotoAnalysisRequest request)
    {
        try
        {
            var result = await _superEliteService.AnalyzePhotosAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo analysis failed for claim {ClaimNumber}", request.ClaimNumber);
            return StatusCode(500, new ApiError { Message = "Photo analysis failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Generate comprehensive audit reports
    /// </summary>
    [HttpPost("generate-audit")]
    public async Task<ActionResult<AuditReportResponse>> GenerateAuditAsync([FromBody] AuditReportRequest request)
    {
        try
        {
            var result = await _superEliteService.GenerateAuditAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit generation failed for claim {ClaimNumber}", request.ClaimNumber);
            return StatusCode(500, new ApiError { Message = "Audit generation failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get real-time claim status and timeline
    /// </summary>
    [HttpGet("claim-status/{claimNumber}")]
    public async Task<ActionResult<ClaimStatusResponse>> GetClaimStatusAsync(string claimNumber)
    {
        try
        {
            var result = await _superEliteService.GetClaimStatusAsync(claimNumber);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claim status lookup failed for claim {ClaimNumber}", claimNumber);
            return StatusCode(500, new ApiError { Message = "Claim status lookup failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Register webhook for real-time notifications
    /// </summary>
    [HttpPost("webhooks")]
    public async Task<ActionResult<WebhookResponse>> RegisterWebhookAsync([FromBody] WebhookRequest request)
    {
        try
        {
            var result = await _superEliteService.RegisterWebhookAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook registration failed for URL {Url}", request.Url);
            return StatusCode(500, new ApiError { Message = "Webhook registration failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get business intelligence analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsResponse>> GetAnalyticsAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string[]? metrics = null,
        [FromQuery] string? groupBy = null)
    {
        try
        {
            var request = new AnalyticsRequest
            {
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow,
                Metrics = metrics ?? new[] { "claims", "estimates", "settlements" },
                GroupBy = groupBy ?? "day"
            };

            var result = await _superEliteService.GetAnalyticsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analytics request failed");
            return StatusCode(500, new ApiError { Message = "Analytics request failed", Details = ex.Message });
        }
    }
}