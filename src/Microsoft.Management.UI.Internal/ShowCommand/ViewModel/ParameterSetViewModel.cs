// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    ///  Contains information about a single ParameterSet inside a cmdlet.
    /// </summary>
    public class ParameterSetViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Field used for the Name parameter.
        /// </summary>
        private string name;

        /// <summary>
        /// value indicating all mandatory parameters have values.
        /// </summary>
        private bool allMandatoryParametersHaveValues;

        /// <summary>
        /// Field used for the Parameters parameter.
        /// </summary>
        private List<ParameterViewModel> parameters;

        #region Construction and Destructor

        /// <summary>
        /// Initializes a new instance of the ParameterSetViewModel class.
        /// </summary>
        /// <param name="name">The name of the parameterSet.</param>
        /// <param name="parameters">The array parameters of the parameterSet.</param>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "this type is internal, made public only for WPF Binding")]
        public ParameterSetViewModel(
            string name,
            List<ParameterViewModel> parameters)
        {
            ArgumentNullException.ThrowIfNull(name);

            ArgumentNullException.ThrowIfNull(parameters);

            parameters.Sort(Compare);

            this.name = name;
            this.parameters = parameters;
            foreach (ParameterViewModel parameter in this.parameters)
            {
                if (!parameter.IsMandatory)
                {
                    continue;
                }

                parameter.PropertyChanged += this.MandatoryParameter_PropertyChanged;
            }

            this.EvaluateAllMandatoryParametersHaveValues();
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// PropertyChanged Event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Property
        /// <summary>
        /// Gets the ParameterSet Name.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the Parameters of this parameterset.
        /// </summary>
        public List<ParameterViewModel> Parameters
        {
            get { return this.parameters; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether all mandatory parameters have values.
        /// </summary>
        public bool AllMandatoryParametersHaveValues
        {
            get
            {
                return this.allMandatoryParametersHaveValues;
            }

            set
            {
                if (this.allMandatoryParametersHaveValues != value)
                {
                    this.allMandatoryParametersHaveValues = value;
                    this.OnNotifyPropertyChanged("AllMandatoryParametersHaveValues");
                }
            }
        }
        #endregion

        #region Public Method
        /// <summary>
        /// Creates script according parameters of this parameterset.
        /// </summary>
        /// <returns>Return script of this parameterset parameters.</returns>
        public string GetScript()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (ParameterViewModel parameter in this.Parameters)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                if (parameter.Parameter.ParameterType.IsSwitch)
                {
                    if (((bool?)parameter.Value) == true)
                    {
                        builder.AppendFormat($"-{parameter.Name} ");
                    }

                    continue;
                }

                string parameterValueString = parameter.Value.ToString();

                if (parameterValueString.Length == 0)
                {
                    continue;
                }

                ShowCommandParameterType parameterType = parameter.Parameter.ParameterType;

                if (parameterType.IsEnum || parameterType.IsString || (parameterType.IsArray && parameterType.ElementType.IsString))
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "\"", "\"");
                }
                else if (parameterType.IsScriptBlock)
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "{", "}");
                }
                else
                {
                    parameterValueString = ParameterSetViewModel.GetDelimitedParameter(parameterValueString, "(", ")");
                }

                builder.Append($"-{parameter.Name} {parameterValueString} ");
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// Gets the individual parameter count of this parameterset.
        /// </summary>
        /// <returns>Return individual parameter count of this parameterset.</returns>
        public int GetIndividualParameterCount()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return 0;
            }

            int i = 0;

            foreach (ParameterViewModel p in this.Parameters)
            {
                if (p.IsInSharedParameterSet)
                {
                    return i;
                }

                i++;
            }

            return i;
        }

        #endregion

        #region Internal Method

        /// <summary>
        /// Compare source parametermodel is equal like target parametermodel.
        /// </summary>
        /// <param name="source">The source of parametermodel.</param>
        /// <param name="target">The target of parametermodel.</param>
        /// <returns>Return compare result.</returns>
        internal static int Compare(ParameterViewModel source, ParameterViewModel target)
        {
            if (source.Parameter.IsMandatory && !target.Parameter.IsMandatory)
            {
                return -1;
            }

            if (!source.Parameter.IsMandatory && target.Parameter.IsMandatory)
            {
                return 1;
            }

            return string.Compare(source.Parameter.Name, target.Parameter.Name);
        }

        #endregion

        /// <summary>
        /// Gets the delimited poarameter if it needs delimitation and is not delimited.
        /// </summary>
        /// <param name="parameterValue">Value needing delimitation.</param>
        /// <param name="openDelimiter">Open delimitation.</param>
        /// <param name="closeDelimiter">Close delimitation.</param>
        /// <returns>The delimited poarameter if it needs delimitation and is not delimited.</returns>
        private static string GetDelimitedParameter(string parameterValue, string openDelimiter, string closeDelimiter)
        {
            string parameterValueTrimmed = parameterValue.Trim();

            if (parameterValueTrimmed.Length == 0)
            {
                return openDelimiter + parameterValue + closeDelimiter;
            }

            char delimitationChar = ParameterSetViewModel.ParameterNeedsDelimitation(parameterValueTrimmed, openDelimiter.Length == 1 && openDelimiter[0] == '{');
            switch (delimitationChar)
            {
                case '1':
                    return openDelimiter + parameterValue + closeDelimiter;
                case '\'':
                    return '\'' + parameterValue + '\'';
                case '\"':
                    return '\"' + parameterValue + '\"';
                default:
                    return parameterValueTrimmed;
            }
        }

        /// <summary>
        /// Returns '0' if the <paramref name="parameterValue"/> does not need delimitation, '1' if it does, and a quote character if it needs to be delimited with a quote.
        /// </summary>
        /// <param name="parameterValue">Parameter value to check.</param>
        /// <param name="requireScriptblock">True if the parameter value should be a scriptblock.</param>
        /// <returns>'0' if the parameter does not need delimitation, '1' if it needs, '\'' if it needs to be delimited with single quote and '\"' if it needs to be delimited with double quotes.</returns>
        private static char ParameterNeedsDelimitation(string parameterValue, bool requireScriptblock)
        {
            Token[] tokens;
            ParseError[] errors;
            ScriptBlockAst values = Parser.ParseInput("commandName -parameterName " + parameterValue, out tokens, out errors);

            if (values == null || values.EndBlock == null || values.EndBlock.Statements.Count == 0)
            {
                return '1';
            }

            PipelineAst pipeline = values.EndBlock.Statements[0] as PipelineAst;
            if (pipeline == null || pipeline.PipelineElements.Count == 0)
            {
                return '1';
            }

            CommandAst commandAst = pipeline.PipelineElements[0] as CommandAst;

            if (commandAst == null || commandAst.CommandElements.Count == 0)
            {
                return '1';
            }

            // 3 is for CommandName, Parameter and its value
            if (commandAst.CommandElements.Count != 3)
            {
                return '1';
            }

            if (requireScriptblock)
            {
                ScriptBlockExpressionAst scriptAst = commandAst.CommandElements[2] as ScriptBlockExpressionAst;
                return scriptAst == null ? '1' : '0';
            }

            StringConstantExpressionAst stringValue = commandAst.CommandElements[2] as StringConstantExpressionAst;
            if (stringValue != null)
            {
                if (errors.Length == 0)
                {
                    return '0';
                }

                char stringTerminationChar;

                if (stringValue.StringConstantType == StringConstantType.BareWord)
                {
                    stringTerminationChar = parameterValue[0];
                }
                else if (stringValue.StringConstantType == StringConstantType.DoubleQuoted || stringValue.StringConstantType == StringConstantType.DoubleQuotedHereString)
                {
                    stringTerminationChar = '\"';
                }
                else
                {
                    stringTerminationChar = '\'';
                }

                char oppositeTerminationChar = stringTerminationChar == '\"' ? '\'' : '\"';

                // If the string is not terminated, it should be delimited by the opposite string termination character
                return oppositeTerminationChar;
            }

            if (errors.Length != 0)
            {
                return '1';
            }

            return '0';
        }

        /// <summary>
        /// Called to evaluate the value of AllMandatoryParametersHaveValues.
        /// </summary>
        private void EvaluateAllMandatoryParametersHaveValues()
        {
            bool newCanRun = true;
            foreach (ParameterViewModel parameter in this.parameters)
            {
                if (!parameter.IsMandatory)
                {
                    continue;
                }

                if (!parameter.HasValue)
                {
                    newCanRun = false;
                    break;
                }
            }

            this.AllMandatoryParametersHaveValues = newCanRun;
        }

        /// <summary>
        /// If property changed will be notify.
        /// </summary>
        /// <param name="propertyName">The changed property.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            #pragma warning disable IDE1005 // IDE1005: Delegate invocation can be simplified.
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            #pragma warning restore IDE1005
        }

        /// <summary>
        /// Used to track changes to parameter values in order to verify the enabled state of buttons.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="e">Event sender.</param>
        private void MandatoryParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("Value", StringComparison.Ordinal))
            {
                return;
            }

            this.EvaluateAllMandatoryParametersHaveValues();
        }
    }
}
