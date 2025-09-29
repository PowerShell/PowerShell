#!/usr/bin/env node

import { spawn } from 'child_process';
import { readFile } from 'fs/promises';

// Simple MCP client for testing
class MCPTestClient {
  constructor() {
    this.messageId = 1;
    this.responsePromises = new Map();
  }

  async start() {
    console.log('🔧 Starting Nelrock Contracting MCP Test Client\n');
    
    // Start the MCP server
    this.server = spawn('node', ['src/index.js'], {
      cwd: '/PowerShell/happyhands-mcp',
      stdio: ['pipe', 'pipe', 'inherit'],
      env: { ...process.env, DOTNET_SERVICE_URL: 'http://localhost:5001' }
    });

    this.server.stdout.on('data', (data) => {
      this.handleServerMessage(data.toString());
    });

    // Initialize the server
    await this.sendRequest('initialize', {
      protocolVersion: '2024-09-01',
      clientInfo: { name: 'nelrock-test-client', version: '1.0.0' },
      capabilities: {}
    });

    console.log('✅ MCP Server initialized\n');
    
    // Run tests
    await this.runTests();
  }

  async sendRequest(method, params = {}) {
    const message = {
      jsonrpc: '2.0',
      id: this.messageId++,
      method,
      params
    };

    return new Promise((resolve, reject) => {
      this.responsePromises.set(message.id, { resolve, reject });
      
      const json = JSON.stringify(message);
      const frame = `Content-Length: ${Buffer.byteLength(json, 'utf8')}\r\n\r\n${json}`;
      this.server.stdin.write(frame);
      
      // Timeout after 10 seconds
      setTimeout(() => {
        if (this.responsePromises.has(message.id)) {
          this.responsePromises.delete(message.id);
          reject(new Error(`Request timeout: ${method}`));
        }
      }, 10000);
    });
  }

  handleServerMessage(data) {
    const messages = this.parseFramedMessages(data);
    
    for (const message of messages) {
      if (message.id && this.responsePromises.has(message.id)) {
        const { resolve, reject } = this.responsePromises.get(message.id);
        this.responsePromises.delete(message.id);
        
        if (message.error) {
          reject(new Error(message.error.message));
        } else {
          resolve(message.result);
        }
      }
    }
  }

  parseFramedMessages(data) {
    const messages = [];
    let buffer = data;

    while (buffer.length > 0) {
      const headerEnd = buffer.indexOf('\r\n\r\n');
      if (headerEnd === -1) break;

      const headers = buffer.slice(0, headerEnd);
      const contentLengthMatch = headers.match(/Content-Length: (\d+)/i);
      
      if (!contentLengthMatch) {
        buffer = buffer.slice(headerEnd + 4);
        continue;
      }

      const contentLength = parseInt(contentLengthMatch[1], 10);
      const messageStart = headerEnd + 4;
      
      if (buffer.length < messageStart + contentLength) break;

      const messageBody = buffer.slice(messageStart, messageStart + contentLength);
      
      try {
        const message = JSON.parse(messageBody);
        messages.push(message);
      } catch (e) {
        console.error('Failed to parse message:', e.message);
      }

      buffer = buffer.slice(messageStart + contentLength);
    }

    return messages;
  }

  async runTests() {
    console.log('🧪 Running MCP Tool Tests\n');

    try {
      // Test 1: List available tools
      console.log('1️⃣ Testing tools/list...');
      const tools = await this.sendRequest('tools/list');
      console.log(`   ✅ Found ${tools.tools.length} tools:`);
      tools.tools.forEach(tool => {
        console.log(`      • ${tool.name}: ${tool.description}`);
      });
      console.log();

      // Test 2: Template preview (Node.js tool)
      console.log('2️⃣ Testing template_preview (Node.js)...');
      const templateResult = await this.sendRequest('tools/call', {
        name: 'template_preview',
        arguments: {
          template_id: 'storm_initial',
          variables: {
            first_name: 'John',
            city: 'Springfield',
            event_date: 'September 29, 2025',
            hail_max: '1.75'
          }
        }
      });
      console.log('   ✅ Template preview:');
      console.log(`      "${templateResult.content[0].data.preview}"`);
      console.log();

      // Test 3: Send SMS (Node.js tool)
      console.log('3️⃣ Testing send_sms (Node.js)...');
      const smsResult = await this.sendRequest('tools/call', {
        name: 'send_sms',
        arguments: {
          to: '+15551234567',
          body: 'Test message from Nelrock Contracting MCP server'
        }
      });
      console.log('   ✅ SMS sent:');
      console.log(`      Status: ${smsResult.content[0].data.status}`);
      console.log();

      // Test 4: Hail stats (.NET tool)
      console.log('4️⃣ Testing hail_stats_at (.NET integration)...');
      const hailResult = await this.sendRequest('tools/call', {
        name: 'hail_stats_at',
        arguments: {
          lat: 39.7817,
          lon: -89.6501,
          date: '2025-09-29'
        }
      });
      console.log('   ✅ Hail statistics:');
      const hailData = hailResult.content[0].data;
      console.log(`      Max hail size: ${hailData.MaxSizeInches}" (${hailData.DurationMinutes} min)`);
      console.log(`      Event ID: ${hailData.EventId}`);
      console.log();

      // Test 5: Build scope (.NET tool)
      console.log('5️⃣ Testing build_scope (.NET integration)...');
      const scopeResult = await this.sendRequest('tools/call', {
        name: 'build_scope',
        arguments: {
          case_id: 'NRC-2025-TEST-001',
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
        }
      });
      console.log('   ✅ Repair scope generated:');
      const scopeData = scopeResult.content[0].data;
      console.log(`      Case ID: ${scopeData.CaseId}`);
      console.log(`      Line items: ${scopeData.LineItems.length}`);
      console.log(`      Total cost: $${scopeData.Total.toLocaleString()}`);
      console.log();

      console.log('🎉 All tests completed successfully!');
      console.log('\n📋 Summary:');
      console.log('   • Node.js MCP server: ✅ Working');
      console.log('   • .NET API integration: ✅ Working');
      console.log('   • Messaging tools: ✅ Working');
      console.log('   • Storm intelligence: ✅ Working');
      console.log('   • Estimating tools: ✅ Working');

    } catch (error) {
      console.error('❌ Test failed:', error.message);
    } finally {
      this.server.kill();
      process.exit(0);
    }
  }
}

// Check if .NET service is running
async function checkDotnetService() {
  try {
    const response = await fetch('http://localhost:5001/swagger/index.html');
    return response.ok;
  } catch {
    return false;
  }
}

// Main execution
async function main() {
  console.log('Checking if .NET service is running...');
  const dotnetRunning = await checkDotnetService();
  
  if (!dotnetRunning) {
    console.log('⚠️  .NET service not detected on port 5001');
    console.log('   Run this first: cd dotnet-services/NelrockContracting.Services && dotnet run');
    console.log('   Then run this test again.');
    process.exit(1);
  }

  console.log('✅ .NET service detected\n');
  
  const client = new MCPTestClient();
  await client.start();
}

if (import.meta.url === `file://${process.argv[1]}`) {
  main().catch(console.error);
}
