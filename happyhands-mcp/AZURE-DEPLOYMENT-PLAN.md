# Nelrock Contracting Azure Deployment Plan

## Overview
Deploy the Super Elite Claim Bots API (.NET) and MCP server (Node.js) to Azure App Service Plan.

## Azure Resources
- **Subscription ID:** e708d7df-304c-4f85-bbca-de457a890087
- **Resource Group:** Nelrockwins1
- **App Service Plan:** ASP-Nelrockwins1-b5b6

## Services to Deploy

### 1. Super Elite Claim Bots API (.NET)
- **Location:** `/PowerShell/happyhands-mcp/dotnet-services/NelrockContracting.Services`
- **Type:** ASP.NET Core Web API
- **Target:** Azure Web App
- **Features:**
  - 12 comprehensive endpoints
  - JWT authentication
  - AI-powered analysis
  - Document generation
  - Market intelligence

### 2. MCP Server (Node.js)
- **Location:** `/PowerShell/happyhands-mcp`
- **Type:** Node.js MCP Server
- **Target:** Azure Web App
- **Features:**
  - Storm intelligence
  - Lead generation
  - Estimating tools
  - Xactimate integration

## Deployment Steps

### Prerequisites
1. Azure CLI authentication
2. Resource group verification
3. App Service Plan validation

### Web App Creation
```bash
# Create .NET API Web App
az webapp create \
  --resource-group Nelrockwins1 \
  --plan ASP-Nelrockwins1-b5b6 \
  --name nelrock-super-elite-api \
  --runtime "DOTNET|8.0"

# Create Node.js MCP Server Web App
az webapp create \
  --resource-group Nelrockwins1 \
  --plan ASP-Nelrockwins1-b5b6 \
  --name nelrock-mcp-server \
  --runtime "NODE|20-lts"
```

### Configuration
```bash
# Configure .NET API settings
az webapp config appsettings set \
  --resource-group Nelrockwins1 \
  --name nelrock-super-elite-api \
  --settings \
    "Jwt__Key=super-elite-claim-bots-production-key" \
    "Jwt__Issuer=SuperEliteClaimBots" \
    "Jwt__Audience=SuperEliteClaimBots" \
    "ASPNETCORE_ENVIRONMENT=Production"

# Configure Node.js MCP Server settings
az webapp config appsettings set \
  --resource-group Nelrockwins1 \
  --name nelrock-mcp-server \
  --settings \
    "NODE_ENV=production" \
    "MCP_SERVER_NAME=nelrock-contracting" \
    "MCP_SERVER_VERSION=2.0.0"
```

### Deployment Commands
```bash
# Deploy .NET API
cd /PowerShell/happyhands-mcp/dotnet-services/NelrockContracting.Services
dotnet publish -c Release -o ./publish
zip -r ../nelrock-api.zip ./publish/*
az webapp deploy \
  --resource-group Nelrockwins1 \
  --name nelrock-super-elite-api \
  --src-path ../nelrock-api.zip

# Deploy Node.js MCP Server
cd /PowerShell/happyhands-mcp
zip -r nelrock-mcp.zip . -x "dotnet-services/*" "test/*" "*.md"
az webapp deploy \
  --resource-group Nelrockwins1 \
  --name nelrock-mcp-server \
  --src-path nelrock-mcp.zip
```

## Expected URLs
- **Super Elite API:** https://nelrock-super-elite-api.azurewebsites.net
- **MCP Server:** https://nelrock-mcp-server.azurewebsites.net
- **Health Check:** https://nelrock-super-elite-api.azurewebsites.net/health
- **Swagger Docs:** https://nelrock-super-elite-api.azurewebsites.net/swagger

## Post-Deployment Testing
1. Health endpoint verification
2. API functionality testing
3. MCP server connectivity
4. End-to-end workflow validation

## Security Configuration
- JWT token validation
- HTTPS enforcement
- CORS policy setup
- Authentication middleware

## Monitoring Setup
- Application Insights
- Log Analytics
- Performance monitoring
- Error tracking

---
Generated: September 29, 2025
Environment: GitHub Codespaces → Azure App Service
