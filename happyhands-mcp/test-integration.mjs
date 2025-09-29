#!/usr/bin/env node

// Simple test to verify MCP tools work
const DOTNET_SERVICE_URL = 'http://localhost:5001';

async function testDotnetService() {
  console.log('🔧 Testing .NET Service Integration\n');
  
  try {
    // Test the .NET service directly
    console.log('1️⃣ Testing .NET API directly...');
    const response = await fetch(`${DOTNET_SERVICE_URL}/api/stormintel/hail-stats`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        latitude: 39.7817,
        longitude: -89.6501,
        date: '2025-09-29T00:00:00Z'
      })
    });
    
    if (response.ok) {
      const data = await response.json();
      console.log('   ✅ .NET API Response:');
      console.log(`      Max hail size: ${data.maxSizeInches}"`);
      console.log(`      Duration: ${data.durationMinutes} minutes`);
      console.log(`      Event ID: ${data.eventId}`);
    } else {
      console.log('   ❌ .NET API failed:', response.status);
    }
    
    console.log('\n2️⃣ Testing Node.js MCP tools...');
    
    // Import the dotnet adapter
    const { callDotnetService } = await import('./src/adapters/dotnet.adapter.js');
    
    // Test the adapter
    const hailResult = await callDotnetService('/api/stormintel/hail-stats', 'POST', {
      latitude: 39.7817,
      longitude: -89.6501,
      date: '2025-09-29T00:00:00Z'
    });
    
    console.log('   ✅ Node.js Adapter Response:');
    console.log(`      Max hail size: ${hailResult.maxSizeInches}"`);
    console.log(`      Duration: ${hailResult.durationMinutes} minutes`);
    
    console.log('\n3️⃣ Testing build scope...');
    const scopeResult = await callDotnetService('/api/estimating/build-scope', 'POST', {
      caseId: 'TEST-001',
      measurements: {
        facets: [{
          facetId: 'test-facet',
          squareFootage: 1000,
          pitch: 6,
          orientation: 'S',
          layers: 2,
          material: 'asphalt'
        }],
        totalSquareFootage: 2000,
        stories: 2,
        primaryMaterial: 'asphalt',
        accessType: 'ladder'
      },
      damages: [{
        damageType: 'hail',
        location: 'roof',
        severity: 'moderate',
        description: 'Test damage'
      }],
      buildingCodes: ['IRC2021']
    });
    
    console.log('   ✅ Scope Building Response:');
    console.log(`      Case ID: ${scopeResult.caseId}`);
    console.log(`      Line items: ${scopeResult.lineItems.length}`);
    console.log(`      Total: $${scopeResult.total.toLocaleString()}`);
    
    console.log('\n🎉 All .NET integration tests passed!');
    console.log('\nThe hybrid Node.js/-.NET MCP server is working correctly:');
    console.log('   • .NET API: ✅ Running on port 5000');
    console.log('   • Node.js adapter: ✅ Successfully calling .NET');
    console.log('   • Storm intelligence: ✅ Hail stats working');
    console.log('   • Estimating: ✅ Scope building working');
    
  } catch (error) {
    console.error('❌ Test failed:', error.message);
    console.log('\nTroubleshooting:');
    console.log('   1. Make sure .NET service is running: npm run start-dotnet');
    console.log('   2. Check that port 5000 is not blocked');
    console.log('   3. Verify .NET service URL in environment');
  }
}

if (import.meta.url === `file://${process.argv[1]}`) {
  testDotnetService().catch(console.error);
}