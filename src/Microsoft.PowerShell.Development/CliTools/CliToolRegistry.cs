// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PowerShell.Development.CliTools
{
    /// <summary>
    /// Represents a registered CLI tool with normalized interface.
    /// </summary>
    public class CliToolDefinition
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> ParameterMappings { get; set; }
        public Dictionary<string, string> ErrorPatterns { get; set; }
        public Dictionary<string, int> ExitCodeMappings { get; set; }
        public List<string> CommonCommands { get; set; }
        public string HelpUrl { get; set; }

        public CliToolDefinition()
        {
            ParameterMappings = new Dictionary<string, string>();
            ErrorPatterns = new Dictionary<string, string>();
            ExitCodeMappings = new Dictionary<string, int>();
            CommonCommands = new List<string>();
        }
    }

    /// <summary>
    /// Result from invoking a CLI tool.
    /// </summary>
    public class CliToolResult
    {
        public string ToolName { get; set; }
        public string Command { get; set; }
        public int ExitCode { get; set; }
        public string ExitCodeCategory { get; set; }
        public List<string> Output { get; set; }
        public List<string> Errors { get; set; }
        public List<CliToolError> CategorizedErrors { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }

        public CliToolResult()
        {
            Output = new List<string>();
            Errors = new List<string>();
            CategorizedErrors = new List<CliToolError>();
        }
    }

    /// <summary>
    /// Represents a categorized error from a CLI tool.
    /// </summary>
    public class CliToolError
    {
        public string Category { get; set; }
        public string Message { get; set; }
        public string File { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string Severity { get; set; }
    }

    /// <summary>
    /// Global registry of CLI tools.
    /// </summary>
    public static class CliToolRegistry
    {
        private static readonly Dictionary<string, CliToolDefinition> _tools = new Dictionary<string, CliToolDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new object();

        static CliToolRegistry()
        {
            // Pre-register common tools
            RegisterDefaultTools();
        }

        public static void Register(CliToolDefinition tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (string.IsNullOrWhiteSpace(tool.Name))
                throw new ArgumentException("Tool name cannot be empty", nameof(tool));

            lock (_lock)
            {
                _tools[tool.Name] = tool;
            }
        }

        public static bool Unregister(string name)
        {
            lock (_lock)
            {
                return _tools.Remove(name);
            }
        }

        public static CliToolDefinition Get(string name)
        {
            lock (_lock)
            {
                return _tools.TryGetValue(name, out var tool) ? tool : null;
            }
        }

        public static List<CliToolDefinition> GetAll()
        {
            lock (_lock)
            {
                return _tools.Values.ToList();
            }
        }

        public static bool IsRegistered(string name)
        {
            lock (_lock)
            {
                return _tools.ContainsKey(name);
            }
        }

        private static void RegisterDefaultTools()
        {
            // Git
            Register(new CliToolDefinition
            {
                Name = "git",
                ExecutablePath = "git",
                Description = "Distributed version control system",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Message", "-m" },
                    { "Verbose", "-v" },
                    { "All", "-a" },
                    { "Force", "-f" },
                    { "Branch", "-b" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "fatal:", "Critical" },
                    { "error:", "Error" },
                    { "warning:", "Warning" },
                    { "hint:", "Information" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "GeneralError", 1 },
                    { "MisusedCommand", 128 }
                },
                CommonCommands = new List<string> { "status", "add", "commit", "push", "pull", "clone", "log" },
                HelpUrl = "https://git-scm.com/docs"
            });

            // npm
            Register(new CliToolDefinition
            {
                Name = "npm",
                ExecutablePath = "npm",
                Description = "Node.js package manager",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Global", "-g" },
                    { "Save", "--save" },
                    { "SaveDev", "--save-dev" },
                    { "Production", "--production" },
                    { "Verbose", "--verbose" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "ERR!", "Error" },
                    { "WARN", "Warning" },
                    { "notice", "Information" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "Error", 1 }
                },
                CommonCommands = new List<string> { "install", "test", "run", "publish", "update", "start" },
                HelpUrl = "https://docs.npmjs.com"
            });

            // cargo (Rust)
            Register(new CliToolDefinition
            {
                Name = "cargo",
                ExecutablePath = "cargo",
                Description = "Rust package manager and build tool",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Release", "--release" },
                    { "Verbose", "-v" },
                    { "Quiet", "-q" },
                    { "All", "--all" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "error:", "Error" },
                    { "error[E", "Error" },
                    { "warning:", "Warning" },
                    { "note:", "Information" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "CompilationError", 101 }
                },
                CommonCommands = new List<string> { "build", "test", "run", "check", "clean", "update" },
                HelpUrl = "https://doc.rust-lang.org/cargo/"
            });

            // dotnet
            Register(new CliToolDefinition
            {
                Name = "dotnet",
                ExecutablePath = "dotnet",
                Description = ".NET SDK CLI",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Configuration", "--configuration" },
                    { "Verbose", "--verbosity" },
                    { "Output", "--output" },
                    { "Framework", "--framework" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "error :", "Error" },
                    { "warning :", "Warning" },
                    { "info :", "Information" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "Error", 1 }
                },
                CommonCommands = new List<string> { "build", "test", "run", "restore", "publish", "clean" },
                HelpUrl = "https://docs.microsoft.com/dotnet/core/tools/"
            });

            // python/pip
            Register(new CliToolDefinition
            {
                Name = "pip",
                ExecutablePath = "pip",
                Description = "Python package installer",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Upgrade", "--upgrade" },
                    { "User", "--user" },
                    { "Verbose", "-v" },
                    { "Quiet", "-q" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "ERROR:", "Error" },
                    { "WARNING:", "Warning" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "Error", 1 }
                },
                CommonCommands = new List<string> { "install", "uninstall", "list", "freeze", "show" },
                HelpUrl = "https://pip.pypa.io/en/stable/"
            });

            // docker
            Register(new CliToolDefinition
            {
                Name = "docker",
                ExecutablePath = "docker",
                Description = "Container platform",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Detached", "-d" },
                    { "Interactive", "-i" },
                    { "TTY", "-t" },
                    { "Verbose", "-v" },
                    { "Remove", "--rm" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "Error:", "Error" },
                    { "ERROR:", "Error" },
                    { "Warning:", "Warning" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "Error", 1 },
                    { "ContainerExitCode", 125 }
                },
                CommonCommands = new List<string> { "build", "run", "ps", "stop", "rm", "images", "pull", "push" },
                HelpUrl = "https://docs.docker.com/reference/"
            });

            // kubectl
            Register(new CliToolDefinition
            {
                Name = "kubectl",
                ExecutablePath = "kubectl",
                Description = "Kubernetes CLI",
                ParameterMappings = new Dictionary<string, string>
                {
                    { "Namespace", "-n" },
                    { "AllNamespaces", "--all-namespaces" },
                    { "Output", "-o" },
                    { "Verbose", "-v" }
                },
                ErrorPatterns = new Dictionary<string, string>
                {
                    { "Error:", "Error" },
                    { "error:", "Error" },
                    { "Warning:", "Warning" }
                },
                ExitCodeMappings = new Dictionary<string, int>
                {
                    { "Success", 0 },
                    { "Error", 1 }
                },
                CommonCommands = new List<string> { "get", "describe", "apply", "delete", "logs", "exec" },
                HelpUrl = "https://kubernetes.io/docs/reference/kubectl/"
            });
        }
    }
}
