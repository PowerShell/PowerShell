const PROTOCOL_VERSION = '2024-09-01'; // descriptive tag for your client
const SERVER_NAME = 'nelrock-contracting-mcp';
const SERVER_VERSION = '0.1.0';

export function createMcpServer({ tools }) {
  const toolList = Object.entries(tools).map(([name, t]) => ({
    name,
    description: t.description,
    input_schema: t.schema
  }));

  function send(msg) {
    const json = JSON.stringify(msg);
    // MCP over stdio frames via Content-Length header
    const payload = `Content-Length: ${Buffer.byteLength(json, 'utf8')}\r\n\r\n${json}`;
    process.stdout.write(payload);
  }

  async function handleRequest(req) {
    // Basic MCP methods
    if (req.method === 'initialize') {
      return {
        protocolVersion: PROTOCOL_VERSION,
        serverInfo: { name: SERVER_NAME, version: SERVER_VERSION },
        capabilities: { experimental: true }
      };
    }

    if (req.method === 'tools/list') {
      return { tools: toolList };
    }

    if (req.method === 'tools/call') {
      const { name, arguments: args } = req.params || {};
      const tool = tools[name];
      if (!tool) throw new Error(`Unknown tool: ${name}`);
      const result = await tool.run(args);
      return { content: [{ type: 'json', data: result }] };
    }

    // Optional: simple ping
    if (req.method === 'ping') {
      return { pong: true };
    }

    throw new Error(`Unknown method: ${req.method}`);
  }

  function start() {
    let buffer = '';
    let contentLength = null;

    process.stdin.on('data', (chunk) => {
      buffer += chunk.toString('utf8');

      // Parse framed messages: headers then JSON body
      while (true) {
        if (contentLength === null) {
          const headerEnd = buffer.indexOf('\r\n\r\n');
          if (headerEnd === -1) break;
          const headers = buffer.slice(0, headerEnd).split('\r\n');
          const lenHeader = headers.find(h => /^Content-Length:/i.test(h));
          if (!lenHeader) {
            buffer = buffer.slice(headerEnd + 4);
            continue;
          }
          contentLength = parseInt(lenHeader.split(':')[1].trim(), 10);
          buffer = buffer.slice(headerEnd + 4);
        }

        if (buffer.length < contentLength) break;

        const body = buffer.slice(0, contentLength);
        buffer = buffer.slice(contentLength);
        contentLength = null;

        let msg;
        try {
          msg = JSON.parse(body);
        } catch (e) {
          // Malformed JSON
          send({
            jsonrpc: '2.0',
            id: null,
            error: { code: -32700, message: 'Parse error' }
          });
          continue;
        }

        (async () => {
          if (msg.jsonrpc !== '2.0') return;
          if (typeof msg.id === 'undefined') return;

          try {
            const result = await handleRequest(msg);
            send({ jsonrpc: '2.0', id: msg.id, result });
          } catch (err) {
            send({
              jsonrpc: '2.0',
              id: msg.id,
              error: { code: -32000, message: err.message || 'Server error' }
            });
          }
        })();
      }
    });

    process.stdin.resume();
  }

  return { start };
}
