// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Development.AIContext
{
    /// <summary>
    /// Get-AIErrorContext cmdlet for analyzing errors with AI-friendly context.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AIErrorContext")]
    [OutputType(typeof(AIErrorContext))]
    public sealed class GetAIErrorContextCommand : PSCmdlet
    {
        /// <summary>
        /// Error record to analyze.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public ErrorRecord ErrorRecord { get; set; }

        /// <summary>
        /// Get error from $Error variable by index.
        /// </summary>
        [Parameter]
        [ValidateRange(0, int.MaxValue)]
        public int Index { get; set; } = 0;

        /// <summary>
        /// Analyze the last N errors.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 100)]
        public int Last { get; set; }

        /// <summary>
        /// Include detailed analysis.
        /// </summary>
        [Parameter]
        public SwitchParameter Detailed { get; set; }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ErrorRecord != null)
            {
                // Analyze provided error
                AnalyzeAndOutput(ErrorRecord);
            }
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (ErrorRecord == null)
            {
                // Get errors from $Error variable
                var errorVariable = SessionState.PSVariable.Get("Error");
                if (errorVariable?.Value is System.Collections.ArrayList errors && errors.Count > 0)
                {
                    if (Last > 0)
                    {
                        // Analyze last N errors
                        int count = Math.Min(Last, errors.Count);
                        for (int i = 0; i < count; i++)
                        {
                            if (errors[i] is ErrorRecord error)
                            {
                                AnalyzeAndOutput(error);
                            }
                        }
                    }
                    else
                    {
                        // Analyze error at specified index
                        if (Index < errors.Count)
                        {
                            if (errors[Index] is ErrorRecord error)
                            {
                                AnalyzeAndOutput(error);
                            }
                        }
                        else
                        {
                            WriteError(new ErrorRecord(
                                new IndexOutOfRangeException($"Error index {Index} is out of range. There are {errors.Count} errors in $Error."),
                                "ErrorIndexOutOfRange",
                                ErrorCategory.InvalidArgument,
                                Index));
                        }
                    }
                }
                else
                {
                    WriteVerbose("No errors found in $Error variable");
                }
            }
        }

        private void AnalyzeAndOutput(ErrorRecord error)
        {
            try
            {
                var context = ErrorAnalyzer.Analyze(error);

                if (context != null)
                {
                    if (Detailed.IsPresent)
                    {
                        WriteVerbose("=== AI Error Analysis ===");
                        WriteVerbose($"Error ID: {context.ErrorId}");
                        WriteVerbose($"Category: {context.Category}");
                        WriteVerbose($"Severity: {context.Severity}");

                        if (!string.IsNullOrEmpty(context.File))
                        {
                            WriteVerbose($"Location: {context.File}:{context.Line}:{context.Column}");
                        }

                        if (!string.IsNullOrEmpty(context.RootCause))
                        {
                            WriteVerbose($"Root Cause: {context.RootCause}");
                        }

                        if (context.SuggestedFixes.Count > 0)
                        {
                            WriteVerbose("Suggested Fixes:");
                            foreach (var fix in context.SuggestedFixes)
                            {
                                WriteVerbose($"  - {fix}");
                            }
                        }

                        if (context.DocumentationLinks.Count > 0)
                        {
                            WriteVerbose("Documentation:");
                            foreach (var link in context.DocumentationLinks)
                            {
                                WriteVerbose($"  - {link}");
                            }
                        }
                    }

                    WriteObject(context);
                }
                else
                {
                    WriteWarning("Unable to analyze error");
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "ErrorAnalysisFailed",
                    ErrorCategory.InvalidOperation,
                    error));
            }
        }
    }
}
