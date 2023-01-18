// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Text;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Contains information about a single parameter inside a parameter set.
    /// If a parameter with the same name belongs to two (or more) parameter sets,
    /// there will be two (or more) ParameterViewModel objects for the parameters,
    /// each one inside its own ParameterSetViewModel.
    /// </summary>
    public class ParameterViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// ParameterMetadata contains information that is the same throughout parameter sets
        /// like Name and Type.
        /// Note: It also happens to contain a list of all ParameterSetMetadata for the parametersets
        /// in this cmdlet, but this information is not used in this class since if a parameter is
        /// in multiple parametersets, there will be a ParameterViewModel for each time the parameter
        /// appears in a parameterset.
        /// </summary>
        private ShowCommandParameterInfo parameter;

        /// <summary>
        /// value entered in the GUI for the parameter.
        /// </summary>
        private object parameterValue;

        /// <summary>
        /// Name of the parameter set this parameter is in.
        /// </summary>
        private string parameterSetName;

        #region Construction and Destructor
        /// <summary>
        /// Initializes a new instance of the ParameterViewModel class.
        /// </summary>
        /// <param name="parameter">The parameter information for this parameter.</param>
        /// <param name="parameterSetName">The name of the parameter set this parameter is in.</param>
        public ParameterViewModel(ShowCommandParameterInfo parameter, string parameterSetName)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            ArgumentNullException.ThrowIfNull(parameterSetName);

            this.parameter = parameter;
            this.parameterSetName = parameterSetName;

            if (this.parameter.ParameterType.IsSwitch)
            {
                this.parameterValue = false;
            }
            else
            {
                this.parameterValue = string.Empty;
            }
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// PropertyChanged Event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties
        /// <summary>
        /// Gets the ParameterMetadata that contains information that is the same throughout parameter sets
        /// like Name and Type.
        /// </summary>
        public ShowCommandParameterInfo Parameter
        {
            get { return this.parameter; }
        }

        /// <summary>
        /// Gets or sets the value for this parameter from the GUI.
        /// </summary>
        public object Value
        {
            get
            {
                return this.parameterValue;
            }

            set
            {
                if (this.parameterValue != value)
                {
                    this.parameterValue = value;
                    this.OnNotifyPropertyChanged("Value");
                }
            }
        }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name
        {
            get { return this.Parameter.Name; }
        }

        /// <summary>
        /// Gets the name of the parameter set this parameter is in.
        /// </summary>
        public string ParameterSetName
        {
            get { return this.parameterSetName; }
        }

        /// <summary>
        /// Gets a value indicating whether this parameter is in the shared parameterset.
        /// </summary>
        public bool IsInSharedParameterSet
        {
            get { return CommandViewModel.IsSharedParameterSetName(this.parameterSetName); }
        }

        /// <summary>
        /// Gets Name with an extra suffix to indicate if the parameter is mandatory to serve.
        /// </summary>
        public string NameTextLabel
        {
            get
            {
                return this.Parameter.IsMandatory ?
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ShowCommandResources.MandatoryNameLabelFormat,
                        this.Name,
                        ShowCommandResources.MandatoryLabelSegment) :
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ShowCommandResources.NameLabelFormat,
                        this.Name);
            }
        }

        /// <summary>
        /// Gets Label in the case this parameter is used in a combo box.
        /// </summary>
        public string NameCheckLabel
        {
            get
            {
                string returnValue = this.Parameter.Name;
                if (this.Parameter.IsMandatory)
                {
                    returnValue = string.Format(CultureInfo.CurrentUICulture, $"{returnValue}{ShowCommandResources.MandatoryLabelSegment}");
                }

                return returnValue;
            }
        }

        /// <summary>
        /// Gets Tooltip string for the parameter.
        /// </summary>
        public string ToolTip
        {
            get
            {
                return ParameterViewModel.EvaluateTooltip(
                    this.Parameter.ParameterType.FullName,
                    this.Parameter.Position,
                    this.Parameter.IsMandatory,
                    this.IsInSharedParameterSet,
                    this.Parameter.ValueFromPipeline);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the parameter is mandatory.
        /// </summary>
        public bool IsMandatory
        {
            get { return this.Parameter.IsMandatory; }
        }

        /// <summary>
        /// Gets a value indicating whether the parameter has a value.
        /// </summary>
        public bool HasValue
        {
            get
            {
                if (this.Value == null)
                {
                    return false;
                }

                if (this.Parameter.ParameterType.IsSwitch)
                {
                    return ((bool?)this.Value) == true;
                }

                return this.Value.ToString().Length != 0;
            }
        }
        #endregion

        /// <summary>
        /// Evaluates the tooltip based on the parameters.
        /// </summary>
        /// <param name="typeName">Parameter type name.</param>
        /// <param name="position">Parameter position.</param>
        /// <param name="mandatory">True if the parameter is mandatory.</param>
        /// <param name="shared">True if the parameter is shared by parameter sets.</param>
        /// <param name="valueFromPipeline">True if the parameter takes value from the pipeline.</param>
        /// <returns> the tooltip based on the parameters.</returns>
        internal static string EvaluateTooltip(string typeName, int position, bool mandatory, bool shared, bool valueFromPipeline)
        {
            StringBuilder returnValue = new StringBuilder(string.Format(
                    CultureInfo.CurrentCulture,
                    ShowCommandResources.TypeFormat,
                    typeName));
            string newlineFormatString = Environment.NewLine + "{0}";

            if (position >= 0)
            {
                string positionFormat = string.Format(
                    CultureInfo.CurrentCulture,
                    ShowCommandResources.PositionFormat,
                    position);

                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, positionFormat);
            }

            string optionalOrMandatory = mandatory ? ShowCommandResources.Mandatory : ShowCommandResources.Optional;

            returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, optionalOrMandatory);

            if (shared)
            {
                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, ShowCommandResources.CommonToAllParameterSets);
            }

            if (valueFromPipeline)
            {
                returnValue.AppendFormat(CultureInfo.InvariantCulture, newlineFormatString, ShowCommandResources.CanReceiveValueFromPipeline);
            }

            return returnValue.ToString();
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
    }
}
