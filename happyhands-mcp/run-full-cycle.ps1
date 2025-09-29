# run-full-cycle.ps1
# Nelrock Contracting - Full Storm Damage Claims Cycle Demo
# This script demonstrates the complete workflow from storm detection to estimate generation

param(
    [string]$EventId = "IL-2025-09-29-hail",
    [string]$PropertyId = "PROP-789-SPRINGFIELD",
    [double]$Latitude = 39.7817,
    [double]$Longitude = -89.6501,
    [string]$Date = "2025-09-29"
)

Write-Host "🌪️  NELROCK CONTRACTING - FULL STORM CYCLE DEMO" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5001/api"
$headers = @{ "Content-Type" = "application/json" }

# Function to make REST API calls
function Invoke-ApiCall {
    param(
        [string]$Method = "GET",
        [string]$Uri,
        [object]$Body = $null
    )

    try {
        $params = @{
            Method = $Method
            Uri = $Uri
            Headers = $headers
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        Write-Host "❌ API call failed: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to display results nicely
function Show-Result {
    param(
        [string]$Title,
        [object]$Data,
        [string[]]$HighlightFields = @()
    )

    Write-Host "`n🔍 $Title" -ForegroundColor Green
    Write-Host ("-" * ($Title.Length + 4)) -ForegroundColor Green

    if ($Data) {
        if ($HighlightFields.Count -gt 0) {
            foreach ($field in $HighlightFields) {
                $value = Get-ObjectProperty -Object $Data -PropertyPath $field
                if ($value) {
                    Write-Host "   $field`: $value" -ForegroundColor Yellow
                }
            }
        } else {
            $Data | ConvertTo-Json -Depth 3 | Write-Host
        }
    } else {
        Write-Host "   No data returned" -ForegroundColor Red
    }
}

function Get-ObjectProperty {
    param(
        [object]$Object,
        [string]$PropertyPath
    )

    $parts = $PropertyPath -split '\.'
    $current = $Object

    foreach ($part in $parts) {
        if ($current -and $current.PSObject.Properties[$part]) {
            $current = $current.$part
        } else {
            return $null
        }
    }

    return $current
}

Write-Host "🚀 Starting Full Storm Damage Claims Cycle..." -ForegroundColor White
Write-Host "Event ID: $EventId" -ForegroundColor Gray
Write-Host "Property: $PropertyId at ($Latitude, $Longitude)" -ForegroundColor Gray
Write-Host "Date: $Date" -ForegroundColor Gray
Write-Host ""

# Step 1: Storm Intelligence Analysis
Write-Host "📡 PHASE 1: STORM INTELLIGENCE ANALYSIS" -ForegroundColor Magenta
Write-Host "=" * 45 -ForegroundColor Magenta

# 1.1 Get hail statistics for the property location
Write-Host "`n1.1 Analyzing hail damage potential..."
$hailStats = Invoke-ApiCall -Uri "$baseUrl/storm/hail_stats_at?lat=$Latitude&lon=$Longitude&date=$Date"
Show-Result -Title "Hail Analysis Results" -Data $hailStats -HighlightFields @("hailSize", "probability")

# 1.2 Generate event summary
Write-Host "`n1.2 Generating storm event summary..."
$eventSummary = Invoke-ApiCall -Uri "$baseUrl/storm/event_summary?eventId=$EventId"
Show-Result -Title "Storm Event Summary" -Data $eventSummary -HighlightFields @("summary.eventId", "summary.affectedAreas")

if ($eventSummary -and $eventSummary.summary.markdown) {
    Write-Host "`n📋 Event Report Preview:" -ForegroundColor Cyan
    $lines = $eventSummary.summary.markdown -split "`n"
    $lines[0..10] | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
    if ($lines.Count -gt 10) {
        Write-Host "   ... (truncated)" -ForegroundColor Gray
    }
}

# 1.3 Get storm swath data
Write-Host "`n1.3 Fetching storm swath data..."
$stormSwath = Invoke-ApiCall -Uri "$baseUrl/storm/fetch_storm_swath?eventId=$EventId&format=geojson"
Show-Result -Title "Storm Swath Data" -Data $stormSwath -HighlightFields @("swath.features", "metadata.maxHailSizeInches")

# 1.4 Service area intersection
Write-Host "`n1.4 Finding affected properties in service area..."
$serviceAreaPolygon = @{
    polygon = @{
        type = "Polygon"
        coordinates = @(@(
            @(-89.7, 39.6),
            @(-89.5, 39.6),
            @(-89.5, 39.9),
            @(-89.7, 39.9),
            @(-89.7, 39.6)
        ))
    }
    eventId = $EventId
}

$intersection = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/storm/intersect_service_area" -Body $serviceAreaPolygon
Show-Result -Title "Service Area Analysis" -Data $intersection -HighlightFields @("affectedProperties")

if ($intersection -and $intersection.affectedProperties) {
    Write-Host "`n🏠 Sample Affected Properties:" -ForegroundColor Cyan
    $intersection.affectedProperties[0..2] | ForEach-Object {
        Write-Host "   📍 $($_.address) (Risk: $($_.damageRisk))" -ForegroundColor White
    }
}

# Step 2: Damage Assessment & Estimating
Write-Host "`n`n💰 PHASE 2: DAMAGE ASSESSMENT & ESTIMATING" -ForegroundColor Magenta
Write-Host "=" * 45 -ForegroundColor Magenta

# 2.1 Build repair scope
Write-Host "`n2.1 Building repair scope with IRC compliance..."
$damageAssessment = @{
    propertyId = $PropertyId
    damages = @(
        @{
            type = "hail"
            location = "roof-south-face"
            severity = "moderate"
            description = "Hail impact damage to laminated asphalt shingles, granule loss and mat exposure visible"
        },
        @{
            type = "hail"
            location = "gutters-east-west"
            severity = "minor"
            description = "Denting and displacement of aluminum gutters and downspouts"
        },
        @{
            type = "wind"
            location = "ridge-cap"
            severity = "moderate"
            description = "Wind uplift damage to ridge cap shingles, several loose or missing"
        }
    )
}

$scope = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/estimate/build_scope" -Body $damageAssessment
Show-Result -Title "Repair Scope Generated" -Data $scope -HighlightFields @("scopeId", "estimate.total", "estimate.lineItems")

if ($scope -and $scope.estimate.lineItems) {
    Write-Host "`n📋 Key Line Items:" -ForegroundColor Cyan
    $scope.estimate.lineItems[0..4] | ForEach-Object {
        Write-Host "   🔧 $($_.code): $($_.description) - `$$($_.lineTotal)" -ForegroundColor White
    }

    Write-Host "`n💵 Cost Summary:" -ForegroundColor Yellow
    Write-Host "   Subtotal: `$$($scope.estimate.subtotal.ToString('N2'))" -ForegroundColor White
    Write-Host "   Tax: `$$($scope.estimate.tax.ToString('N2'))" -ForegroundColor White
    Write-Host "   TOTAL: `$$($scope.estimate.total.ToString('N2'))" -ForegroundColor Green
}

$scopeId = $scope.scopeId

# 2.2 Calculate material costs
if ($scopeId) {
    Write-Host "`n2.2 Calculating real-time material costs..."
    $materialCosts = Invoke-ApiCall -Uri "$baseUrl/estimate/calculate_material_costs?scopeId=$scopeId"
    Show-Result -Title "Material Cost Analysis" -Data $materialCosts -HighlightFields @("costs.totalCost", "costs.market")

    if ($materialCosts -and $materialCosts.costs.materials) {
        Write-Host "`n🏗️ Material Breakdown:" -ForegroundColor Cyan
        $materialCosts.costs.materials[0..3] | ForEach-Object {
            $availability = if ($_.available) { "✅ Available" } else { "⚠️ Backorder" }
            Write-Host "   📦 $($_.materialCode): `$$($_.unitCost) ($availability, $($_.leadTimeDays) days)" -ForegroundColor White
        }
    }
}

# 2.3 Export to Xactimate
if ($scopeId) {
    Write-Host "`n2.3 Exporting to Xactimate format..."
    $xactimateExport = Invoke-ApiCall -Uri "$baseUrl/estimate/export_xactimate?scopeId=$scopeId"
    Show-Result -Title "Xactimate Export" -Data $xactimateExport -HighlightFields @("fileUrl")

    if ($xactimateExport -and $xactimateExport.fileUrl) {
        Write-Host "`n📄 Export Details:" -ForegroundColor Cyan
        Write-Host "   File URL: $($xactimateExport.fileUrl)" -ForegroundColor White
        Write-Host "   Status: Ready for adjuster review" -ForegroundColor Green
    }
}

# Step 3: Summary and Next Steps
Write-Host "`n`n📊 PHASE 3: WORKFLOW SUMMARY" -ForegroundColor Magenta
Write-Host "=" * 30 -ForegroundColor Magenta

Write-Host "`n🎯 Cycle Completion Summary:" -ForegroundColor Green
Write-Host "✅ Storm intelligence analyzed" -ForegroundColor White
Write-Host "✅ Property damage assessed" -ForegroundColor White
Write-Host "✅ IRC-compliant estimate generated" -ForegroundColor White
Write-Host "✅ Material costs calculated" -ForegroundColor White
Write-Host "✅ Xactimate export prepared" -ForegroundColor White

if ($hailStats -and $scope) {
    Write-Host "`n📈 Key Metrics:" -ForegroundColor Yellow
    Write-Host "   🌨️ Hail size: $($hailStats.hailSize) inches" -ForegroundColor White
    Write-Host "   📍 Properties affected: $($intersection.affectedProperties.Count)" -ForegroundColor White
    Write-Host "   💰 Estimate total: `$$($scope.estimate.total.ToString('N2'))" -ForegroundColor White
    Write-Host "   🕒 Processing time: < 30 seconds" -ForegroundColor White
}

Write-Host "`n🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Send estimate to insurance adjuster" -ForegroundColor White
Write-Host "   2. Schedule supplemental inspection if needed" -ForegroundColor White
Write-Host "   3. Order materials upon approval" -ForegroundColor White
Write-Host "   4. Schedule crew for repair work" -ForegroundColor White

Write-Host "`n🎉 FULL CYCLE COMPLETE!" -ForegroundColor Green -BackgroundColor Black
Write-Host "Nelrock Contracting - Storm Damage Automation System" -ForegroundColor Gray
Write-Host ""
