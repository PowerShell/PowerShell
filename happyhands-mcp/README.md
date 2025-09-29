# Nelrock Contracting MCP Server

A hybrid Node.js/-.NET MCP server for storm damage restoration and contracting workflows.

## Architecture

- **Node.js MCP Server**: Handles MCP protocol communication and lightweight operations
- **.NET Core API**: Provides complex business logic, Azure integrations, and heavy computations
- **Hybrid Communication**: Node.js tools call .NET services via HTTP API

## Features

### Core MCP Tools (Node.js)
- **Messaging**: SMS/Email with scheduling and templates
- **Scheduling**: Calendar management with availability/booking
- **Data Storage**: JSON-based persistence with audit trails

### Advanced Services (.NET)
- **Storm Intelligence**: Weather data analysis, GIS intersections, damage assessment
- **Estimating**: Scope building, Xactimate export, material cost calculations
- **Azure Integration**: Blob storage, Communication Services, enterprise APIs

## Quick Start

### Prerequisites
- Node.js 18+
- .NET 8.0 SDK
- Docker (optional)

### Setup

1. **Install Node.js dependencies**:
   ```bash
   cd happyhands-mcp
   npm install
   ```

2. **Setup .NET service**:
   ```bash
   cd dotnet-services
   dotnet restore
   dotnet build
   ```

3. **Configure environment**:
   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

### Running

1. **Start .NET API** (Terminal 1):
   ```bash
   cd dotnet-services/NelrockContracting.Services
   dotnet run
   # Runs on http://localhost:5000
   ```

2. **Start MCP Server** (Terminal 2):
   ```bash
   cd happyhands-mcp
   npm start
   # MCP server ready on stdio
   ```

## MCP Tools Reference

### Messaging Tools
- `send_sms`: Send SMS with optional scheduling
- `send_email`: Send email with attachments
- `template_preview`: Preview message templates

### Scheduling Tools  
- `availability`: Get available time slots
- `book_slot`: Book an appointment
- `reschedule`: Change appointment time
- `cancel`: Cancel appointment

### Storm Intelligence Tools (.NET-powered)
- `fetch_storm_swath`: Get storm paths and intensity data
- `hail_stats_at`: Detailed hail analysis for location/date
- `intersect_service_area`: Find storm/service area overlaps
- `event_summary`: Generate markdown storm reports

### Estimating Tools (.NET-powered)
- `build_scope`: Create detailed repair estimates
- `export_xactimate`: Export to Xactimate format
- `calculate_material_costs`: Real-time material pricing

## Example Usage

```javascript
// Fetch storm data for a region
const stormData = await callTool('fetch_storm_swath', {
  bbox: { north: 40.0, south: 39.0, east: -88.0, west: -90.0 },
  start_utc: '2025-09-29T18:00:00Z',
  end_utc: '2025-09-29T23:00:00Z',
  hazards: ['hail', 'wind']
});

// Build repair estimate
const estimate = await callTool('build_scope', {
  case_id: 'NRC-2025-001',
  measurements: {
    facets: [{
      facetId: 'south-slope',
      squareFootage: 1200,
      pitch: 6,
      orientation: 'S',
      layers: 2,
      material: 'asphalt'
    }],
    totalSquareFootage: 2400,
    stories: 2,
    primaryMaterial: 'laminated asphalt'
  },
  damages: [{
    damageType: 'hail',
    location: 'south slope',
    severity: 'moderate',
    description: 'Granular loss and exposed mat'
  }],
  buildingCodes: ['IRC2021']
});

// Send customer notification
await callTool('send_sms', {
  to: '+15551234567',
  body: 'Your estimate is ready! Total: $12,450. Click here to review: https://nelrock.com/estimate/NRC-2025-001'
});
```

## Project Structure

```
happyhands-mcp/
├── package.json
├── .env.example
├── src/
│   ├── index.js              # Main MCP server
│   ├── mcp.js                # MCP protocol handler
│   ├── store.js              # JSON data storage
│   ├── tools/
│   │   ├── messaging.js      # SMS/Email tools
│   │   ├── scheduling.js     # Calendar tools
│   │   └── storm-intel.js    # .NET-powered tools
│   └── adapters/
│       ├── sms.mock.js       # SMS adapter (mock)
│       ├── email.mock.js     # Email adapter (mock)
│       ├── calendar.mock.js  # Calendar adapter (mock)
│       └── dotnet.adapter.js # .NET service adapter
└── dotnet-services/
    └── NelrockContracting.Services/
        ├── Controllers/      # API endpoints
        ├── Models/          # Data models
        ├── Services/        # Business logic
        └── Program.cs       # Service configuration
```

## Integration Points

### Real-World Adapters
Replace mock adapters with real services:
- **SMS**: Twilio, Azure Communication Services
- **Email**: SendGrid, Azure Communication Services  
- **Calendar**: Google Calendar, Microsoft Graph
- **Storage**: Azure Blob Storage, AWS S3
- **Weather**: NOAA API, Weather Underground

### Xactimate Integration
The .NET service provides Xactimate export functionality:
- Generates ESX files compatible with Xactimate
- Exports PDF estimates
- Imports adjuster scopes
- Handles code compliance requirements

### Azure Services Integration
Built-in support for Azure enterprise services:
- **Azure Communication Services**: Production SMS/Email
- **Azure Blob Storage**: File storage and retrieval
- **Azure Functions**: Serverless workflow triggers
- **Azure Logic Apps**: Complex workflow orchestration

## Development

### Adding New Tools
1. Create tool definition in appropriate file
2. Add schema validation
3. Implement business logic
4. Add to tools export
5. Update main index.js

### Adding .NET Services
1. Create new controller in `Controllers/`
2. Define models in `Models/`
3. Implement service in `Services/`
4. Add adapter function in `dotnet.adapter.js`
5. Create corresponding MCP tool

### Testing
```bash
# Test .NET API directly
curl -X POST http://localhost:5000/api/stormintel/hail-stats \
  -H "Content-Type: application/json" \
  -d '{"latitude": 39.7817, "longitude": -89.6501, "date": "2025-09-29T00:00:00Z"}'

# Test MCP tools (requires MCP client)
# Use with GitHub Copilot, Claude Desktop, or custom MCP client
```

## Production Deployment

### Docker Support
```dockerfile
# Multi-stage build for both Node.js and .NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS dotnet-runtime
FROM node:18-alpine AS node-runtime
# ... (full Dockerfile available on request)
```

### Environment Variables
Set in production:
- `DOTNET_SERVICE_URL`: Internal service URL
- `AZURE_*`: Azure service connection strings
- External API keys for weather, pricing services

---

**Nelrock Contracting Storm Intelligence System**  
*Automating storm damage assessment and restoration workflows*
