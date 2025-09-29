import 'dotenv/config';
import { createMcpServer } from './mcp.js';
import { messagingTools } from './tools/messaging.js';
import { schedulingTools } from './tools/scheduling.js';
import { stormIntelTools, estimatingTools } from './tools/storm-intel.js';
import { claimAnalysisTools } from './tools/claim-analysis.js';

const tools = {
  ...messagingTools,
  ...schedulingTools,
  ...stormIntelTools,
  ...estimatingTools,
  ...claimAnalysisTools
};

const server = createMcpServer({ tools });
server.start();
