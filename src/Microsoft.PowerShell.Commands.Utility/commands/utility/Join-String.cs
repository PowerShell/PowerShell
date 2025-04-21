// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text;

namespace Microsoft.PowerShell.Commands.Utility
{
    /// <summary>
    /// Join-Object implementation.
    /// </summary>
    [Cmdlet(VerbsCommon.Join, "String", RemotingCapability = RemotingCapability.None, DefaultParameterSetName = "default")]
    [OutputType(typeof(string))]
    public sealed class JoinStringCommand : PSCmdlet
    {
        /// <summary>A bigger default to not get re-allocations in common use cases.</summary>
        private const int DefaultOutputStringCapacity = 256;

        private readonly StringBuilder _outputBuilder = new(DefaultOutputStringCapacity);
        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;
        private string _separator;
        private char _quoteChar;
        private bool _firstInputObject = true;

        /// <summary>
        /// Gets or sets the property name or script block to use as the value to join.
        /// </summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PropertyNameCompleter))]
        public PSPropertyExpression Property { get; set; }

        /// <summary>
        /// Gets or sets the delimiter to join the output with.
        /// </summary>
        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(SeparatorArgumentCompleter))]
        [AllowEmptyString]
        public string Separator
        {
            get => _separator ?? LanguagePrimitives.ConvertTo<string>(GetVariableValue("OFS"));
            set => _separator = value;
        }

        /// <summary>
        /// Gets or sets text to include before the joined input text.
        /// </summary>
        [Parameter]
        [Alias("op")]
        public string OutputPrefix { get; set; }

        /// <summary>
        /// Gets or sets text to include after the joined input text.
        /// </summary>
        [Parameter]
        [Alias("os")]
        public string OutputSuffix { get; set; }

        /// <summary>
        /// Gets or sets if the output items should we wrapped in single quotes.
        /// </summary>
        [Parameter(ParameterSetName = "SingleQuote")]
        public SwitchParameter SingleQuote { get; set; }

        /// <summary>
        /// Gets or sets if the output items should we wrapped in double quotes.
        /// </summary>
        [Parameter(ParameterSetName = "DoubleQuote")]
        public SwitchParameter DoubleQuote { get; set; }

        /// <summary>
        /// Gets or sets a format string that is applied to each input object.
        /// </summary>
        [Parameter(ParameterSetName = "Format")]
        [ArgumentCompleter(typeof(FormatStringArgumentCompleter))]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets if the current culture should be used with formatting instead of the invariant culture.
        /// </summary>
        [Parameter]
        public SwitchParameter UseCulture { get; set; }

        /// <summary>
        /// Gets or sets the input object to join into text.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; }

        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            _quoteChar = SingleQuote ? '\'' : DoubleQuote ? '"' : char.MinValue;
            _outputBuilder.Append(OutputPrefix);
            if (UseCulture)
            {
                _cultureInfo = CultureInfo.CurrentCulture;
            }
        }

        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            if (InputObject != null)
            {
                foreach (PSObject inputObject in InputObject)
                {
                    if (inputObject != null && inputObject != AutomationNull.Value)
                    {
                        var inputValue = Property == null
                                            ? inputObject
                                            : Property.GetValues(inputObject, false, true).FirstOrDefault()?.Result;

                        // conversion to string always succeeds.
                        if (!LanguagePrimitives.TryConvertTo<string>(inputValue, _cultureInfo, out var stringValue))
                        {
                            throw new PSInvalidCastException("InvalidCastFromAnyTypeToString", ExtendedTypeSystem.InvalidCastCannotRetrieveString, null);
                        }

                        if (_firstInputObject)
                        {
                            _firstInputObject = false;
                        }
                        else
                        {
                            _outputBuilder.Append(Separator);
                        }

                        if (_quoteChar != char.MinValue)
                        {
                            _outputBuilder.Append(_quoteChar);
                            _outputBuilder.Append(stringValue);
                            _outputBuilder.Append(_quoteChar);
                        }
                        else if (string.IsNullOrEmpty(FormatString))
                        {
                            _outputBuilder.Append(stringValue);
                        }
                        else
                        {
                            _outputBuilder.AppendFormat(_cultureInfo, FormatString, inputValue);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void EndProcessing()
        {
            _outputBuilder.Append(OutputSuffix);
            WriteObject(_outputBuilder.ToString());
        }
    }

    /// <summary>
    /// Provides completion for the Separator parameter of the Join-String cmdlet.
    /// </summary>
    public sealed class SeparatorArgumentCompleter : IArgumentCompleter
    {
        private const string NewLineText =
#if UNIX
        "`n";
#else
        "`r`n";
#endif

        private static readonly CompletionHelpers.CompletionDisplayInfoMapper SeparatorDisplayInfoMapper = separator => separator switch
        {
            "," => (
                ToolTip: TabCompletionStrings.SeparatorCommaToolTip,
                ListItemText: "Comma"),
            ", " => (
                ToolTip: TabCompletionStrings.SeparatorCommaSpaceToolTip,
                ListItemText: "Comma-Space"),
            ";" => (
                ToolTip: TabCompletionStrings.SeparatorSemiColonToolTip,
                ListItemText: "Semi-Colon"),
            "; " => (
                ToolTip: TabCompletionStrings.SeparatorSemiColonSpaceToolTip,
                ListItemText: "Semi-Colon-Space"),
            "-" => (
                ToolTip: TabCompletionStrings.SeparatorDashToolTip,
                ListItemText: "Dash"),
            " " => (
                ToolTip: TabCompletionStrings.SeparatorSpaceToolTip,
                ListItemText: "Space"),
            NewLineText => (
                ToolTip: StringUtil.Format(TabCompletionStrings.SeparatorNewlineToolTip, NewLineText),
                ListItemText: "Newline"),
            _ => (
                ToolTip: separator,
                ListItemText: separator),
        };

        private static readonly IReadOnlyList<string> s_separatorValues = new List<string>(capacity: 7)
        {
            ",",
            ", ",
            ";",
            "; ",
            NewLineText,
            "-",
            " ",
        };

        /// <summary>
        /// Returns completion results for Separator parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: s_separatorValues,
                    displayInfoMapper: SeparatorDisplayInfoMapper,
                    resultType: CompletionResultType.ParameterValue);
    }

    /// <summary>
    /// Provides completion for the FormatString parameter of the Join-String cmdlet.
    /// </summary>
    public sealed class FormatStringArgumentCompleter : IArgumentCompleter
    {
        private static readonly IReadOnlyList<string> s_formatStringValues = new List<string>(capacity: 4)
        {
            "[{0}]",
            "{0:N2}",
#if UNIX
            "`n    `${0}",
            "`n    [string] `${0}",
#else
            "`r`n    `${0}",
            "`r`n    [string] `${0}",
#endif
        };

        /// <summary>
        /// Returns completion results for FormatString parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: s_formatStringValues,
                    matchStrategy: CompletionHelpers.WildcardPatternEscapeMatch);
    }
}
