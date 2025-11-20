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
    /// Parsed AI response with extracted code suggestions.
    /// </summary>
    public class ParsedAIResponse
    {
        public string OriginalResponse { get; set; }
        public List<CodeSuggestion> CodeSuggestions { get; set; }
        public List<CommandSuggestion> CommandSuggestions { get; set; }
        public List<FileSuggestion> FileSuggestions { get; set; }
        public string Summary { get; set; }
        public List<string> ActionItems { get; set; }

        public ParsedAIResponse()
        {
            CodeSuggestions = new List<CodeSuggestion>();
            CommandSuggestions = new List<CommandSuggestion>();
            FileSuggestions = new List<FileSuggestion>();
            ActionItems = new List<string>();
        }
    }

    /// <summary>
    /// Code suggestion from AI.
    /// </summary>
    public class CodeSuggestion
    {
        public string Language { get; set; }
        public string Code { get; set; }
        public string Context { get; set; }
        public string FilePath { get; set; }
        public int? LineNumber { get; set; }
        public SuggestionType Type { get; set; }
    }

    /// <summary>
    /// Command suggestion from AI.
    /// </summary>
    public class CommandSuggestion
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public bool RequiresConfirmation { get; set; }
        public List<string> Prerequisites { get; set; }

        public CommandSuggestion()
        {
            Prerequisites = new List<string>();
        }
    }

    /// <summary>
    /// File modification suggestion from AI.
    /// </summary>
    public class FileSuggestion
    {
        public string FilePath { get; set; }
        public FileOperation Operation { get; set; }
        public string OldContent { get; set; }
        public string NewContent { get; set; }
        public string Reason { get; set; }
    }

    public enum SuggestionType
    {
        NewCode,
        Replacement,
        Insertion,
        Deletion
    }

    public enum FileOperation
    {
        Create,
        Modify,
        Delete,
        Rename
    }

    /// <summary>
    /// Parses AI responses and extracts actionable suggestions.
    /// </summary>
    [Cmdlet(VerbsData.Convert, "AIResponse")]
    [Alias("parse-ai", "aiparse")]
    [OutputType(typeof(ParsedAIResponse))]
    public sealed class ConvertAIResponseCommand : PSCmdlet
    {
        /// <summary>
        /// AI response text to parse.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Response { get; set; }

        /// <summary>
        /// Path to file containing AI response.
        /// </summary>
        [Parameter]
        public string FilePath { get; set; }

        /// <summary>
        /// Extract code blocks.
        /// </summary>
        [Parameter]
        public SwitchParameter ExtractCode { get; set; }

        /// <summary>
        /// Extract commands.
        /// </summary>
        [Parameter]
        public SwitchParameter ExtractCommands { get; set; }

        /// <summary>
        /// Extract file suggestions.
        /// </summary>
        [Parameter]
        public SwitchParameter ExtractFiles { get; set; }

        /// <summary>
        /// Extract all suggestions.
        /// </summary>
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

            // Extract code blocks
            if (ExtractCode || All)
            {
                parsed.CodeSuggestions = ExtractCodeBlocks(Response);
            }

            // Extract commands
            if (ExtractCommands || All)
            {
                parsed.CommandSuggestions = ExtractCommands(Response);
            }

            // Extract file suggestions
            if (ExtractFiles || All)
            {
                parsed.FileSuggestions = ExtractFileModifications(Response);
            }

            // Extract summary and action items
            parsed.Summary = ExtractSummary(Response);
            parsed.ActionItems = ExtractActionItems(Response);

            WriteObject(parsed);
        }

        private List<CodeSuggestion> ExtractCodeBlocks(string text)
        {
            var suggestions = new List<CodeSuggestion>();

            // Match fenced code blocks: ```language\ncode\n```
            var pattern = @"```(\w+)?\s*\n(.*?)```";
            var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var language = match.Groups[1].Success ? match.Groups[1].Value : "text";
                var code = match.Groups[2].Value.Trim();

                // Extract context (text before code block)
                var startIndex = match.Index;
                var contextStart = Math.Max(0, startIndex - 200);
                var context = text.Substring(contextStart, startIndex - contextStart).Trim();

                // Try to extract file path and line number from context
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

                // Determine suggestion type
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

            return suggestions;
        }

        private SuggestionType DetermineSuggestionType(string context)
        {
            var lowerContext = context.ToLowerInvariant();

            if (lowerContext.Contains("replace") || lowerContext.Contains("change"))
                return SuggestionType.Replacement;
            if (lowerContext.Contains("add") || lowerContext.Contains("insert"))
                return SuggestionType.Insertion;
            if (lowerContext.Contains("delete") || lowerContext.Contains("remove"))
                return SuggestionType.Deletion;

            return SuggestionType.NewCode;
        }

        private List<CommandSuggestion> ExtractCommands(string text)
        {
            var suggestions = new List<CommandSuggestion>();

            // Match shell commands in various formats
            var patterns = new[]
            {
                @"(?:run|execute|type):\s*`([^`]+)`",                    // Run: `command`
                @"\$\s*([^\n]+)",                                         // $ command
                @"(?:Run|Execute|Try):\s*([^\n]+)",                      // Run: command
                @"```(?:bash|sh|powershell|pwsh)\s*\n([^`]+)```"        // Code blocks
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var command = match.Groups[1].Value.Trim();
                    if (string.IsNullOrWhiteSpace(command)) continue;

                    // Extract description (text before command)
                    var startIndex = match.Index;
                    var descStart = Math.Max(0, startIndex - 100);
                    var description = text.Substring(descStart, startIndex - descStart).Trim();

                    // Determine if confirmation needed
                    var requiresConfirmation = IsDestructiveCommand(command);

                    suggestions.Add(new CommandSuggestion
                    {
                        Command = command,
                        Description = description,
                        RequiresConfirmation = requiresConfirmation
                    });
                }
            }

            return suggestions.DistinctBy(s => s.Command).ToList();
        }

        private bool IsDestructiveCommand(string command)
        {
            var destructiveKeywords = new[]
            {
                "rm ", "delete ", "remove ", "drop ", "truncate ",
                "format ", "erase ", "destroy ", "kill ", "force"
            };

            return destructiveKeywords.Any(keyword =>
                command.ToLowerInvariant().Contains(keyword));
        }

        private List<FileSuggestion> ExtractFileModifications(string text)
        {
            var suggestions = new List<FileSuggestion>();

            // Match file operation suggestions
            var patterns = new[]
            {
                @"(?:create|add)\s+(?:a\s+)?(?:new\s+)?file\s+(?:called\s+)?([^\s:]+)",
                @"(?:modify|update|change)\s+(?:the\s+)?file\s+([^\s:]+)",
                @"(?:delete|remove)\s+(?:the\s+)?file\s+([^\s:]+)",
                @"(?:rename)\s+([^\s]+)\s+to\s+([^\s]+)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var operation = DetermineFileOperation(match.Value);
                    var filePath = match.Groups[1].Value;

                    suggestions.Add(new FileSuggestion
                    {
                        FilePath = filePath,
                        Operation = operation,
                        Reason = ExtractFileOperationReason(text, match.Index)
                    });
                }
            }

            return suggestions;
        }

        private FileOperation DetermineFileOperation(string text)
        {
            var lower = text.ToLowerInvariant();
            if (lower.Contains("create") || lower.Contains("add")) return FileOperation.Create;
            if (lower.Contains("modify") || lower.Contains("update") || lower.Contains("change")) return FileOperation.Modify;
            if (lower.Contains("delete") || lower.Contains("remove")) return FileOperation.Delete;
            if (lower.Contains("rename")) return FileOperation.Rename;
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
            // Try to find summary section
            var summaryPatterns = new[]
            {
                @"(?:Summary|Overview|TL;DR):\s*([^\n]+(?:\n(?!\n)[^\n]+)*)",
                @"^([^\n]+)",  // First line as fallback
            };

            foreach (var pattern in summaryPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            // Fallback: first 200 characters
            return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
        }

        private List<string> ExtractActionItems(string text)
        {
            var items = new List<string>();

            // Match numbered lists
            var numberedPattern = @"^\s*\d+\.\s+(.+)$";
            var numberedMatches = Regex.Matches(text, numberedPattern, RegexOptions.Multiline);
            items.AddRange(numberedMatches.Select(m => m.Groups[1].Value.Trim()));

            // Match bulleted lists
            var bulletedPattern = @"^\s*[-*•]\s+(.+)$";
            var bulletedMatches = Regex.Matches(text, bulletedPattern, RegexOptions.Multiline);
            items.AddRange(bulletedMatches.Select(m => m.Groups[1].Value.Trim()));

            // Match "you should", "I recommend", etc.
            var recommendationPattern = @"(?:you should|i recommend|try to|consider|make sure to)\s+([^.!?\n]+)";
            var recommendationMatches = Regex.Matches(text, recommendationPattern, RegexOptions.IgnoreCase);
            items.AddRange(recommendationMatches.Select(m => m.Groups[1].Value.Trim()));

            return items.Distinct().ToList();
        }
    }

    /// <summary>
    /// Applies parsed AI suggestions to files and executes commands.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "AISuggestions")]
    [Alias("apply-ai", "aiapply")]
    public sealed class InvokeAISuggestionsCommand : PSCmdlet
    {
        /// <summary>
        /// Parsed AI response.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ParsedAIResponse Response { get; set; }

        /// <summary>
        /// Apply code suggestions.
        /// </summary>
        [Parameter]
        public SwitchParameter ApplyCode { get; set; }

        /// <summary>
        /// Execute commands.
        /// </summary>
        [Parameter]
        public SwitchParameter ExecuteCommands { get; set; }

        /// <summary>
        /// Apply file modifications.
        /// </summary>
        [Parameter]
        public SwitchParameter ApplyFiles { get; set; }

        /// <summary>
        /// Apply all suggestions.
        /// </summary>
        [Parameter]
        public SwitchParameter ApplyAll { get; set; }

        /// <summary>
        /// Skip confirmation prompts.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Preview changes without applying.
        /// </summary>
        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        protected override void ProcessRecord()
        {
            if (ApplyCode || ApplyAll)
            {
                ApplyCodeSuggestions();
            }

            if (ExecuteCommands || ApplyAll)
            {
                ExecuteCommandSuggestions();
            }

            if (ApplyFiles || ApplyAll)
            {
                ApplyFileModifications();
            }
        }

        private void ApplyCodeSuggestions()
        {
            foreach (var suggestion in Response.CodeSuggestions)
            {
                WriteVerbose($"Processing code suggestion ({suggestion.Type}): {suggestion.Language}");

                if (WhatIf)
                {
                    WriteObject($"Would apply {suggestion.Type} to {suggestion.FilePath ?? "new file"}");
                    WriteObject($"Code:\n{suggestion.Code}");
                    continue;
                }

                if (!Force && !ShouldContinue(
                    $"Apply {suggestion.Type} suggestion?",
                    $"Code: {suggestion.Code.Substring(0, Math.Min(50, suggestion.Code.Length))}..."))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(suggestion.FilePath))
                {
                    // Apply to specific file
                    try
                    {
                        var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(suggestion.FilePath);

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
                                var existingContent = File.ReadAllText(path);
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
                else
                {
                    // Output code to pipeline
                    WriteObject(suggestion.Code);
                }
            }
        }

        private void ExecuteCommandSuggestions()
        {
            foreach (var suggestion in Response.CommandSuggestions)
            {
                WriteVerbose($"Processing command: {suggestion.Command}");

                if (WhatIf)
                {
                    WriteObject($"Would execute: {suggestion.Command}");
                    continue;
                }

                if (suggestion.RequiresConfirmation && !Force)
                {
                    if (!ShouldContinue(
                        $"Execute potentially destructive command?",
                        $"Command: {suggestion.Command}"))
                    {
                        continue;
                    }
                }

                try
                {
                    var result = InvokeCommand.InvokeScript(suggestion.Command);
                    WriteObject($"Executed: {suggestion.Command}");

                    if (result.Any())
                    {
                        WriteObject("Output:");
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

        private void ApplyFileModifications()
        {
            foreach (var suggestion in Response.FileSuggestions)
            {
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
                    var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(suggestion.FilePath);

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

                        case FileOperation.Delete:
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                WriteObject($"Deleted file: {path}");
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
