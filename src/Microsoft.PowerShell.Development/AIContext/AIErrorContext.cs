// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.AIContext
{
    /// <summary>
    /// AI-friendly error context with analysis and suggestions.
    /// </summary>
    public class AIErrorContext
    {
        public string ErrorId { get; set; }
        public string Category { get; set; }
        public string OriginalMessage { get; set; }
        public string SimplifiedMessage { get; set; }
        public string RootCause { get; set; }
        public List<string> SuggestedFixes { get; set; }
        public List<string> DocumentationLinks { get; set; }
        public Dictionary<string, object> AdditionalContext { get; set; }
        public string Tool { get; set; }
        public string File { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public List<string> RelatedErrors { get; set; }
        public string Severity { get; set; }

        public AIErrorContext()
        {
            SuggestedFixes = new List<string>();
            DocumentationLinks = new List<string>();
            AdditionalContext = new Dictionary<string, object>();
            RelatedErrors = new List<string>();
        }
    }

    /// <summary>
    /// Error pattern for matching and analyzing errors.
    /// </summary>
    internal class ErrorPattern
    {
        public string Pattern { get; set; }
        public string Category { get; set; }
        public string RootCause { get; set; }
        public List<string> SuggestedFixes { get; set; }
        public List<string> DocumentationLinks { get; set; }
        public string Severity { get; set; }

        public ErrorPattern()
        {
            SuggestedFixes = new List<string>();
            DocumentationLinks = new List<string>();
        }
    }

    /// <summary>
    /// Analyzes errors and provides AI-friendly context.
    /// </summary>
    public static class ErrorAnalyzer
    {
        private static readonly List<ErrorPattern> _patterns = new List<ErrorPattern>();
        private static readonly object _lock = new object();

        static ErrorAnalyzer()
        {
            InitializePatterns();
        }

        private static void InitializePatterns()
        {
            // PowerShell common errors
            _patterns.Add(new ErrorPattern
            {
                Pattern = @"CommandNotFoundException|command not found|not recognized",
                Category = "CommandNotFound",
                RootCause = "The command or cmdlet does not exist or is not in the PATH",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Verify the command name is spelled correctly",
                    "Check if the module is imported: Import-Module <ModuleName>",
                    "Verify the command is installed: Get-Command <CommandName>",
                    "Add the executable directory to your PATH"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_command_precedence"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"ParameterBindingException|Cannot bind parameter",
                Category = "ParameterBinding",
                RootCause = "The parameter value is invalid or parameter name is incorrect",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Check parameter spelling and case sensitivity",
                    "Verify the parameter value type matches expected type",
                    "Use Get-Help <CommandName> -Parameter <ParameterName> to see parameter details",
                    "Check if the parameter is positional or requires -ParameterName"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_parameters"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"FileNotFoundException|cannot find|does not exist",
                Category = "FileNotFound",
                RootCause = "The specified file or path does not exist",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Verify the file path is correct",
                    "Use Test-Path to check if the file exists",
                    "Check for typos in the filename",
                    "Verify you're in the correct directory: Get-Location"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.management/test-path"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"UnauthorizedAccessException|Access.*denied|Permission denied",
                Category = "PermissionDenied",
                RootCause = "Insufficient permissions to perform the operation",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Run PowerShell as Administrator (Windows) or use sudo (Linux/macOS)",
                    "Check file/directory permissions",
                    "Verify you have write access to the location",
                    "Check if the file is locked by another process"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_execution_policies"
                }
            });

            // Build tool errors
            _patterns.Add(new ErrorPattern
            {
                Pattern = @"error CS\d+|compilation failed|syntax error",
                Category = "CompilationError",
                RootCause = "Code syntax error or compilation failure",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Check the error message for line and column numbers",
                    "Review recent code changes",
                    "Verify all namespaces are properly imported",
                    "Check for missing semicolons, braces, or parentheses"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/dotnet/csharp/language-reference/compiler-messages/"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"cannot find package|package not found|404",
                Category = "PackageNotFound",
                RootCause = "The requested package or dependency does not exist or cannot be accessed",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Verify the package name and version are correct",
                    "Check your package feed/registry is accessible",
                    "Run package restore: dotnet restore / npm install / cargo build",
                    "Verify network connectivity to package registry"
                },
                DocumentationLinks = new List<string>
                {
                    "https://www.nuget.org/",
                    "https://www.npmjs.com/",
                    "https://crates.io/"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"type mismatch|cannot convert|invalid cast",
                Category = "TypeMismatch",
                RootCause = "Value cannot be converted to the expected type",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Check the data type of the value being passed",
                    "Use explicit type conversion: [int]$value or [string]$value",
                    "Verify the value is not null before conversion",
                    "Check if the value contains the expected format"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_type_conversion"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"timeout|timed out|deadline exceeded",
                Category = "Timeout",
                RootCause = "Operation exceeded the allowed time limit",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Increase the timeout value if configurable",
                    "Check network connectivity",
                    "Verify the target service is responding",
                    "Consider using async operations for long-running tasks"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_jobs"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"out of memory|insufficient memory|OOM",
                Category = "OutOfMemory",
                RootCause = "Insufficient memory available to complete the operation",
                Severity = "Critical",
                SuggestedFixes = new List<string>
                {
                    "Process data in smaller chunks",
                    "Use streaming instead of loading everything into memory",
                    "Dispose of objects explicitly: .Dispose() or using statements",
                    "Increase available memory or optimize memory usage"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/dotnet/standard/garbage-collection/"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"null reference|object reference not set|NullReferenceException",
                Category = "NullReference",
                RootCause = "Attempting to use a null object",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Check if the object is null before using it: if ($obj -ne $null)",
                    "Use null-conditional operator: $obj?.Property",
                    "Verify the object was initialized properly",
                    "Check if a command returned null when a value was expected"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_operators"
                }
            });

            _patterns.Add(new ErrorPattern
            {
                Pattern = @"Module.*not found|cannot find module",
                Category = "ModuleNotFound",
                RootCause = "The specified PowerShell module is not installed or not in the module path",
                Severity = "Error",
                SuggestedFixes = new List<string>
                {
                    "Install the module: Install-Module <ModuleName>",
                    "Check module name spelling",
                    "Verify module path: $env:PSModulePath",
                    "Import module explicitly: Import-Module <ModuleName>"
                },
                DocumentationLinks = new List<string>
                {
                    "https://docs.microsoft.com/powershell/module/powershellget/install-module"
                }
            });
        }

        public static AIErrorContext Analyze(ErrorRecord errorRecord)
        {
            if (errorRecord == null)
                return null;

            var context = new AIErrorContext
            {
                ErrorId = errorRecord.FullyQualifiedErrorId,
                Category = errorRecord.CategoryInfo.Category.ToString(),
                OriginalMessage = errorRecord.Exception?.Message ?? errorRecord.ToString(),
                Severity = DetermineSeverity(errorRecord)
            };

            // Simplify the message
            context.SimplifiedMessage = SimplifyMessage(context.OriginalMessage);

            // Extract file/line info if available
            ExtractLocationInfo(errorRecord, context);

            // Match against patterns
            MatchPatterns(context);

            // Extract additional context
            ExtractAdditionalContext(errorRecord, context);

            return context;
        }

        private static void ExtractLocationInfo(ErrorRecord errorRecord, AIErrorContext context)
        {
            if (errorRecord.InvocationInfo != null)
            {
                context.File = errorRecord.InvocationInfo.ScriptName;
                context.Line = errorRecord.InvocationInfo.ScriptLineNumber;
                context.Column = errorRecord.InvocationInfo.OffsetInLine;
            }

            // Try to extract from error message
            if (string.IsNullOrEmpty(context.File))
            {
                var match = Regex.Match(context.OriginalMessage, @"(?<file>[^:]+):(?<line>\d+):(?<col>\d+)");
                if (match.Success)
                {
                    context.File = match.Groups["file"].Value;
                    if (int.TryParse(match.Groups["line"].Value, out int line))
                        context.Line = line;
                    if (int.TryParse(match.Groups["col"].Value, out int col))
                        context.Column = col;
                }
            }
        }

        private static void MatchPatterns(AIErrorContext context)
        {
            lock (_lock)
            {
                foreach (var pattern in _patterns)
                {
                    if (Regex.IsMatch(context.OriginalMessage, pattern.Pattern, RegexOptions.IgnoreCase))
                    {
                        if (string.IsNullOrEmpty(context.RootCause))
                            context.RootCause = pattern.RootCause;

                        context.SuggestedFixes.AddRange(pattern.SuggestedFixes);
                        context.DocumentationLinks.AddRange(pattern.DocumentationLinks);

                        if (string.IsNullOrEmpty(context.Severity) || pattern.Severity == "Critical")
                            context.Severity = pattern.Severity;

                        // Use the more specific category from pattern
                        if (pattern.Category != context.Category)
                        {
                            context.AdditionalContext["SpecificCategory"] = pattern.Category;
                        }

                        break; // Use first match
                    }
                }
            }

            // Default suggestions if none found
            if (context.SuggestedFixes.Count == 0)
            {
                context.SuggestedFixes.Add("Review the error message for specific details");
                context.SuggestedFixes.Add("Check recent changes that might have caused this error");
                context.SuggestedFixes.Add("Search for the error message online");
            }
        }

        private static void ExtractAdditionalContext(ErrorRecord errorRecord, AIErrorContext context)
        {
            if (errorRecord.TargetObject != null)
            {
                context.AdditionalContext["TargetObject"] = errorRecord.TargetObject.ToString();
            }

            if (errorRecord.InvocationInfo != null)
            {
                context.AdditionalContext["Command"] = errorRecord.InvocationInfo.MyCommand?.Name ?? "Unknown";
                context.AdditionalContext["Line"] = errorRecord.InvocationInfo.Line;
            }

            if (errorRecord.Exception != null)
            {
                context.AdditionalContext["ExceptionType"] = errorRecord.Exception.GetType().Name;
                if (errorRecord.Exception.InnerException != null)
                {
                    context.AdditionalContext["InnerException"] = errorRecord.Exception.InnerException.Message;
                }
            }
        }

        private static string SimplifyMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Remove stack traces
            var lines = message.Split('\n');
            var simplified = lines.FirstOrDefault() ?? message;

            // Remove common prefixes
            simplified = Regex.Replace(simplified, @"^(Exception|Error|Warning):\s*", "", RegexOptions.IgnoreCase);

            // Limit length
            if (simplified.Length > 200)
            {
                simplified = simplified.Substring(0, 197) + "...";
            }

            return simplified.Trim();
        }

        private static string DetermineSeverity(ErrorRecord errorRecord)
        {
            if (errorRecord.Exception is OutOfMemoryException ||
                errorRecord.Exception is StackOverflowException)
            {
                return "Critical";
            }

            switch (errorRecord.CategoryInfo.Category)
            {
                case ErrorCategory.SecurityError:
                case ErrorCategory.PermissionDenied:
                    return "Critical";
                case ErrorCategory.ResourceExists:
                case ErrorCategory.ResourceBusy:
                case ErrorCategory.NotSpecified:
                    return "Warning";
                default:
                    return "Error";
            }
        }

        public static void RegisterPattern(ErrorPattern pattern)
        {
            lock (_lock)
            {
                _patterns.Add(pattern);
            }
        }
    }
}
