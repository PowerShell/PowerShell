using NelrockContracting.Services.Models;

namespace NelrockContracting.Services.Services
{
    public class MockAIAnalysisService : IAIAnalysisService
    {
        private readonly ILogger<MockAIAnalysisService> _logger;

        public MockAIAnalysisService(ILogger<MockAIAnalysisService> logger)
        {
            _logger = logger;
        }

        public async Task<AIEstimateAnalysis> AnalyzeEstimateAsync(EstimateAnalysisData data)
        {
            _logger.LogInformation("Mock AI: Analyzing estimate with carrier total simulation");

            await Task.Delay(1500); // Simulate AI processing time

            return new AIEstimateAnalysis
            {
                CarrierTotal = 45000m,
                FraudScore = 0.15m,
                FraudRiskLevel = "Low",
                FraudFactors = new List<string> { "Standard pricing patterns", "Consistent with market rates" },
                FraudConfidence = 0.92m,
                OverallConfidence = 0.88m,
                Discrepancies = new List<EstimateDiscrepancy>
                {
                    new()
                    {
                        LineItem = "Roof Replacement - Architectural Shingles",
                        CarrierAmount = 12000m,
                        MarketAmount = 15000m,
                        DiscrepancyAmount = 3000m,
                        DiscrepancyPercentage = 25m,
                        DiscrepancyType = "Under-valued",
                        Justification = "Carrier estimate below current market rates for premium materials"
                    },
                    new()
                    {
                        LineItem = "Gutters - 6\" Seamless Aluminum",
                        CarrierAmount = 1800m,
                        MarketAmount = 2200m,
                        DiscrepancyAmount = 400m,
                        DiscrepancyPercentage = 22m,
                        DiscrepancyType = "Under-valued",
                        Justification = "Regional labor costs not properly accounted for"
                    }
                },
                Scope = new List<ScopeItem>
                {
                    new() { Category = "Roofing", Description = "Replace damaged shingles", Quantity = 35, Unit = "SQ", UnitPrice = 450m, Total = 15750m },
                    new() { Category = "Gutters", Description = "Replace storm-damaged gutters", Quantity = 120, Unit = "LF", UnitPrice = 18m, Total = 2160m }
                }
            };
        }

        public async Task<AIBlueprintAnalysis> AnalyzeBlueprintAsync(BlueprintAnalysisData data)
        {
            _logger.LogInformation("Mock AI: Analyzing blueprint for damage mapping");

            await Task.Delay(2000); // Simulate AI processing time

            return new AIBlueprintAnalysis
            {
                OverallConfidence = 0.91m,
                DamageMapping = new List<DamageMapping>
                {
                    new()
                    {
                        Location = "Southwest corner - Roof section A",
                        DamageType = "Hail Impact",
                        Severity = "Moderate",
                        Coordinates = new List<Coordinate>
                        {
                            new() { X = 120.5m, Y = 85.2m },
                            new() { X = 145.3m, Y = 95.7m }
                        },
                        PhotoReferences = new List<string> { "photo_001.jpg", "photo_004.jpg" }
                    }
                },
                VisualComparisons = new List<VisualComparison>
                {
                    new()
                    {
                        ComparisonType = "Before/After Overlay",
                        BeforeImage = "blueprint_original.pdf",
                        AfterImage = "damage_mapped.pdf",
                        OverlayImage = "comparison_overlay.pdf",
                        Analysis = "Clear hail damage pattern visible on south-facing roof sections"
                    }
                }
            };
        }

        public async Task<AIFraudAnalysis> AnalyzeFraudRiskAsync(FraudDetectionRequest request)
        {
            _logger.LogInformation("Mock AI: Analyzing fraud risk for claim {ClaimNumber}", request.ClaimNumber);

            await Task.Delay(1200); // Simulate AI processing time

            return new AIFraudAnalysis
            {
                RiskLevel = "Low",
                RiskScore = 0.18m,
                Confidence = 0.87m,
                RiskFactors = new List<RiskFactor>
                {
                    new() { Factor = "Geographic consistency", Description = "Estimate pricing consistent with regional norms", Weight = 0.3m, Impact = -0.1m },
                    new() { Factor = "Contractor reputation", Description = "Established contractor with clean history", Weight = 0.2m, Impact = -0.05m }
                },
                PatternAnalysis = new PatternAnalysis
                {
                    IdentifiedPatterns = new List<string> { "Standard storm damage claim", "Regional pricing alignment" },
                    PatternConfidence = 0.89m,
                    Analysis = "No unusual patterns detected. Claim follows typical storm damage profile."
                },
                GeographicAnomalies = new List<GeographicAnomaly>(),
                VendorRelationships = new VendorRelationshipAnalysis
                {
                    RelatedVendors = new List<string> { "ABC Roofing Supply", "Regional Materials Inc" },
                    RelationshipStrength = 0.3m,
                    RiskAssessment = "Standard vendor relationships, no red flags"
                },
                HistoricalPatterns = new HistoricalPatternAnalysis
                {
                    Patterns = new List<string> { "Seasonal storm claims", "Consistent contractor network" },
                    Frequency = 0.25m,
                    TrendAnalysis = "Normal claim frequency for geographic area and season"
                }
            };
        }

        public async Task<AIPhotoAnalysis> AnalyzePhotosAsync(List<SuperEliteFileData> photos, string analysisType)
        {
            _logger.LogInformation("Mock AI: Analyzing {PhotoCount} photos for {AnalysisType}", photos.Count, analysisType);

            await Task.Delay(800 * photos.Count); // Simulate processing time per photo

            var photoAnalyses = photos.Select((photo, index) => new PhotoAnalysis
            {
                PhotoId = $"photo_{index + 1:D3}",
                FileName = photo.FileName,
                ConfidenceScore = 0.85m + (decimal)(new Random().NextDouble() * 0.1),
                DetectedObjects = new List<DetectedObject>
                {
                    new() { ObjectType = "Roof", Confidence = 0.95m, BoundingBox = new PhotoBoundingBox { X = 100, Y = 50, Width = 400, Height = 300 }, Description = "Asphalt shingle roof surface" },
                    new() { ObjectType = "Damage", Confidence = 0.78m, BoundingBox = new PhotoBoundingBox { X = 250, Y = 150, Width = 100, Height = 80 }, Description = "Visible impact marks" }
                },
                DamageIndicators = new List<DamageIndicator>
                {
                    new() { DamageType = "Hail Impact", Severity = "Moderate", Confidence = 0.82m, Location = new PhotoBoundingBox { X = 250, Y = 150, Width = 100, Height = 80 }, Description = "Multiple impact craters on shingle surface" }
                },
                QualityAssessment = new QualityAssessment
                {
                    Sharpness = 0.87m,
                    Brightness = 0.75m,
                    Contrast = 0.82m,
                    HasTimestamp = true,
                    HasMetadata = true
                }
            }).ToList();

            return new AIPhotoAnalysis
            {
                ProcessingTimeMs = 800 * photos.Count,
                OverallConfidence = 0.84m,
                PhotoAnalyses = photoAnalyses,
                DamageAssessment = new PhotoDamageAssessment
                {
                    OverallSeverity = "Moderate",
                    DamageTypes = new List<string> { "Hail Impact", "Wind Damage", "Granule Loss" },
                    EstimatedRepairCost = 18500m,
                    Priority = "High"
                },
                AnnotatedPhotos = photoAnalyses.Select(pa => new AnnotatedPhoto
                {
                    OriginalPhotoId = pa.PhotoId,
                    AnnotatedImageUrl = $"/api/photos/annotated/{pa.PhotoId}",
                    Summary = "Hail damage clearly visible with impact measurements",
                    Annotations = new List<Annotation>
                    {
                        new() { Type = "Damage", Label = "Hail Impact", Location = new PhotoBoundingBox { X = 250, Y = 150, Width = 100, Height = 80 }, Description = "1+ inch hail impacts" }
                    }
                }).ToList(),
                OverallAssessment = new OverallAssessment
                {
                    Summary = "Significant hail damage detected across multiple roof surfaces",
                    KeyFindings = new List<string> { "Multiple impact sites", "Consistent damage pattern", "Granule loss evident" },
                    Recommendations = new List<string> { "Full roof replacement recommended", "Immediate tarp coverage for weather protection" },
                    TotalEstimatedCost = 18500m
                }
            };
        }
    }

    public class MockDocumentGenerationService : IDocumentGenerationService
    {
        private readonly ILogger<MockDocumentGenerationService> _logger;

        public MockDocumentGenerationService(ILogger<MockDocumentGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<GeneratedDocument> GenerateAuditReportAsync(AIEstimateAnalysis analysis, MarketData marketData)
        {
            _logger.LogInformation("Mock Document: Generating audit report");
            await Task.Delay(500);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Audit_Report_Professional.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/audit-report",
                SizeBytes = 245760,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateLegalMemoAsync(AIEstimateAnalysis analysis, ComplianceResults compliance)
        {
            _logger.LogInformation("Mock Document: Generating legal memo");
            await Task.Delay(300);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Legal_Memo_Comprehensive.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/legal-memo",
                SizeBytes = 186420,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateSupplementAsync(AIEstimateAnalysis analysis)
        {
            _logger.LogInformation("Mock Document: Generating supplement suggestions");
            await Task.Delay(200);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Supplement_Suggestions_Detailed.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/supplement",
                SizeBytes = 128340,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateAnnotatedBlueprintAsync(SuperEliteFileData blueprint, List<DamageMapping> damageMapping)
        {
            _logger.LogInformation("Mock Document: Generating annotated blueprint");
            await Task.Delay(800);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Blueprint_Annotated_Professional.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/annotated-blueprint",
                SizeBytes = 892450,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GeneratePresentationPackageAsync(GeneratedDocument blueprint, AIBlueprintAnalysis analysis, List<SuperEliteFileData> photos)
        {
            _logger.LogInformation("Mock Document: Generating presentation package");
            await Task.Delay(1200);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Presentation_Package_Complete.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/presentation",
                SizeBytes = 1450600,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateDemandLetterAsync(LegalSupportRequest request)
        {
            _logger.LogInformation("Mock Document: Generating demand letter");
            await Task.Delay(400);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Demand_Letter_Professional.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/demand-letter",
                SizeBytes = 95240,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateAppraisalRequestAsync(LegalSupportRequest request)
        {
            _logger.LogInformation("Mock Document: Generating appraisal request");
            await Task.Delay(300);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Appraisal_Request_Formal.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/appraisal-request",
                SizeBytes = 78560,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateLitigationSupportAsync(LegalSupportRequest request)
        {
            _logger.LogInformation("Mock Document: Generating litigation support");
            await Task.Delay(600);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Litigation_Support_Package.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/litigation-support",
                SizeBytes = 324780,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateExpertWitnessReportAsync(LegalSupportRequest request)
        {
            _logger.LogInformation("Mock Document: Generating expert witness report");
            await Task.Delay(900);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Expert_Witness_Report_Comprehensive.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/expert-witness",
                SizeBytes = 567890,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateInvestigationPlanAsync(AIFraudAnalysis fraudAnalysis)
        {
            _logger.LogInformation("Mock Document: Generating investigation plan");
            await Task.Delay(350);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Investigation_Plan_Strategic.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/investigation-plan",
                SizeBytes = 156780,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<GeneratedDocument> GenerateComprehensiveAuditAsync(AuditReportRequest request)
        {
            _logger.LogInformation("Mock Document: Generating comprehensive audit");
            await Task.Delay(1000);

            return new GeneratedDocument
            {
                DocumentId = Guid.NewGuid().ToString(),
                FileName = "Comprehensive_Audit_Report.pdf",
                Format = "PDF",
                DownloadUrl = "/api/documents/download/comprehensive-audit",
                SizeBytes = 678900,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    public class MockMarketDataService : IMarketDataService
    {
        private readonly ILogger<MockMarketDataService> _logger;

        public MockMarketDataService(ILogger<MockMarketDataService> logger)
        {
            _logger = logger;
        }

        public async Task<MarketData> GetMarketRatesAsync(string zipCode, DateTime effectiveDate)
        {
            _logger.LogInformation("Mock Market: Getting rates for {ZipCode}", zipCode);
            await Task.Delay(300);

            return new MarketData
            {
                EstimatedTotal = 48500m,
                LastUpdated = DateTime.UtcNow,
                MaterialCosts = new List<MarketMaterialCost>
                {
                    new() { MaterialType = "Architectural Shingles", Unit = "SQ", Cost = 450m, MarketVariance = 0.15m },
                    new() { MaterialType = "Underlayment", Unit = "SQ", Cost = 85m, MarketVariance = 0.08m },
                    new() { MaterialType = "Flashing", Unit = "LF", Cost = 12m, MarketVariance = 0.12m }
                },
                LaborRates = new List<LaborRate>
                {
                    new() { TradeType = "Roofer", HourlyRate = 75m, MarketVariance = 0.10m, SkillLevel = "Journeyman" },
                    new() { TradeType = "Helper", HourlyRate = 45m, MarketVariance = 0.05m, SkillLevel = "Apprentice" }
                },
                EquipmentRates = new List<EquipmentRate>
                {
                    new() { EquipmentType = "Crane - 40 Ton", DailyRate = 850m, RentalPeriod = "Daily", MarketVariance = 0.12m },
                    new() { EquipmentType = "Dumpster - 30 Yard", DailyRate = 320m, RentalPeriod = "Weekly", MarketVariance = 0.08m }
                }
            };
        }

        public async Task<MarketData> GetComprehensiveMarketDataAsync(MarketAnalysisRequest request)
        {
            _logger.LogInformation("Mock Market: Getting comprehensive data for {ZipCode}", request.ZipCode);
            await Task.Delay(500);

            var baseData = await GetMarketRatesAsync(request.ZipCode, request.EffectiveDate);
            
            baseData.SeasonalVariations = new SeasonalVariations
            {
                CurrentMultiplier = 1.15m,
                Explanation = "Spring storm season increases demand and pricing",
                Factors = new List<SeasonalFactor>
                {
                    new() { Season = "Spring", Multiplier = 1.15m, Description = "High storm activity" },
                    new() { Season = "Summer", Multiplier = 1.10m, Description = "Peak construction season" },
                    new() { Season = "Fall", Multiplier = 0.95m, Description = "Slower demand period" },
                    new() { Season = "Winter", Multiplier = 0.85m, Description = "Weather delays" }
                }
            };

            baseData.SupplyChainFactors = new List<SupplyChainFactor>
            {
                new() { Factor = "Material Shortage", Impact = 0.12m, Description = "Regional supply constraints", EffectiveDate = DateTime.UtcNow.AddDays(-30) },
                new() { Factor = "Shipping Delays", Impact = 0.05m, Description = "Transportation bottlenecks", EffectiveDate = DateTime.UtcNow.AddDays(-15) }
            };

            baseData.TrendForecasting = new TrendForecasting
            {
                Confidence = 0.78m,
                Methodology = "Historical analysis with seasonal adjustment",
                Predictions = new List<TrendPrediction>
                {
                    new() { PredictionDate = DateTime.UtcNow.AddMonths(1), PredictedValue = 485m, Category = "Shingle Cost per SQ", Confidence = 0.85m },
                    new() { PredictionDate = DateTime.UtcNow.AddMonths(3), PredictedValue = 465m, Category = "Shingle Cost per SQ", Confidence = 0.72m }
                }
            };

            baseData.RegionalComparisons = new List<RegionalComparison>
            {
                new() { Region = "Metro Area", CostIndex = 1.08m, RelativeVariance = 0.08m, Description = "Urban premium pricing" },
                new() { Region = "Suburban", CostIndex = 1.00m, RelativeVariance = 0.00m, Description = "Baseline market rates" },
                new() { Region = "Rural", CostIndex = 0.87m, RelativeVariance = -0.13m, Description = "Lower demand, reduced costs" }
            };

            return baseData;
        }
    }

    public class MockComplianceService : IComplianceService
    {
        private readonly ILogger<MockComplianceService> _logger;

        public MockComplianceService(ILogger<MockComplianceService> logger)
        {
            _logger = logger;
        }

        public async Task<ComplianceResults> CheckBuildingCodesAsync(PropertyDetails property, List<ScopeItem> scope)
        {
            _logger.LogInformation("Mock Compliance: Checking building codes for {PropertyType}", property.PropertyType);
            await Task.Delay(400);

            return new ComplianceResults
            {
                OverallScore = 0.92m,
                Issues = new List<ComplianceIssue>
                {
                    new() { CodeSection = "IRC R905.2.4", Description = "Shingle overhang requirements", Severity = "Minor", Recommendation = "Ensure 3/4\" overhang at rake edges", CostImpact = 250m },
                    new() { CodeSection = "IRC R905.2.8", Description = "Ice barrier requirements", Severity = "Moderate", Recommendation = "Install ice barrier in climate zones 4-8", CostImpact = 850m }
                },
                BuildingCodes = new List<BuildingCodeCompliance>
                {
                    new() { CodeType = "IRC", Version = "2021", IsCompliant = true, Violations = new List<string>(), ComplianceScore = 0.95m },
                    new() { CodeType = "IBC", Version = "2021", IsCompliant = true, Violations = new List<string>(), ComplianceScore = 0.88m }
                },
                AccessibilityCompliance = new AccessibilityCompliance
                {
                    ADACompliant = true,
                    Violations = new List<string>(),
                    Recommendations = new List<string> { "Consider accessible pathway to roof access points" },
                    CostToComply = 0m
                },
                EnvironmentalCompliance = new EnvironmentalCompliance
                {
                    IsCompliant = true,
                    ApplicableRegulations = new List<string> { "EPA RRP Rule", "Local Noise Ordinances" },
                    RequiredPermits = new List<string> { "Building Permit", "Demolition Permit" },
                    Recommendations = new List<string> { "Follow lead-safe work practices", "Obtain noise variance if needed" }
                }
            };
        }

        public async Task<ComplianceVerification> VerifyBlueprintComplianceAsync(SuperEliteFileData blueprint)
        {
            _logger.LogInformation("Mock Compliance: Verifying blueprint compliance");
            await Task.Delay(600);

            return new ComplianceVerification
            {
                IsCompliant = true,
                ComplianceScore = 0.89m,
                ViolatedCodes = new List<string>(),
                Recommendations = new List<string> 
                { 
                    "Add structural engineer stamp for truss modifications",
                    "Verify local setback requirements"
                }
            };
        }

        public async Task<ComplianceResults> CheckComprehensiveComplianceAsync(ComplianceCheckRequest request)
        {
            _logger.LogInformation("Mock Compliance: Comprehensive compliance check");
            await Task.Delay(800);

            var baseResults = await CheckBuildingCodesAsync(request.Property, request.ScopeItems);
            
            baseResults.LocalOrdinances = new List<LocalOrdinance>
            {
                new() { OrdinanceNumber = "2023-15", Description = "Storm water management requirements", IsApplicable = true, ComplianceStatus = "Compliant" },
                new() { OrdinanceNumber = "2023-22", Description = "Historic district preservation", IsApplicable = false, ComplianceStatus = "N/A" }
            };

            baseResults.Violations = new List<ComplianceViolation>();
            baseResults.Recommendations = new List<ComplianceRecommendation>
            {
                new() { RecommendationType = "Code Update", Description = "Consider upgrading to 2024 IRC standards", EstimatedCost = 1200m, Priority = "Low" },
                new() { RecommendationType = "Safety Enhancement", Description = "Add additional ventilation as recommended", EstimatedCost = 450m, Priority = "Medium" }
            };

            return baseResults;
        }
    }

    public class MockWebhookService : IWebhookService
    {
        private readonly ILogger<MockWebhookService> _logger;
        private readonly Dictionary<string, WebhookRequest> _webhooks = new();

        public MockWebhookService(ILogger<MockWebhookService> logger)
        {
            _logger = logger;
        }

        public async Task<string> RegisterWebhookAsync(WebhookRequest request)
        {
            _logger.LogInformation("Mock Webhook: Registering webhook for {Url}", request.Url);
            await Task.Delay(100);

            var webhookId = Guid.NewGuid().ToString();
            _webhooks[webhookId] = request;
            
            return webhookId;
        }

        public async Task SendWebhookAsync(string webhookId, object data)
        {
            _logger.LogInformation("Mock Webhook: Sending webhook {WebhookId}", webhookId);
            await Task.Delay(50);

            if (_webhooks.TryGetValue(webhookId, out var webhook))
            {
                _logger.LogInformation("Mock: Would send webhook to {Url} with data", webhook.Url);
                // In real implementation, would make HTTP POST to webhook.Url
            }
        }
    }
}