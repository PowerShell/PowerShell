using NelrockContracting.Services.Models;

namespace NelrockContracting.Services.Services;

public interface IEstimatingService
{
    Task<ScopeResponse> BuildScopeAsync(ScopeRequest request);
    Task<XactimateExportResponse> ExportXactimateAsync(XactimateExportRequest request);
    Task<AdjusterScopeImportResponse> ImportAdjusterScopeAsync(IFormFile file, string caseId);
    Task<MaterialCostResponse> CalculateMaterialCostsAsync(MaterialCostRequest request);
}

public class EstimatingService : IEstimatingService
{
    private readonly ILogger<EstimatingService> _logger;

    public EstimatingService(ILogger<EstimatingService> logger)
    {
        _logger = logger;
    }

    public async Task<ScopeResponse> BuildScopeAsync(ScopeRequest request)
    {
        _logger.LogInformation("Building scope for case {CaseId}", request.CaseId);
        
        await Task.Delay(300); // Simulate complex calculations
        
        var lineItems = new List<LineItem>();
        
        // Calculate roofing materials based on measurements
        foreach (var facet in request.Measurements.Facets)
        {
            var squares = Math.Ceiling(facet.SquareFootage / 100.0);
            
            // Main roofing material
            lineItems.Add(new LineItem
            {
                Code = "R-SHG-LAM30",
                Description = $"Remove and replace laminated asphalt shingles, 30-year, {facet.Material}",
                Quantity = squares,
                Unit = "SQ",
                UnitPrice = 450.00m,
                LineTotal = (decimal)squares * 450.00m,
                Category = "Roofing",
                Notes = $"Facet {facet.FacetId}, {facet.Pitch}/12 pitch, {facet.Layers} layer(s) removal"
            });
            
            // Underlayment
            lineItems.Add(new LineItem
            {
                Code = "R-UND-SYNTH",
                Description = "Synthetic underlayment, high-temp",
                Quantity = squares * 1.1, // 10% waste factor
                Unit = "SQ",
                UnitPrice = 85.00m,
                LineTotal = (decimal)(squares * 1.1) * 85.00m,
                Category = "Roofing",
                Notes = "Code-compliant synthetic underlayment"
            });
        }
        
        // Add code-required items
        if (request.BuildingCodes.Contains("IRC2021"))
        {
            var linearFeet = CalculateLinearFeet(request.Measurements);
            lineItems.Add(new LineItem
            {
                Code = "R-DE-GALV",
                Description = "Galvanized drip edge, per IRC §R905.2.8.5",
                Quantity = (double)linearFeet,
                Unit = "LF",
                UnitPrice = 3.25m,
                LineTotal = linearFeet * 3.25m,
                Category = "Code Compliance",
                Notes = "Required by IRC 2021"
            });
        }
        
        // Add damage-specific items
        foreach (var damage in request.Damages)
        {
            if (damage.DamageType == "hail" && damage.Severity == "severe")
            {
                lineItems.Add(new LineItem
                {
                    Code = "R-DECK-OSB",
                    Description = "Replace damaged roof decking",
                    Quantity = 10, // Mock calculation
                    Unit = "SF",
                    UnitPrice = 2.85m,
                    LineTotal = 28.50m,
                    Category = "Storm Damage",
                    Notes = $"Hail damage at {damage.Location}"
                });
            }
        }
        
        var subtotal = lineItems.Sum(li => li.LineTotal);
        var tax = subtotal * 0.08m; // 8% tax
        var total = subtotal + tax;
        
        return new ScopeResponse
        {
            CaseId = request.CaseId,
            LineItems = lineItems.ToArray(),
            SubTotal = subtotal,
            Tax = tax,
            Total = total,
            Notes = "Scope based on IRC 2021, local building codes, and storm damage assessment. All work includes permits and inspections."
        };
    }

    public async Task<XactimateExportResponse> ExportXactimateAsync(XactimateExportRequest request)
    {
        _logger.LogInformation("Exporting to Xactimate for case {CaseId}", request.CaseId);
        
        await Task.Delay(500); // Simulate file generation
        
        // Mock Xactimate export
        var esxId = $"ESX-{request.CaseId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var pdfUrl = $"https://storage.nelrockcontracting.com/estimates/{esxId}.pdf";
        
        return new XactimateExportResponse
        {
            EsxId = esxId,
            PdfUrl = pdfUrl,
            Status = "completed",
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<AdjusterScopeImportResponse> ImportAdjusterScopeAsync(IFormFile file, string caseId)
    {
        _logger.LogInformation("Importing adjuster scope for case {CaseId}", caseId);
        
        await Task.Delay(200);
        
        // Mock PDF/Excel parsing
        var parsedItems = new[]
        {
            new LineItem
            {
                Code = "ADJ-001",
                Description = "Adjuster approved: Replace storm-damaged shingles",
                Quantity = 25,
                Unit = "SQ",
                UnitPrice = 420.00m,
                LineTotal = 10500.00m,
                Category = "Approved",
                Notes = "Adjuster: John Smith, State Farm"
            }
        };
        
        return new AdjusterScopeImportResponse
        {
            CaseId = caseId,
            ParsedItems = parsedItems,
            Warnings = Array.Empty<string>(),
            Success = true
        };
    }

    public async Task<MaterialCostResponse> CalculateMaterialCostsAsync(MaterialCostRequest request)
    {
        _logger.LogInformation("Calculating material costs for market {Market}", request.Market);
        
        await Task.Delay(150);
        
        var costs = new List<MaterialCost>();
        var random = new Random();
        
        foreach (var material in request.Materials)
        {
            var baseCost = GetBaseCost(material.MaterialCode);
            var marketMultiplier = GetMarketMultiplier(request.Market);
            var unitCost = baseCost * marketMultiplier;
            
            costs.Add(new MaterialCost
            {
                MaterialCode = material.MaterialCode,
                UnitCost = Math.Round(unitCost, 2),
                ExtendedCost = Math.Round(unitCost * (decimal)material.Quantity, 2),
                Supplier = GetRandomSupplier(),
                Available = random.NextDouble() > 0.1, // 90% availability
                LeadTimeDays = random.Next(1, 14)
            });
        }
        
        return new MaterialCostResponse
        {
            Market = request.Market,
            Costs = costs.ToArray(),
            TotalCost = costs.Sum(c => c.ExtendedCost),
            PricingDate = request.PricingDate,
            Source = "Regional Material Database"
        };
    }

    private decimal CalculateLinearFeet(RoofMeasurements measurements)
    {
        // Simplified perimeter calculation
        return (decimal)(Math.Sqrt(measurements.TotalSquareFootage) * 4 * 1.2); // Rough estimate with complexity factor
    }

    private decimal GetBaseCost(string materialCode)
    {
        return materialCode switch
        {
            "R-SHG-LAM30" => 125.00m,
            "R-UND-SYNTH" => 75.00m,
            "R-DE-GALV" => 2.50m,
            "R-DECK-OSB" => 2.25m,
            _ => 50.00m
        };
    }

    private decimal GetMarketMultiplier(string market)
    {
        // Mock market pricing variations
        return market switch
        {
            var m when m.Contains("Chicago") => 1.25m,
            var m when m.Contains("Springfield") => 1.05m,
            var m when m.Contains("Peoria") => 1.08m,
            _ => 1.00m
        };
    }

    private string GetRandomSupplier()
    {
        var suppliers = new[] { "ABC Supply", "Home Depot Pro", "Lowe's Pro", "84 Lumber", "Menards" };
        return suppliers[new Random().Next(suppliers.Length)];
    }
}
