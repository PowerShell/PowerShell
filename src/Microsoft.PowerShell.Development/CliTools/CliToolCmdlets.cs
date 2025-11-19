// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.CliTools
{
    /// <summary>
    /// Register-CliTool cmdlet for registering CLI tools.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "CliTool")]
    [OutputType(typeof(CliToolDefinition))]
    public sealed class RegisterCliToolCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        [ValidateNotNullOrEmpty]
        public string ExecutablePath { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public Hashtable ParameterMappings { get; set; }

        [Parameter]
        public Hashtable ErrorPatterns { get; set; }

        [Parameter]
        public Hashtable ExitCodeMappings { get; set; }

        [Parameter]
        public string[] CommonCommands { get; set; }

        [Parameter]
        public string HelpUrl { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var tool = new CliToolDefinition
            {
                Name = Name,
                ExecutablePath = ExecutablePath ?? Name,
                Description = Description ?? string.Empty,
                HelpUrl = HelpUrl ?? string.Empty
            };

            // Convert hashtables to dictionaries
            if (ParameterMappings != null)
            {
                foreach (var key in ParameterMappings.Keys)
                {
                    tool.ParameterMappings[key.ToString()] = ParameterMappings[key]?.ToString() ?? string.Empty;
                }
            }

            if (ErrorPatterns != null)
            {
                foreach (var key in ErrorPatterns.Keys)
                {
                    tool.ErrorPatterns[key.ToString()] = ErrorPatterns[key]?.ToString() ?? string.Empty;
                }
            }

            if (ExitCodeMappings != null)
            {
                foreach (var key in ExitCodeMappings.Keys)
                {
                    if (int.TryParse(ExitCodeMappings[key]?.ToString(), out int code))
                    {
                        tool.ExitCodeMappings[key.ToString()] = code;
                    }
                }
            }

            if (CommonCommands != null)
            {
                tool.CommonCommands.AddRange(CommonCommands);
            }

            CliToolRegistry.Register(tool);

            WriteVerbose($"Registered CLI tool: {Name}");

            if (PassThru.IsPresent)
            {
                WriteObject(tool);
            }
        }
    }

    /// <summary>
    /// Get-CliTool cmdlet for retrieving registered CLI tools.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CliTool")]
    [OutputType(typeof(CliToolDefinition))]
    public sealed class GetCliToolCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [SupportsWildcards]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (string.IsNullOrEmpty(Name))
            {
                // Return all tools
                foreach (var tool in CliToolRegistry.GetAll().OrderBy(t => t.Name))
                {
                    WriteObject(tool);
                }
            }
            else
            {
                var pattern = new WildcardPattern(Name, WildcardOptions.IgnoreCase);
                var tools = CliToolRegistry.GetAll()
                    .Where(t => pattern.IsMatch(t.Name))
                    .OrderBy(t => t.Name);

                foreach (var tool in tools)
                {
                    WriteObject(tool);
                }
            }
        }
    }

    /// <summary>
    /// Unregister-CliTool cmdlet for removing registered CLI tools.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Unregister, "CliTool", SupportsShouldProcess = true)]
    public sealed class UnregisterCliToolCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (ShouldProcess(Name, "Unregister CLI tool"))
            {
                if (CliToolRegistry.Unregister(Name))
                {
                    WriteVerbose($"Unregistered CLI tool: {Name}");
                }
                else
                {
                    WriteWarning($"CLI tool not found: {Name}");
                }
            }
        }
    }

    /// <summary>
    /// Invoke-CliTool cmdlet for executing registered CLI tools with normalized interface.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "CliTool")]
    [OutputType(typeof(CliToolResult))]
    public sealed class InvokeCliToolCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Tool { get; set; }

        [Parameter(Position = 1, ValueFromRemainingArguments = true)]
        public string[] Arguments { get; set; }

        [Parameter]
        public Hashtable Parameters { get; set; }

        [Parameter]
        public string WorkingDirectory { get; set; }

        [Parameter]
        public int Timeout { get; set; } = 300000; // 5 minutes default

        [Parameter]
        public SwitchParameter CategorizeErrors { get; set; }

        protected override void ProcessRecord()
        {
            var toolDef = CliToolRegistry.Get(Tool);
            if (toolDef == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"CLI tool '{Tool}' is not registered. Use Register-CliTool to register it first, or use Get-CliTool to see available tools."),
                    "ToolNotRegistered",
                    ErrorCategory.ObjectNotFound,
                    Tool));
                return;
            }

            // Build arguments
            var argList = new List<string>();

            // Add mapped parameters
            if (Parameters != null)
            {
                foreach (var key in Parameters.Keys)
                {
                    var paramName = key.ToString();
                    var paramValue = Parameters[key];

                    if (toolDef.ParameterMappings.TryGetValue(paramName, out string mappedFlag))
                    {
                        argList.Add(mappedFlag);
                        if (paramValue != null && !(paramValue is bool))
                        {
                            argList.Add(paramValue.ToString());
                        }
                    }
                    else
                    {
                        WriteWarning($"Parameter '{paramName}' is not defined in tool mapping for '{Tool}'");
                    }
                }
            }

            // Add remaining arguments
            if (Arguments != null)
            {
                argList.AddRange(Arguments);
            }

            var result = new CliToolResult
            {
                ToolName = Tool,
                Command = $"{toolDef.ExecutablePath} {string.Join(" ", argList)}"
            };

            var startTime = DateTime.Now;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = toolDef.ExecutablePath,
                    Arguments = string.Join(" ", argList),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = WorkingDirectory ?? SessionState.Path.CurrentFileSystemLocation.Path
                };

                using (var process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            result.Output.Add(e.Data);
                            WriteVerbose($"[OUT] {e.Data}");
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            result.Errors.Add(e.Data);
                            WriteVerbose($"[ERR] {e.Data}");
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool exited = process.WaitForExit(Timeout);
                    result.Duration = DateTime.Now - startTime;

                    if (!exited)
                    {
                        process.Kill();
                        WriteError(new ErrorRecord(
                            new TimeoutException($"Command timed out after {Timeout}ms"),
                            "CommandTimeout",
                            ErrorCategory.OperationTimeout,
                            Tool));
                        result.Success = false;
                        result.ExitCode = -1;
                        WriteObject(result);
                        return;
                    }

                    result.ExitCode = process.ExitCode;
                    result.Success = process.ExitCode == 0;

                    // Map exit code to category
                    var exitCodeMapping = toolDef.ExitCodeMappings.FirstOrDefault(kvp => kvp.Value == process.ExitCode);
                    result.ExitCodeCategory = exitCodeMapping.Key ?? (process.ExitCode == 0 ? "Success" : "Error");
                }

                // Categorize errors if requested
                if (CategorizeErrors.IsPresent && toolDef.ErrorPatterns.Count > 0)
                {
                    foreach (var line in result.Errors.Concat(result.Output))
                    {
                        foreach (var pattern in toolDef.ErrorPatterns)
                        {
                            if (line.Contains(pattern.Key))
                            {
                                var error = new CliToolError
                                {
                                    Category = pattern.Value,
                                    Message = line,
                                    Severity = pattern.Value
                                };

                                // Try to extract file/line/column info
                                var match = Regex.Match(line, @"(?<file>[^:]+):(?<line>\d+):(?<col>\d+)");
                                if (match.Success)
                                {
                                    error.File = match.Groups["file"].Value;
                                    if (int.TryParse(match.Groups["line"].Value, out int lineNum))
                                        error.Line = lineNum;
                                    if (int.TryParse(match.Groups["col"].Value, out int colNum))
                                        error.Column = colNum;
                                }

                                result.CategorizedErrors.Add(error);
                                break;
                            }
                        }
                    }
                }

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "InvokeCliToolError",
                    ErrorCategory.InvalidOperation,
                    Tool));
            }
        }
    }
}
