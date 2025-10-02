// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Get-Clipboard' cmdlet.
    /// This cmdlet get the content from system clipboard.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Clipboard", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2109905")]
    [Alias("gcb")]
    [OutputType(typeof(string))]
    public class GetClipboardCommand : PSCmdlet
    {
        /// <summary>
        /// Property that sets raw parameter. This will allow clipboard return text or file list as one string.
        /// </summary>
        [Parameter]
        public SwitchParameter Raw
        {
            get
            {
                return _raw;
            }

            set
            {
                _raw = value;
            }
        }

        /// <summary>
        /// The delimiters to use when splitting the clipboard content.
        /// </summary>
        [Parameter]
        [ArgumentCompleter(typeof(DelimiterCompleter))]
        public string[] Delimiter { get; set; } = [Environment.NewLine];

        private bool _raw;

        /// <summary>
        /// This method implements the ProcessRecord method for Get-Clipboard command.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.WriteObject(GetClipboardContentAsText(), true);
        }

        /// <summary>
        /// Returns the clipboard content as text format.
        /// </summary>
        /// <returns>Array of strings representing content from clipboard.</returns>
        private List<string> GetClipboardContentAsText()
        {
            var result = new List<string>();
            string textContent = null;

            try
            {
                textContent = Clipboard.GetText();
            }
            catch (PlatformNotSupportedException)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ClipboardResources.UnsupportedPlatform), "FailedToGetClipboardUnsupportedPlatform", ErrorCategory.InvalidOperation, "Clipboard"));
            }

            if (_raw)
            {
                result.Add(textContent);
            }
            else
            {
                result.AddRange(textContent.Split(Delimiter, StringSplitOptions.None));
            }

            return result;
        }
    }

    /// <summary>
    /// Provides argument completion for the Delimiter parameter.
    /// </summary>
    public sealed class DelimiterCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Provides argument completion for the Delimiter parameter.
        /// </summary>
        /// <param name="commandName">The name of the command that is being completed.</param>
        /// <param name="parameterName">The name of the parameter that is being completed.</param>
        /// <param name="wordToComplete">The input text to filter the results by.</param>
        /// <param name="commandAst">The ast of the command that triggered the completion.</param>
        /// <param name="fakeBoundParameters">The parameters bound to the command.</param>
        /// <returns>Completion results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            wordToComplete ??= string.Empty;
            var pattern = new WildcardPattern(wordToComplete + '*', WildcardOptions.IgnoreCase);
            if (pattern.IsMatch("CRLF") || pattern.IsMatch("Windows"))
            {
                yield return new CompletionResult("\"`r`n\"", "CRLF", CompletionResultType.ParameterValue, "Windows (CRLF)");
            }

            if (pattern.IsMatch("LF") || pattern.IsMatch("Unix") || pattern.IsMatch("Linux"))
            {
                yield return new CompletionResult("\"`n\"", "LF", CompletionResultType.ParameterValue, "UNIX (LF)");
            }
        }
    }
}
