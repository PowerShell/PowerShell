﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
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
        private const int Capacity = 50;

        // ReSharper disable once CollectionNeverQueried.Local
        private readonly List<PSObject> _inputObjects = new List<PSObject>(Capacity);
        private DynamicPropertyGetter _propGetter = new DynamicPropertyGetter();

        /// <summary>
        /// Gets or sets the property name or script block to use as the value to join.
        /// </summary>
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PropertyNameCompleter))]
        public object Property { get; set; }

        /// <summary>
        /// Gets or sets the delimiter to join the output with.
        /// </summary>
        [Parameter(Position = 1)]
        [ArgumentCompleter(typeof(JoinItemCompleter))]
        [AllowEmptyString]
        public string Delimiter { get; set; }

        /// <summary>
        /// Gets or sets text to include before the joined input text.
        /// </summary>
        [Parameter]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets text to include after the joined input text.
        /// </summary>
        [Parameter]
        public string Suffix { get; set; }

        /// <summary>
        /// Gets or sets if the output items should we wrapped in single quotes.
        /// </summary>
        [Parameter(ParameterSetName = "Quote")]
        public SwitchParameter Quote { get; set; }

        /// <summary>
        /// Gets or sets if the output items should we wrapped in double quotes.
        /// </summary>
        [Parameter(ParameterSetName = "DoubleQuote")]
        public SwitchParameter DoubleQuote { get; set; }

        /// <summary>
        /// Gets or sets the input object to join into text.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public PSObject InputObject { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            _inputObjects.Add(InputObject);
        }

        /// <inheritdoc />
        protected override void EndProcessing()
        {
            var quoteChar = Quote ? '\'' : DoubleQuote ? '"' : char.MinValue;

            const int defaultOutputStringCapacity = 256;
            var builder = new StringBuilder(defaultOutputStringCapacity);
            builder.Append(Prefix);

            if (Delimiter == null)
            {
                Delimiter = LanguagePrimitives.ConvertTo<string>(GetVariableValue("OFS"));
            }


            void AppendValue(string val)
            {
                if (quoteChar != char.MinValue)
                {
                    builder.Append(quoteChar).Append(val).Append(quoteChar);
                }
                else
                {
                    builder.Append(val);
                }
            }

            if (_inputObjects.Count == 0)
            {
                return;
            }

            if (Property == null)
            {

                AppendValue(LanguagePrimitives.ConvertTo<string>(_inputObjects[0]));

                for (var index = 1; index < _inputObjects.Count; index++)
                {
                    builder.Append(Delimiter);
                    AppendValue(LanguagePrimitives.ConvertTo<string>(_inputObjects[index]));
                }
            }
            else if (Property is string propertyName)
            {
                string GetPropertyValueString(PSObject input, string name)
                {
                    return LanguagePrimitives.ConvertTo<string>(_propGetter.GetValue(input, name));
                }

                AppendValue(GetPropertyValueString(_inputObjects[0], propertyName));

                for (var index = 1; index < _inputObjects.Count; index++)
                {
                    builder.Append(Delimiter);
                    AppendValue(GetPropertyValueString(_inputObjects[index], propertyName));
                }
            }
            else if (Property is ScriptBlock sb)
            {
                string GetScriptBlockResultString(PSObject input)
                {
                    var result = sb.DoInvokeReturnAsIs(
                        useLocalScope: true,
                        errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                        dollarUnder: input,
                        input: AutomationNull.Value,
                        scriptThis: AutomationNull.Value,
                        args: Utils.EmptyArray<object>());
                    return LanguagePrimitives.ConvertTo<string>(result);
                }

                AppendValue(GetScriptBlockResultString(_inputObjects[0]));

                for (var index = 1; index < _inputObjects.Count; index++)
                {
                    builder.Append(Delimiter);
                    AppendValue(GetScriptBlockResultString(_inputObjects[index]));
                }
            }
            else
            {
                throw new ArgumentException();
            }

            builder.Append(Suffix);
            WriteObject(builder.ToString(), false);
        }
    }

    internal class JoinItemCompleter : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
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
                switch (Environment.NewLine)
                {
                    case "\r": return "`r";
                    case "\n": return "`n";
                    case "\r\n": return "`r`n";
                    default: return Environment.NewLine.Replace("\r", "`r").Replace("\n", "`n");
                }
            }
        }
    }
}