// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json;

namespace Microsoft.PowerShell.Development.AIIntegration
{
    /// <summary>
    /// Represents an AI prompt.
    /// </summary>
    public class AIPrompt
    {
        public string Template { get; set; }
        public string RenderedPrompt { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public DateTime CreatedAt { get; set; }
        public long EstimatedTokens { get; set; }

        public AIPrompt()
        {
            Context = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Builds structured prompts for AI assistants.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "AIPrompt")]
    [OutputType(typeof(AIPrompt))]
    [Alias("prompt", "aiprompt")]
    public sealed class NewAIPromptCommand : PSCmdlet
    {
        /// <summary>
        /// Template for the prompt.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateSet("Error", "CodeReview", "Debug", "Explain", "Refactor", "Test", "Deploy", "Custom")]
        public string Template { get; set; }

        /// <summary>
        /// Custom prompt text (for Custom template).
        /// </summary>
        [Parameter]
        public string CustomPrompt { get; set; }

        /// <summary>
        /// Include terminal snapshot.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeSnapshot { get; set; }

        /// <summary>
        /// Include code context.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeCode { get; set; }

        /// <summary>
        /// Include recent errors.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeErrors { get; set; }

        /// <summary>
        /// Include project context.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeProject { get; set; }

        /// <summary>
        /// Include all context.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeAll { get; set; }

        /// <summary>
        /// Specific files to include.
        /// </summary>
        [Parameter]
        public string[] Files { get; set; }

        /// <summary>
        /// Additional context to include.
        /// </summary>
        [Parameter]
        public hashtable AdditionalContext { get; set; }

        /// <summary>
        /// Output format.
        /// </summary>
        [Parameter]
        [ValidateSet("Text", "Markdown", "Json")]
        public string OutputFormat { get; set; } = "Markdown";

        /// <summary>
        /// Save to file.
        /// </summary>
        [Parameter]
        public string OutputFile { get; set; }

        /// <summary>
        /// Copy to clipboard.
        /// </summary>
        [Parameter]
        public SwitchParameter ToClipboard { get; set; }

        protected override void ProcessRecord()
        {
            var prompt = new AIPrompt
            {
                Template = Template,
                CreatedAt = DateTime.Now
            };

            // Gather context
            if (IncludeSnapshot || IncludeAll)
            {
                prompt.Context["Snapshot"] = GetTerminalSnapshot();
            }

            if (IncludeCode || IncludeAll)
            {
                prompt.Context["Code"] = GetCodeContext();
            }

            if (IncludeErrors || IncludeAll)
            {
                prompt.Context["Errors"] = GetErrorContext();
            }

            if (IncludeProject || IncludeAll)
            {
                prompt.Context["Project"] = GetProjectContext();
            }

            if (Files != null && Files.Length > 0)
            {
                prompt.Context["SpecificFiles"] = GetSpecificFiles();
            }

            if (AdditionalContext != null)
            {
                foreach (var key in AdditionalContext.Keys)
                {
                    prompt.Context[key.ToString()] = AdditionalContext[key];
                }
            }

            // Render prompt
            prompt.RenderedPrompt = RenderPrompt(prompt);

            // Estimate tokens (rough estimate: 1 token ≈ 4 characters)
            prompt.EstimatedTokens = prompt.RenderedPrompt.Length / 4;

            // Output
            if (!string.IsNullOrEmpty(OutputFile))
            {
                SaveToFile(prompt);
            }

            if (ToClipboard)
            {
                CopyToClipboard(prompt.RenderedPrompt);
            }

            WriteObject(prompt);
        }

        private object GetTerminalSnapshot()
        {
            try
            {
                var result = InvokeCommand.InvokeScript(
                    "Get-TerminalSnapshot -All"
                );

                return result.FirstOrDefault()?.BaseObject;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get terminal snapshot: {ex.Message}");
                return null;
            }
        }

        private object GetCodeContext()
        {
            try
            {
                var script = Files != null && Files.Length > 0
                    ? $"Get-CodeContext -Files {string.Join(",", Files.Select(f => $"'{f}'"))} -IncludeContent -IncludeMetrics"
                    : "Get-CodeContext -RecentlyModified -Hours 24 -IncludeContent -IncludeMetrics";

                var result = InvokeCommand.InvokeScript(script);
                return result.FirstOrDefault()?.BaseObject;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get code context: {ex.Message}");
                return null;
            }
        }

        private object GetErrorContext()
        {
            try
            {
                var result = InvokeCommand.InvokeScript(
                    "Get-AIErrorContext -Last 10"
                );

                return result.Select(r => r.BaseObject).ToList();
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get error context: {ex.Message}");
                return null;
            }
        }

        private object GetProjectContext()
        {
            try
            {
                var result = InvokeCommand.InvokeScript(
                    "Get-ProjectContext"
                );

                return result.FirstOrDefault()?.BaseObject;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get project context: {ex.Message}");
                return null;
            }
        }

        private List<object> GetSpecificFiles()
        {
            var files = new List<object>();

            foreach (var file in Files)
            {
                try
                {
                    var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(file);
                    if (File.Exists(path))
                    {
                        files.Add(new
                        {
                            Path = path,
                            Content = File.ReadAllText(path),
                            Size = new FileInfo(path).Length
                        });
                    }
                }
                catch (Exception ex)
                {
                    WriteWarning($"Failed to read file {file}: {ex.Message}");
                }
            }

            return files;
        }

        private string RenderPrompt(AIPrompt prompt)
        {
            var sb = new StringBuilder();

            switch (Template)
            {
                case "Error":
                    RenderErrorPrompt(sb, prompt);
                    break;
                case "CodeReview":
                    RenderCodeReviewPrompt(sb, prompt);
                    break;
                case "Debug":
                    RenderDebugPrompt(sb, prompt);
                    break;
                case "Explain":
                    RenderExplainPrompt(sb, prompt);
                    break;
                case "Refactor":
                    RenderRefactorPrompt(sb, prompt);
                    break;
                case "Test":
                    RenderTestPrompt(sb, prompt);
                    break;
                case "Deploy":
                    RenderDeployPrompt(sb, prompt);
                    break;
                case "Custom":
                    RenderCustomPrompt(sb, prompt);
                    break;
            }

            return sb.ToString();
        }

        private void RenderErrorPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Error Analysis Request");
                sb.AppendLine();
                sb.AppendLine("I'm encountering errors in my development environment. Please analyze the following context and help me resolve them.");
                sb.AppendLine();

                if (prompt.Context.ContainsKey("Errors"))
                {
                    sb.AppendLine("## Recent Errors");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine(JsonSerializer.Serialize(prompt.Context["Errors"], new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (prompt.Context.ContainsKey("Snapshot"))
                {
                    sb.AppendLine("## Terminal State");
                    sb.AppendLine();
                    sb.AppendLine("```json");
                    sb.AppendLine(JsonSerializer.Serialize(prompt.Context["Snapshot"], new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                if (prompt.Context.ContainsKey("Code"))
                {
                    sb.AppendLine("## Related Code");
                    sb.AppendLine();
                    sb.AppendLine("```json");
                    sb.AppendLine(JsonSerializer.Serialize(prompt.Context["Code"], new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                sb.AppendLine("## Request");
                sb.AppendLine();
                sb.AppendLine("Please:");
                sb.AppendLine("1. Analyze the errors and identify root causes");
                sb.AppendLine("2. Suggest specific fixes");
                sb.AppendLine("3. Provide code examples if applicable");
                sb.AppendLine("4. Explain why the error occurred");
            }
            else if (OutputFormat == "Json")
            {
                sb.AppendLine(JsonSerializer.Serialize(prompt.Context, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                sb.AppendLine("ERROR ANALYSIS REQUEST");
                sb.AppendLine("======================");
                sb.AppendLine();
                sb.AppendLine("I'm encountering errors. Please analyze the following context:");
                sb.AppendLine();
                sb.AppendLine(JsonSerializer.Serialize(prompt.Context, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        private void RenderCodeReviewPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Code Review Request");
                sb.AppendLine();
                sb.AppendLine("Please review the following code for:");
                sb.AppendLine("- Code quality and best practices");
                sb.AppendLine("- Potential bugs or issues");
                sb.AppendLine("- Performance improvements");
                sb.AppendLine("- Security vulnerabilities");
                sb.AppendLine("- Maintainability concerns");
                sb.AppendLine();

                if (prompt.Context.ContainsKey("Code"))
                {
                    sb.AppendLine("## Code to Review");
                    sb.AppendLine();
                    sb.AppendLine("```json");
                    sb.AppendLine(JsonSerializer.Serialize(prompt.Context["Code"], new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("```");
                }

                if (prompt.Context.ContainsKey("Project"))
                {
                    sb.AppendLine();
                    sb.AppendLine("## Project Context");
                    sb.AppendLine();
                    sb.AppendLine("```json");
                    sb.AppendLine(JsonSerializer.Serialize(prompt.Context["Project"], new JsonSerializerOptions { WriteIndented = true }));
                    sb.AppendLine("```");
                }
            }
            else if (OutputFormat == "Json")
            {
                sb.AppendLine(JsonSerializer.Serialize(prompt.Context, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                sb.AppendLine("CODE REVIEW REQUEST");
                sb.AppendLine("===================");
                sb.AppendLine();
                sb.AppendLine(JsonSerializer.Serialize(prompt.Context, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        private void RenderDebugPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Debugging Assistance Request");
                sb.AppendLine();
                sb.AppendLine("I need help debugging an issue. Here's the context:");
                sb.AppendLine();

                AppendAllContext(sb, prompt);

                sb.AppendLine();
                sb.AppendLine("## What I Need");
                sb.AppendLine();
                sb.AppendLine("1. Help identifying the root cause");
                sb.AppendLine("2. Step-by-step debugging approach");
                sb.AppendLine("3. Specific areas to investigate");
                sb.AppendLine("4. Potential solutions");
            }
            else
            {
                sb.AppendLine("DEBUGGING ASSISTANCE");
                sb.AppendLine("===================");
                sb.AppendLine();
                AppendAllContext(sb, prompt);
            }
        }

        private void RenderExplainPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Code Explanation Request");
                sb.AppendLine();
                sb.AppendLine("Please explain the following code:");
                sb.AppendLine();
                sb.AppendLine("- What it does");
                sb.AppendLine("- How it works");
                sb.AppendLine("- Key design decisions");
                sb.AppendLine("- Potential improvements");
                sb.AppendLine();

                AppendAllContext(sb, prompt);
            }
            else
            {
                sb.AppendLine("CODE EXPLANATION REQUEST");
                sb.AppendLine("=======================");
                sb.AppendLine();
                AppendAllContext(sb, prompt);
            }
        }

        private void RenderRefactorPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Refactoring Request");
                sb.AppendLine();
                sb.AppendLine("Please help refactor the following code to:");
                sb.AppendLine("- Improve readability");
                sb.AppendLine("- Enhance maintainability");
                sb.AppendLine("- Follow best practices");
                sb.AppendLine("- Optimize performance");
                sb.AppendLine();

                AppendAllContext(sb, prompt);

                sb.AppendLine();
                sb.AppendLine("## Deliverables");
                sb.AppendLine();
                sb.AppendLine("1. Refactored code");
                sb.AppendLine("2. Explanation of changes");
                sb.AppendLine("3. Benefits of the refactoring");
            }
            else
            {
                sb.AppendLine("REFACTORING REQUEST");
                sb.AppendLine("==================");
                sb.AppendLine();
                AppendAllContext(sb, prompt);
            }
        }

        private void RenderTestPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Test Generation Request");
                sb.AppendLine();
                sb.AppendLine("Please generate comprehensive tests for the following code:");
                sb.AppendLine();

                AppendAllContext(sb, prompt);

                sb.AppendLine();
                sb.AppendLine("## Requirements");
                sb.AppendLine();
                sb.AppendLine("1. Unit tests for all public methods");
                sb.AppendLine("2. Edge case coverage");
                sb.AppendLine("3. Negative test cases");
                sb.AppendLine("4. Integration tests if applicable");
            }
            else
            {
                sb.AppendLine("TEST GENERATION REQUEST");
                sb.AppendLine("======================");
                sb.AppendLine();
                AppendAllContext(sb, prompt);
            }
        }

        private void RenderDeployPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (OutputFormat == "Markdown")
            {
                sb.AppendLine("# Deployment Assistance Request");
                sb.AppendLine();
                sb.AppendLine("I need help with deployment. Here's the context:");
                sb.AppendLine();

                AppendAllContext(sb, prompt);

                sb.AppendLine();
                sb.AppendLine("## What I Need");
                sb.AppendLine();
                sb.AppendLine("1. Deployment strategy recommendations");
                sb.AppendLine("2. Potential issues to watch for");
                sb.AppendLine("3. Rollback plan");
                sb.AppendLine("4. Post-deployment verification steps");
            }
            else
            {
                sb.AppendLine("DEPLOYMENT ASSISTANCE");
                sb.AppendLine("====================");
                sb.AppendLine();
                AppendAllContext(sb, prompt);
            }
        }

        private void RenderCustomPrompt(StringBuilder sb, AIPrompt prompt)
        {
            if (!string.IsNullOrEmpty(CustomPrompt))
            {
                sb.AppendLine(CustomPrompt);
                sb.AppendLine();
            }

            sb.AppendLine("## Context");
            sb.AppendLine();
            AppendAllContext(sb, prompt);
        }

        private void AppendAllContext(StringBuilder sb, AIPrompt prompt)
        {
            foreach (var kvp in prompt.Context)
            {
                sb.AppendLine($"### {kvp.Key}");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(JsonSerializer.Serialize(kvp.Value, new JsonSerializerOptions { WriteIndented = true }));
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        private void SaveToFile(AIPrompt prompt)
        {
            try
            {
                var path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(OutputFile);
                File.WriteAllText(path, prompt.RenderedPrompt);
                WriteVerbose($"Prompt saved to: {path}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "SaveFileFailed",
                    ErrorCategory.WriteError,
                    OutputFile));
            }
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                // Use PowerShell's Set-Clipboard if available
                InvokeCommand.InvokeScript($"Set-Clipboard -Value @'\n{text}\n'@");
                WriteVerbose("Prompt copied to clipboard");
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to copy to clipboard: {ex.Message}");
            }
        }
    }
}
