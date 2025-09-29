namespace NelrockContracting.Services.Models;

// Estimating Models
public record ScopeRequest
{
    public required string CaseId { get; init; }
    public required RoofMeasurements Measurements { get; init; }
    public DamageAssessment[] Damages { get; init; } = Array.Empty<DamageAssessment>();
    public string[] BuildingCodes { get; init; } = Array.Empty<string>();
}

public record RoofMeasurements
{
    public RoofFacet[] Facets { get; init; } = Array.Empty<RoofFacet>();
    public double TotalSquareFootage { get; init; }
    public int Stories { get; init; }
    public string PrimaryMaterial { get; init; } = string.Empty;
    public string AccessType { get; init; } = string.Empty;
}

public record RoofFacet
{
    public required string FacetId { get; init; }
    public double SquareFootage { get; init; }
    public double Pitch { get; init; }
    public string Orientation { get; init; } = string.Empty; // N, S, E, W, etc.
    public int Layers { get; init; }
    public string Material { get; init; } = string.Empty;
}

public record DamageAssessment
{
    public required string DamageType { get; init; } // "hail", "wind", "impact", etc.
    public required string Location { get; init; }
    public string Severity { get; init; } = string.Empty; // "minor", "moderate", "severe"
    public string Description { get; init; } = string.Empty;
    public string[] PhotoIds { get; init; } = Array.Empty<string>();
}

public record ScopeResponse
{
    public required string CaseId { get; init; }
    public LineItem[] LineItems { get; init; } = Array.Empty<LineItem>();
    public decimal SubTotal { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public record LineItem
{
    public required string Code { get; init; }
    public required string Description { get; init; }
    public double Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

public record XactimateExportRequest
{
    public required string CaseId { get; init; }
    public LineItem[] LineItems { get; init; } = Array.Empty<LineItem>();
    public ExportOptions? Options { get; init; }
}

public record ExportOptions
{
    public bool IncludePhotos { get; init; } = true;
    public bool IncludeNotes { get; init; } = true;
    public string Template { get; init; } = "standard";
}

public record XactimateExportResponse
{
    public required string EsxId { get; init; }
    public required string PdfUrl { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdjusterScopeImportResponse
{
    public required string CaseId { get; init; }
    public LineItem[] ParsedItems { get; init; } = Array.Empty<LineItem>();
    public string[] Warnings { get; init; } = Array.Empty<string>();
    public bool Success { get; init; }
}

public record MaterialCostRequest
{
    public required string Market { get; init; } // zip code or city
    public MaterialSpec[] Materials { get; init; } = Array.Empty<MaterialSpec>();
    public DateTime PricingDate { get; init; }
}

public record MaterialSpec
{
    public required string MaterialCode { get; init; }
    public required string Description { get; init; }
    public double Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Grade { get; init; } = string.Empty;
}

public record MaterialCostResponse
{
    public required string Market { get; init; }
    public MaterialCost[] Costs { get; init; } = Array.Empty<MaterialCost>();
    public decimal TotalCost { get; init; }
    public DateTime PricingDate { get; init; }
    public string Source { get; init; } = string.Empty;
}

public record MaterialCost
{
    public required string MaterialCode { get; init; }
    public decimal UnitCost { get; init; }
    public decimal ExtendedCost { get; init; }
    public string Supplier { get; init; } = string.Empty;
    public bool Available { get; init; }
    public int LeadTimeDays { get; init; }
}
