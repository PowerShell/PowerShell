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
        [ArgumentCompleter(typeof(JoinItemCompleter))]
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
        [ArgumentCompleter(typeof(JoinItemCompleter))]
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

    [SuppressMessage(
        "Microsoft.Performance",
        "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "Class is instantiated through late-bound reflection")]
    internal class JoinItemCompleter : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            switch (parameterName)
            {
                case "Separator": return CompleteSeparator(wordToComplete);
                case "FormatString": return CompleteFormatString(wordToComplete);
            }

            return null;
        }

        private static IEnumerable<CompletionResult> CompleteFormatString(string wordToComplete)
        {
            var res = new List<CompletionResult>();
            void AddMatching(string completionText)
            {
                if (completionText.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                {
                    res.Add(new CompletionResult(completionText));
                }
            }

            AddMatching("'[{0}]'");
            AddMatching("'{0:N2}'");
            AddMatching("\"`r`n    `${0}\"");
            AddMatching("\"`r`n    [string] `${0}\"");

            return res;
        }

        private IEnumerable<CompletionResult> CompleteSeparator(string wordToComplete)
        {
            var res = new List<CompletionResult>(10);

            void AddMatching(string completionText, string listText, string toolTip)
            {
                if (completionText.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                {
                    res.Add(new CompletionResult(completionText, listText, CompletionResultType.ParameterValue, toolTip));
                }
            }

            AddMatching("', '", "Comma-Space", "', ' - Comma-Space");
            AddMatching("';'", "Semi-Colon", "';'  - Semi-Colon ");
            AddMatching("'; '", "Semi-Colon-Space", "'; ' - Semi-Colon-Space");
            AddMatching($"\"{NewLineText}\"", "Newline", $"{NewLineText} - Newline");
            AddMatching("','", "Comma", "','  - Comma");
            AddMatching("'-'", "Dash", "'-'  - Dash");
            AddMatching("' '", "Space", "' '  - Space");
            return res;
        }

        public string NewLineText
        {
            get
            {
#if UNIX
                return "`n";
#else
                return "`r`n";
#endif
            }
        }
    }
}
