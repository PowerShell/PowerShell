// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.AIResponseParser
{
    /// <summary>
    /// Secure AI Response Parser with command whitelisting and path validation.
    /// </summary>
    [Cmdlet(VerbsData.Convert, "AIResponseSecure")]
    [Alias("parse-ai-secure", "aiparsesecure")]
    [OutputType(typeof(ParsedAIResponse))]
    public sealed class ConvertAIResponseSecureCommand : PSCmdlet
    {
        // Safe command whitelist - only read-only commands
        private static readonly HashSet<string> SafeCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Get-ChildItem", "Get-Content", "Get-Item", "Get-ItemProperty",
            "Get-Location", "Get-Process", "Get-Service", "Get-Variable",
            "Test-Path", "Select-Object", "Where-Object", "ForEach-Object",
            "Format-List", "Format-Table", "Out-String",
            "Get-Date", "Get-Host", "Get-PSVersion",
            // PowerShell Development module safe commands
            "Get-ProjectContext", "Get-TerminalSnapshot", "Get-CodeContext",
            "Get-Workflow", "Get-SmartSuggestion", "Get-RemoteTarget"
        };

        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Response { get; set; }

        [Parameter]
        public string FilePath { get; set; }

        [Parameter]
        public SwitchParameter ExtractCode { get; set; }

        [Parameter]
        public SwitchParameter ExtractCommands { get; set; }

        [Parameter]
        public SwitchParameter ExtractFiles { get; set; }

        [Parameter]
        public SwitchParameter All { get; set; }

        protected override void BeginProcessing()
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(FilePath);
                if (File.Exists(path))
                {
                    Response = File.ReadAllText(path);
                }
                else
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }
            }
        }

        protected override void ProcessRecord()
        {
            var parsed = new ParsedAIResponse
            {
                OriginalResponse = Response
            };

            if (ExtractCode || All)
            {
                parsed.CodeSuggestions = ExtractCodeBlocksSecure(Response);
            }

            if (ExtractCommands || All)
            {
                parsed.CommandSuggestions = ExtractCommandsSecure(Response);
            }

            if (ExtractFiles || All)
            {
                parsed.FileSuggestions = ExtractFileModificationsSecure(Response);
            }

            parsed.Summary = ExtractSummary(Response);
            parsed.ActionItems = ExtractActionItems(Response);

            WriteObject(parsed);
        }

        private List<CodeSuggestion> ExtractCodeBlocksSecure(string text)
        {
            var suggestions = new List<CodeSuggestion>();

            // Use regex with timeout to prevent DoS
            var pattern = @"```(\w+)?\s*\n(.*?)```";
            var regex = new Regex(pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(1));

            try
            {
                var matches = regex.Matches(text);

                foreach (Match match in matches)
                {
                    var language = match.Groups[1].Success ? match.Groups[1].Value : "text";
                    var code = match.Groups[2].Value.Trim();

                    // Size limit on code blocks
                    if (code.Length > 100 * 1024) // 100KB limit
                    {
                        WriteWarning("Code block too large, skipping");
                        continue;
                    }

                    var startIndex = match.Index;
                    var contextStart = Math.Max(0, startIndex - 200);
                    var context = text.Substring(contextStart, startIndex - contextStart).Trim();

                    string filePath = null;
                    int? lineNumber = null;

                    var fileMatch = Regex.Match(context, @"(?:in|file|at)\s+([^\s:]+\.[\w]+)(?::(\d+))?", RegexOptions.IgnoreCase);
                    if (fileMatch.Success)
                    {
                        filePath = fileMatch.Groups[1].Value;
                        if (fileMatch.Groups[2].Success)
                        {
                            lineNumber = int.Parse(fileMatch.Groups[2].Value);
                        }
                    }

                    var suggestionType = DetermineSuggestionType(context);

                    suggestions.Add(new CodeSuggestion
                    {
                        Language = language,
                        Code = code,
                        Context = context,
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Type = suggestionType
                    });
                }
            }
            catch (RegexMatchTimeoutException)
            {
                WriteWarning("Regex timeout - input too complex");
                return new List<CodeSuggestion>();
            }

            return suggestions;
        }

        private List<CommandSuggestion> ExtractCommandsSecure(string text)
        {
            var suggestions = new List<CommandSuggestion>();

            var patterns = new[]
            {
                @"(?:run|execute|type):\s*`([^`]+)`",
                @"\$\s*([^\n]+)",
                @"(?:Run|Execute|Try):\s*([^\n]+)",
                @"```(?:bash|sh|powershell|pwsh)\s*\n([^`]+)```"
            };

            foreach (var pattern in patterns)
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.Multiline, TimeSpan.FromSeconds(1));
                    var matches = regex.Matches(text);

                    foreach (Match match in matches)
                    {
                        var command = match.Groups[1].Value.Trim();
                        if (string.IsNullOrWhiteSpace(command)) continue;

                        // Extract base command
                        var cmdName = command.Split(new[] { ' ', ';', '|', '&' }, StringSplitOptions.RemoveEmptyEntries)[0];

                        // Check if command is in whitelist
                        if (!SafeCommands.Contains(cmdName))
                        {
                            WriteWarning($"Command '{cmdName}' is not in safe whitelist, skipping");
                            continue;
                        }

                        var startIndex = match.Index;
                        var descStart = Math.Max(0, startIndex - 100);
                        var description = text.Substring(descStart, startIndex - descStart).Trim();

                        suggestions.Add(new CommandSuggestion
                        {
                            Command = command,
                            Description = description,
                            RequiresConfirmation = false,  // All whitelisted commands are safe
                            Prerequisites = new List<string>()
                        });
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    WriteWarning("Regex timeout on command extraction");
                }
            }

            return suggestions.DistinctBy(s => s.Command).ToList();
        }

        private List<FileSuggestion> ExtractFileModificationsSecure(string text)
        {
            var suggestions = new List<FileSuggestion>();

            var patterns = new[]
            {
                @"(?:create|add)\s+(?:a\s+)?(?:new\s+)?file\s+(?:called\s+)?([^\s:]+)",
                @"(?:modify|update|change)\s+(?:the\s+)?file\s+([^\s:]+)"
                // Removed delete/remove patterns for security
            };

            foreach (var pattern in patterns)
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    var matches = regex.Matches(text);

                    foreach (Match match in matches)
                    {
                        var filePath = match.Groups[1].Value;

                        // Validate file path
                        if (!IsPathSafe(filePath))
                        {
                            WriteWarning($"Path '{filePath}' is not safe, skipping");
                            continue;
                        }

                        var operation = DetermineFileOperation(match.Value);

                        // Only allow create and modify, not delete
                        if (operation != FileOperation.Delete)
                        {
                            suggestions.Add(new FileSuggestion
                            {
                                FilePath = filePath,
                                Operation = operation,
                                Reason = ExtractFileOperationReason(text, match.Index)
                            });
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    WriteWarning("Regex timeout on file extraction");
                }
            }

            return suggestions;
        }

        private bool IsPathSafe(string path)
        {
            try
            {
                // Get current working directory
                var workingDir = SessionState.Path.CurrentFileSystemLocation.Path;
                var fullPath = Path.GetFullPath(Path.Combine(workingDir, path));

                // Must be under working directory (no path traversal)
                if (!fullPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Block system directories
                var blockedPaths = new[]
                {
                    "/etc", "/sys", "/proc", "/boot", "/dev",
                    "C:\\Windows", "C:\\Program Files", "C:\\Program Files (x86)",
                    "/System", "/Library", "/Applications"
                };

                foreach (var blocked in blockedPaths)
                {
                    if (fullPath.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Block hidden files/directories
                var filename = Path.GetFileName(fullPath);
                if (filename.StartsWith(".") && filename != ".gitignore" && filename != ".env.example")
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private SuggestionType DetermineSuggestionType(string context)
        {
            var lowerContext = context.ToLowerInvariant();

            if (lowerContext.Contains("replace") || lowerContext.Contains("change"))
                return SuggestionType.Replacement;
            if (lowerContext.Contains("add") || lowerContext.Contains("insert"))
                return SuggestionType.Insertion;

            return SuggestionType.NewCode;
        }

        private FileOperation DetermineFileOperation(string text)
        {
            var lower = text.ToLowerInvariant();
            if (lower.Contains("create") || lower.Contains("add")) return FileOperation.Create;
            if (lower.Contains("modify") || lower.Contains("update") || lower.Contains("change")) return FileOperation.Modify;
            return FileOperation.Modify;
        }

        private string ExtractFileOperationReason(string text, int index)
        {
            var start = Math.Max(0, index - 100);
            var length = Math.Min(200, text.Length - start);
            return text.Substring(start, length).Trim();
        }

        private string ExtractSummary(string text)
        {
            var summaryPatterns = new[]
            {
                @"(?:Summary|Overview|TL;DR):\s*([^\n]+(?:\n(?!\n)[^\n]+)*)",
                @"^([^\n]+)"
            };

            foreach (var pattern in summaryPatterns)
            {
                try
                {
                    var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                    var match = regex.Match(text);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
                catch (RegexMatchTimeoutException) { }
            }

            return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
        }

        private List<string> ExtractActionItems(string text)
        {
            var items = new List<string>();

            try
            {
                var numberedPattern = @"^\s*\d+\.\s+(.+)$";
                var regex = new Regex(numberedPattern, RegexOptions.Multiline, TimeSpan.FromSeconds(1));
                var numberedMatches = regex.Matches(text);
                items.AddRange(numberedMatches.Select(m => m.Groups[1].Value.Trim()));

                var bulletedPattern = @"^\s*[-*•]\s+(.+)$";
                regex = new Regex(bulletedPattern, RegexOptions.Multiline, TimeSpan.FromSeconds(1));
                var bulletedMatches = regex.Matches(text);
                items.AddRange(bulletedMatches.Select(m => m.Groups[1].Value.Trim()));
            }
            catch (RegexMatchTimeoutException)
            {
                WriteWarning("Regex timeout on action item extraction");
            }

            return items.Distinct().Take(20).ToList(); // Limit to 20 items
        }
    }

    /// <summary>
    /// Securely apply parsed AI suggestions with strict validation.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "AISuggestionsSecure")]
    [Alias("apply-ai-secure", "aiapplysecure")]
    public sealed class InvokeAISuggestionsSecureCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ParsedAIResponse Response { get; set; }

        [Parameter]
        public SwitchParameter ApplyCode { get; set; }

        [Parameter]
        public SwitchParameter ExecuteCommands { get; set; }

        [Parameter]
        public SwitchParameter ApplyFiles { get; set; }

        [Parameter]
        public SwitchParameter ApplyAll { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        private const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

        protected override void ProcessRecord()
        {
            if (ApplyCode || ApplyAll)
            {
                ApplyCodeSuggestionsSecure();
            }

            if (ExecuteCommands || ApplyAll)
            {
                ExecuteCommandSuggestionsSecure();
            }

            if (ApplyFiles || ApplyAll)
            {
                ApplyFileModificationsSecure();
            }
        }

        private void ApplyCodeSuggestionsSecure()
        {
            foreach (var suggestion in Response.CodeSuggestions)
            {
                WriteVerbose($"Processing code suggestion ({suggestion.Type}): {suggestion.Language}");

                if (WhatIf)
                {
                    WriteObject($"Would apply {suggestion.Type} to {suggestion.FilePath ?? "new file"}");
                    continue;
                }

                if (!Force && !ShouldContinue(
                    $"Apply {suggestion.Type} suggestion?",
                    $"File: {suggestion.FilePath}"))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(suggestion.FilePath))
                {
                    try
                    {
                        var workingDir = SessionState.Path.CurrentFileSystemLocation.Path;
                        var path = Path.GetFullPath(Path.Combine(workingDir, suggestion.FilePath));

                        // Validate path is safe
                        if (!path.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
                        {
                            WriteError(new ErrorRecord(
                                new SecurityException($"Path '{suggestion.FilePath}' is outside working directory"),
                                "PathTraversalAttempt",
                                ErrorCategory.SecurityError,
                                suggestion.FilePath));
                            continue;
                        }

                        // Size limit
                        if (suggestion.Code.Length > MAX_FILE_SIZE)
                        {
                            WriteWarning($"Code too large (>{MAX_FILE_SIZE / (1024 * 1024)}MB), skipping");
                            continue;
                        }

                        switch (suggestion.Type)
                        {
                            case SuggestionType.NewCode:
                                File.WriteAllText(path, suggestion.Code);
                                WriteObject($"Created file: {path}");
                                break;

                            case SuggestionType.Replacement:
                                File.WriteAllText(path, suggestion.Code);
                                WriteObject($"Replaced content in: {path}");
                                break;

                            case SuggestionType.Insertion:
                                var existingContent = File.Exists(path) ? File.ReadAllText(path) : "";
                                var newContent = existingContent + "\n" + suggestion.Code;
                                File.WriteAllText(path, newContent);
                                WriteObject($"Inserted code in: {path}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "ApplyCodeFailed", ErrorCategory.WriteError, suggestion.FilePath));
                    }
                }
            }
        }

        private void ExecuteCommandSuggestionsSecure()
        {
            foreach (var suggestion in Response.CommandSuggestions)
            {
                WriteVerbose($"Processing command: {suggestion.Command}");

                if (WhatIf)
                {
                    WriteObject($"Would execute: {suggestion.Command}");
                    continue;
                }

                if (!Force && !ShouldContinue(
                    $"Execute command?",
                    $"Command: {suggestion.Command}"))
                {
                    continue;
                }

                try
                {
                    var result = InvokeCommand.InvokeScript(suggestion.Command);
                    WriteObject($"Executed: {suggestion.Command}");

                    if (result.Any())
                    {
                        foreach (var item in result)
                        {
                            WriteObject(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "ExecuteCommandFailed", ErrorCategory.InvalidOperation, suggestion.Command));
                }
            }
        }

        private void ApplyFileModificationsSecure()
        {
            foreach (var suggestion in Response.FileSuggestions)
            {
                // Only allow create and modify, never delete
                if (suggestion.Operation == FileOperation.Delete)
                {
                    WriteWarning("Delete operations are not allowed for security reasons");
                    continue;
                }

                WriteVerbose($"Processing file operation: {suggestion.Operation} on {suggestion.FilePath}");

                if (WhatIf)
                {
                    WriteObject($"Would {suggestion.Operation.ToString().ToLower()} file: {suggestion.FilePath}");
                    continue;
                }

                if (!Force && !ShouldContinue(
                    $"Perform {suggestion.Operation} operation?",
                    $"File: {suggestion.FilePath}"))
                {
                    continue;
                }

                try
                {
                    var workingDir = SessionState.Path.CurrentFileSystemLocation.Path;
                    var path = Path.GetFullPath(Path.Combine(workingDir, suggestion.FilePath));

                    // Validate path
                    if (!path.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteError(new ErrorRecord(
                            new SecurityException($"Path '{suggestion.FilePath}' is outside working directory"),
                            "PathTraversalAttempt",
                            ErrorCategory.SecurityError,
                            suggestion.FilePath));
                        continue;
                    }

                    switch (suggestion.Operation)
                    {
                        case FileOperation.Create:
                            File.WriteAllText(path, suggestion.NewContent ?? string.Empty);
                            WriteObject($"Created file: {path}");
                            break;

                        case FileOperation.Modify:
                            if (File.Exists(path))
                            {
                                File.WriteAllText(path, suggestion.NewContent ?? File.ReadAllText(path));
                                WriteObject($"Modified file: {path}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "FileOperationFailed", ErrorCategory.WriteError, suggestion.FilePath));
                }
            }
        }
    }
}
