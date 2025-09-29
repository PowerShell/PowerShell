#!/bin/bash

echo "=== Starting Nelrock Contracting MCP Server with .NET Integration ==="
echo "This demonstrates the hybrid Node.js/-.NET architecture"
echo ""

# Start .NET API in background
echo "1. Starting .NET API service on port 5001..."
cd dotnet-services/NelrockContracting.Services
/root/.dotnet/dotnet run &
DOTNET_PID=$!

# Give .NET service time to start
echo "   Waiting for .NET service to initialize..."
sleep 5

# Start Node.js MCP server
echo ""
echo "2. Starting Node.js MCP server..."
cd ../../
echo "   MCP server ready for connections"
echo "   .NET service PID: $DOTNET_PID"
echo ""
echo "=== Available MCP Tools ==="
echo "• Messaging: send_sms, send_email, template_preview"
echo "• Scheduling: availability, book_slot, reschedule, cancel"
echo "• Storm Intel (.NET): fetch_storm_swath, hail_stats_at, intersect_service_area, event_summary"
echo "• Estimating (.NET): build_scope, export_xactimate, calculate_material_costs"
echo ""
echo "Press Ctrl+C to stop both services"

# Set up trap to clean up background process
trap "echo 'Stopping services...'; kill $DOTNET_PID 2>/dev/null; exit" INT TERM

# Start MCP server
DOTNET_SERVICE_URL=http://localhost:5001 node src/index.js
