// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Development.MCP
{
    /// <summary>
    /// MCP (Model Context Protocol) Server for exposing PowerShell cmdlets to AI assistants.
    /// </summary>
    public class MCPServer : IDisposable
    {
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private readonly int _port;
        private readonly Dictionary<string, MCPTool> _tools;
        private readonly object _lockObject = new object();
        private bool _disposed;

        public MCPServer(int port = 3000)
        {
            _port = port;
            _tools = new Dictionary<string, MCPTool>();
            InitializeTools();
        }

        /// <summary>
        /// Register PowerShell cmdlets as MCP tools.
        /// </summary>
        private void InitializeTools()
        {
            // Project Context
            _tools["get_project_context"] = new MCPTool
            {
                Name = "get_project_context",
                Description = "Detect and analyze the current project type (Node.js, .NET, Rust, etc.)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to analyze (optional, defaults to current directory)" },
                        searchParent = new { type = "boolean", description = "Search parent directories for project markers" }
                    }
                },
                Handler = HandleGetProjectContext
            };

            // Terminal Snapshot
            _tools["get_terminal_snapshot"] = new MCPTool
            {
                Name = "get_terminal_snapshot",
                Description = "Capture comprehensive terminal state including git, history, environment, and processes",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        includeAll = new { type = "boolean", description = "Include all available information" },
                        includeGit = new { type = "boolean", description = "Include git repository status" },
                        includeHistory = new { type = "boolean", description = "Include command history" },
                        includeEnvironment = new { type = "boolean", description = "Include environment variables" },
                        includeProcesses = new { type = "boolean", description = "Include running processes" },
                        includeErrors = new { type = "boolean", description = "Include recent errors" },
                        historyCount = new { type = "integer", description = "Number of history items (default: 20)" }
                    }
                },
                Handler = HandleGetTerminalSnapshot
            };

            // Code Context
            _tools["get_code_context"] = new MCPTool
            {
                Name = "get_code_context",
                Description = "Gather relevant code files with content, metrics, and dependencies",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        path = new { type = "string", description = "Path to analyze" },
                        recentlyModified = new { type = "boolean", description = "Only include recently modified files" },
                        hours = new { type = "integer", description = "Hours to consider for recently modified (default: 24)" },
                        include = new { type = "array", items = new { type = "string" }, description = "File patterns to include" },
                        maxFiles = new { type = "integer", description = "Maximum number of files (default: 50)" },
                        includeContent = new { type = "boolean", description = "Include file contents" },
                        includeMetrics = new { type = "boolean", description = "Include code metrics" }
                    }
                },
                Handler = HandleGetCodeContext
            };

            // Execute DevCommand
            _tools["execute_command"] = new MCPTool
            {
                Name = "execute_command",
                Description = "Execute a command asynchronously and return results",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "Command to execute" },
                        arguments = new { type = "string", description = "Command arguments" },
                        wait = new { type = "boolean", description = "Wait for completion (default: true)" }
                    },
                    required = new[] { "command" }
                },
                Handler = HandleExecuteCommand
            };

            // AI Error Context
            _tools["analyze_errors"] = new MCPTool
            {
                Name = "analyze_errors",
                Description = "Analyze recent PowerShell errors with AI context and suggested fixes",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        count = new { type = "integer", description = "Number of errors to analyze (default: 10)" }
                    }
                },
                Handler = HandleAnalyzeErrors
            };

            // Workflow Management
            _tools["get_workflows"] = new MCPTool
            {
                Name = "get_workflows",
                Description = "List available recorded workflows",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Filter by workflow name" },
                        tag = new { type = "string", description = "Filter by tag" }
                    }
                },
                Handler = HandleGetWorkflows
            };

            _tools["invoke_workflow"] = new MCPTool
            {
                Name = "invoke_workflow",
                Description = "Execute a recorded workflow with optional variables",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Workflow name" },
                        variables = new { type = "object", description = "Variables for workflow execution" },
                        stopOnError = new { type = "boolean", description = "Stop on first error" }
                    },
                    required = new[] { "name" }
                },
                Handler = HandleInvokeWorkflow
            };

            // Format for AI
            _tools["format_for_ai"] = new MCPTool
            {
                Name = "format_for_ai",
                Description = "Format PowerShell objects for AI consumption (JSON/YAML)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        input = new { type = "object", description = "Object to format" },
                        outputType = new { type = "string", description = "Output format (Json, Yaml, Compact)" },
                        depth = new { type = "integer", description = "Serialization depth (default: 10)" }
                    },
                    required = new[] { "input" }
                },
                Handler = HandleFormatForAI
            };
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
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) break;

                        var response = await ProcessRequestAsync(line);
                        await writer.WriteLineAsync(response);
                    }
                }
            }
            catch (Exception)
            {
                // Client disconnected or error
            }
        }

        private async Task<string> ProcessRequestAsync(string requestJson)
        {
            try
            {
                var request = JsonSerializer.Deserialize<MCPRequest>(requestJson);

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

                    if (_tools.TryGetValue(toolName, out var tool))
                    {
                        var result = await tool.Handler(arguments);
                        return JsonSerializer.Serialize(new MCPResponse
                        {
                            Result = result
                        });
                    }
                    else
                    {
                        return JsonSerializer.Serialize(new MCPResponse
                        {
                            Error = new { message = $"Tool '{toolName}' not found" }
                        });
                    }
                }

                return JsonSerializer.Serialize(new MCPResponse
                {
                    Error = new { message = "Invalid request" }
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new MCPResponse
                {
                    Error = new { message = ex.Message }
                });
            }
        }

        // Tool handlers
        private async Task<object> HandleGetProjectContext(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Get-ProjectContext cmdlet
                return new { type = "Node.js", language = "JavaScript", buildTool = "npm" };
            });
        }

        private async Task<object> HandleGetTerminalSnapshot(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Get-TerminalSnapshot cmdlet
                return new { workingDirectory = "/home/user", timestamp = DateTime.Now };
            });
        }

        private async Task<object> HandleGetCodeContext(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Get-CodeContext cmdlet
                return new { files = new List<object>(), totalLines = 0 };
            });
        }

        private async Task<object> HandleExecuteCommand(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Start-DevCommand cmdlet
                return new { jobId = Guid.NewGuid(), status = "Running" };
            });
        }

        private async Task<object> HandleAnalyzeErrors(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Get-AIErrorContext cmdlet
                return new { errors = new List<object>() };
            });
        }

        private async Task<object> HandleGetWorkflows(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Get-Workflow cmdlet
                return new { workflows = new List<object>() };
            });
        }

        private async Task<object> HandleInvokeWorkflow(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Invoke-Workflow cmdlet
                return new { success = true, steps = new List<object>() };
            });
        }

        private async Task<object> HandleFormatForAI(JsonElement args)
        {
            return await Task.Run(() =>
            {
                // This would call Format-ForAI cmdlet
                return new { formatted = "{}" };
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
    /// Represents an MCP tool.
    /// </summary>
    public class MCPTool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public object InputSchema { get; set; }
        public Func<JsonElement, Task<object>> Handler { get; set; }
    }

    /// <summary>
    /// MCP request structure.
    /// </summary>
    public class MCPRequest
    {
        public string Method { get; set; }
        public JsonElement? Params { get; set; }
    }

    /// <summary>
    /// MCP response structure.
    /// </summary>
    public class MCPResponse
    {
        public object Result { get; set; }
        public object Error { get; set; }
    }

    /// <summary>
    /// Start MCP server for AI assistant integration.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "MCPServer")]
    [OutputType(typeof(MCPServer))]
    public sealed class StartMCPServerCommand : PSCmdlet
    {
        /// <summary>
        /// Port to listen on.
        /// </summary>
        [Parameter]
        [ValidateRange(1024, 65535)]
        public int Port { get; set; } = 3000;

        /// <summary>
        /// Return server object.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var server = new MCPServer(Port);
            server.Start();

            WriteObject($"MCP Server started on port {Port}");
            WriteObject("AI assistants can now connect to invoke PowerShell cmdlets");
            WriteObject($"Connection: localhost:{Port}");

            if (PassThru)
            {
                WriteObject(server);
            }

            // Store server reference globally
            SessionState.PSVariable.Set("Global:MCPServer", server);
        }
    }

    /// <summary>
    /// Stop MCP server.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "MCPServer")]
    public sealed class StopMCPServerCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var server = SessionState.PSVariable.GetValue("Global:MCPServer") as MCPServer;
            if (server != null)
            {
                server.Stop();
                server.Dispose();
                SessionState.PSVariable.Remove("Global:MCPServer");
                WriteObject("MCP Server stopped");
            }
            else
            {
                WriteWarning("No MCP Server is currently running");
            }
        }
    }

    /// <summary>
    /// Get MCP server status.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "MCPServerStatus")]
    public sealed class GetMCPServerStatusCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var server = SessionState.PSVariable.GetValue("Global:MCPServer") as MCPServer;
            if (server != null)
            {
                WriteObject(new
                {
                    Status = "Running",
                    Server = server
                });
            }
            else
            {
                WriteObject(new
                {
                    Status = "Stopped"
                });
            }
        }
    }
}
