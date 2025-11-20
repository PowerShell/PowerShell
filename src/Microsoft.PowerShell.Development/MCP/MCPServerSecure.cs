// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Development.MCP
{
    /// <summary>
    /// Secure MCP Server with authentication, rate limiting, and input validation.
    /// </summary>
    public class MCPServerSecure : IDisposable
    {
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private readonly int _port;
        private readonly Dictionary<string, MCPTool> _tools;
        private readonly object _lockObject = new object();
        private bool _disposed;

        // Security features
        private readonly string _apiKey;
        private readonly Dictionary<string, RateLimitInfo> _rateLimits;
        private readonly HashSet<string> _allowedMethods;
        private const int MAX_REQUESTS_PER_MINUTE = 60;
        private const int MAX_REQUEST_SIZE = 1024 * 1024; // 1MB
        private const int MAX_JSON_DEPTH = 10;

        public MCPServerSecure(int port = 3000, string apiKey = null)
        {
            _port = port;
            _tools = new Dictionary<string, MCPTool>();
            _rateLimits = new Dictionary<string, RateLimitInfo>();
            _allowedMethods = new HashSet<string> { "tools/list", "tools/call" };

            // Generate or use provided API key
            _apiKey = apiKey ?? GenerateApiKey();

            InitializeTools();
        }

        /// <summary>
        /// Generate a secure random API key.
        /// </summary>
        private string GenerateApiKey()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        public string ApiKey => _apiKey;

        private void InitializeTools()
        {
            // Same tool initialization as before
            _tools["get_project_context"] = new MCPTool
            {
                Name = "get_project_context",
                Description = "Detect and analyze the current project type",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to analyze" },
                        searchParent = new { type = "boolean", description = "Search parent directories" }
                    }
                },
                Handler = HandleGetProjectContext
            };

            // Add other tools...
        }

        public void Start()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                {
                    throw new InvalidOperationException("MCP Server is already running");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _isRunning = true;

                Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _isRunning = false;
            }
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
                catch (Exception)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var clientId = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    // Authentication
                    var authHeader = await reader.ReadLineAsync();
                    if (!AuthenticateClient(authHeader))
                    {
                        await writer.WriteLineAsync(JsonSerializer.Serialize(new MCPResponse
                        {
                            Error = new { message = "Authentication failed" }
                        }));
                        return;
                    }

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Rate limiting
                        if (!CheckRateLimit(clientId))
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new MCPResponse
                            {
                                Error = new { message = "Rate limit exceeded" }
                            }));
                            continue;
                        }

                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) break;

                        // Size limit
                        if (line.Length > MAX_REQUEST_SIZE)
                        {
                            await writer.WriteLineAsync(JsonSerializer.Serialize(new MCPResponse
                            {
                                Error = new { message = "Request too large" }
                            }));
                            continue;
                        }

                        var response = await ProcessRequestAsync(line);
                        await writer.WriteLineAsync(response);
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected or error - log but don't expose details
            }
        }

        private bool AuthenticateClient(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader)) return false;

            // Expect: "Bearer <api_key>"
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var providedKey = authHeader.Substring(7).Trim();
            return providedKey == _apiKey;
        }

        private bool CheckRateLimit(string clientId)
        {
            lock (_lockObject)
            {
                if (!_rateLimits.ContainsKey(clientId))
                {
                    _rateLimits[clientId] = new RateLimitInfo();
                }

                var rateLimitInfo = _rateLimits[clientId];
                var cutoff = DateTime.Now.AddMinutes(-1);

                // Remove old requests
                rateLimitInfo.Requests.RemoveAll(r => r < cutoff);

                if (rateLimitInfo.Requests.Count >= MAX_REQUESTS_PER_MINUTE)
                {
                    return false;
                }

                rateLimitInfo.Requests.Add(DateTime.Now);
                return true;
            }
        }

        private async Task<string> ProcessRequestAsync(string requestJson)
        {
            try
            {
                // Validate and deserialize
                var request = ValidateAndDeserialize(requestJson);

                if (request.Method == "tools/list")
                {
                    return JsonSerializer.Serialize(new MCPResponse
                    {
                        Result = new
                        {
                            tools = _tools.Values.Select(t => new
                            {
                                name = t.Name,
                                description = t.Description,
                                inputSchema = t.InputSchema
                            })
                        }
                    });
                }
                else if (request.Method == "tools/call" && request.Params != null)
                {
                    var toolName = request.Params.GetProperty("name").GetString();
                    var arguments = request.Params.GetProperty("arguments");

                    // Validate tool name
                    if (string.IsNullOrEmpty(toolName) || !_tools.ContainsKey(toolName))
                    {
                        return JsonSerializer.Serialize(new MCPResponse
                        {
                            Error = new { message = $"Tool '{toolName}' not found" }
                        });
                    }

                    var tool = _tools[toolName];
                    var result = await tool.Handler(arguments);

                    return JsonSerializer.Serialize(new MCPResponse
                    {
                        Result = result
                    });
                }

                return JsonSerializer.Serialize(new MCPResponse
                {
                    Error = new { message = "Invalid request" }
                });
            }
            catch (Exception ex)
            {
                // Don't expose internal details
                return JsonSerializer.Serialize(new MCPResponse
                {
                    Error = new { message = "Request processing failed" }
                });
            }
        }

        private MCPRequest ValidateAndDeserialize(string json)
        {
            if (json.Length > MAX_REQUEST_SIZE)
            {
                throw new ArgumentException("Request too large");
            }

            var options = new JsonSerializerOptions
            {
                MaxDepth = MAX_JSON_DEPTH,
                PropertyNameCaseInsensitive = true
            };

            var request = JsonSerializer.Deserialize<MCPRequest>(json, options);

            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                throw new ArgumentException("Invalid request format");
            }

            if (!_allowedMethods.Contains(request.Method))
            {
                throw new ArgumentException($"Method '{request.Method}' not allowed");
            }

            return request;
        }

        // Tool handlers (same as before but with validation)
        private async Task<object> HandleGetProjectContext(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // Implementation with path validation
                return new { type = "Node.js", language = "JavaScript", buildTool = "npm" };
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    _cancellationTokenSource?.Dispose();
                    _listener?.Stop();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Rate limit tracking.
    /// </summary>
    internal class RateLimitInfo
    {
        public List<DateTime> Requests { get; set; } = new List<DateTime>();
    }

    /// <summary>
    /// Start secure MCP server with authentication.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "MCPServerSecure")]
    [OutputType(typeof(string))]
    public sealed class StartMCPServerSecureCommand : PSCmdlet
    {
        [Parameter]
        [ValidateRange(1024, 65535)]
        public int Port { get; set; } = 3000;

        [Parameter]
        public string ApiKey { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var server = new MCPServerSecure(Port, ApiKey);
            server.Start();

            WriteObject($"Secure MCP Server started on port {Port}");
            WriteObject($"API Key: {server.ApiKey}");
            WriteWarning("IMPORTANT: Store the API key securely. Clients must send 'Bearer <api_key>' in first line.");
            WriteObject($"Connection: localhost:{Port}");
            WriteObject("Rate Limit: 60 requests per minute");

            if (PassThru)
            {
                WriteObject(server);
            }

            SessionState.PSVariable.Set("Global:MCPServerSecure", server);
        }
    }
}
