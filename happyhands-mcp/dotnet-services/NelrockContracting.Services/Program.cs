using NelrockContracting.Services.Services;
using NelrockContracting.Services.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Super Elite Claim Bots API", Version = "v2.0" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// Add Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super-elite-claim-bots-secret-key-development-only";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SuperEliteClaimBots";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SuperEliteClaimBots";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add custom services
builder.Services.AddScoped<IStormIntelligenceService, StormIntelligenceService>();
builder.Services.AddScoped<IEstimatingService, EstimatingService>();
builder.Services.AddScoped<IAzureIntegrationService, AzureIntegrationService>();

// Add Super Elite services
builder.Services.AddScoped<ISuperEliteService, SuperEliteService>();
builder.Services.AddScoped<IAIAnalysisService, MockAIAnalysisService>();
builder.Services.AddScoped<IDocumentGenerationService, MockDocumentGenerationService>();
builder.Services.AddScoped<IMarketDataService, MockMarketDataService>();
builder.Services.AddScoped<IComplianceService, MockComplianceService>();
builder.Services.AddScoped<IWebhookService, MockWebhookService>();

// Add CORS for Node.js MCP server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMcpServer", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Super Elite Claim Bots API v2.0");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowMcpServer");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
