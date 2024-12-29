// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Windows;

using Microsoft.Management.UI;
using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

using SMAI = System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Contains information about a cmdlet's Shard ParameterSet,
    /// ParameterSets, Parameters, Common Parameters and error message.
    /// </summary>
    public class CommandViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        /// <summary>
        /// The name of the AllParameterSets.
        /// </summary>
        private const string SharedParameterSetName = "__AllParameterSets";

        /// <summary>
        /// Grid length constant.
        /// </summary>
        private static readonly GridLength star = new GridLength(1, GridUnitType.Star);

        /// <summary>
        /// The module containing this cmdlet in the gui.
        /// </summary>
        private ModuleViewModel parentModule;

        /// <summary>
        /// The name of the default ParameterSet.
        /// </summary>
        private string defaultParameterSetName;

        /// <summary>
        /// Field used for the AreCommonParametersExpanded parameter.
        /// </summary>
        private bool areCommonParametersExpanded;

        /// <summary>
        /// Field used for the SelectedParameterSet parameter.
        /// </summary>
        private ParameterSetViewModel selectedParameterSet;

        /// <summary>
        /// Field used for the ParameterSets parameter.
        /// </summary>
        private List<ParameterSetViewModel> parameterSets = new List<ParameterSetViewModel>();

        /// <summary>
        /// Field used for the ParameterSetTabControlVisibility parameter.
        /// </summary>
        private bool noCommonParameters;

        /// <summary>
        /// Field used for the CommonParameters parameter.
        /// </summary>
        private ParameterSetViewModel comonParameters;

        /// <summary>
        /// The ShowCommandCommandInfo this model is based on.
        /// </summary>
        private ShowCommandCommandInfo commandInfo;

        /// <summary>
        ///  value indicating whether the selected parameter set has all mandatory parameters valid.
        /// </summary>
        private bool selectedParameterSetAllMandatoryParametersHaveValues;

        /// <summary>
        /// value indicating whether the command name should be qualified by the module in GetScript.
        /// </summary>
        private bool moduleQualifyCommandName;

        /// <summary>
        /// The height for common parameters that will depend on CommonParameterVisibility.
        /// </summary>
        private GridLength commonParametersHeight;
        #endregion

        /// <summary>
        /// Prevents a default instance of the CommandViewModel class from being created.
        /// </summary>
        private CommandViewModel()
        {
        }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// PropertyChanged Event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Indicates the command needs to display the help for a command.
        /// </summary>
        public event EventHandler<HelpNeededEventArgs> HelpNeeded;

        /// <summary>
        /// Indicates a module needs to be imported.
        /// </summary>
        public event EventHandler<EventArgs> ImportModule;

        #region Public Properties
        /// <summary>
        /// Gets or sets a value indicating whether the command name should be qualified by the module in GetScript.
        /// </summary>
        public bool ModuleQualifyCommandName
        {
            get { return this.moduleQualifyCommandName; }
            set { this.moduleQualifyCommandName = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the common parameters are expanded.
        /// </summary>
        public bool AreCommonParametersExpanded
        {
            get
            {
                return this.areCommonParametersExpanded;
            }

            set
            {
                if (this.areCommonParametersExpanded == value)
                {
                    return;
                }

                this.areCommonParametersExpanded = value;
                this.OnNotifyPropertyChanged("AreCommonParametersExpanded");
                this.SetCommonParametersHeight();
            }
        }

        /// <summary>
        /// Gets or sets the SelectedParameterSet parameter.
        /// </summary>
        public ParameterSetViewModel SelectedParameterSet
        {
            get
            {
                return this.selectedParameterSet;
            }

            set
            {
                if (this.selectedParameterSet != value)
                {
                    if (this.selectedParameterSet != null)
                    {
                        this.selectedParameterSet.PropertyChanged -= this.SelectedParameterSet_PropertyChanged;
                    }

                    this.selectedParameterSet = value;
                    if (this.selectedParameterSet != null)
                    {
                        this.selectedParameterSet.PropertyChanged += this.SelectedParameterSet_PropertyChanged;
                        this.SelectedParameterSetAllMandatoryParametersHaveValues = this.SelectedParameterSet.AllMandatoryParametersHaveValues;
                    }
                    else
                    {
                        this.SelectedParameterSetAllMandatoryParametersHaveValues = true;
                    }

                    this.OnNotifyPropertyChanged("SelectedParameterSet");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selected parameter set has all mandatory parameters valid.
        /// If there is no selected parameter set this value is true
        /// </summary>
        public bool SelectedParameterSetAllMandatoryParametersHaveValues
        {
            get
            {
                return this.selectedParameterSetAllMandatoryParametersHaveValues;
            }

            set
            {
                if (this.selectedParameterSetAllMandatoryParametersHaveValues == value)
                {
                    return;
                }

                this.selectedParameterSetAllMandatoryParametersHaveValues = value;
                this.OnNotifyPropertyChanged("SelectedParameterSetAllMandatoryParametersHaveValues");
            }
        }

        /// <summary>
        /// Gets the ParameterSets parameter.
        /// </summary>
        public List<ParameterSetViewModel> ParameterSets
        {
            get { return this.parameterSets; }
        }

        /// <summary>
        /// Gets the visibility for the tab control displaying several ParameterSetControl. This is displayed when there are more than 1 parameter sets.
        /// </summary>
        public Visibility ParameterSetTabControlVisibility
        {
            get { return (this.ParameterSets.Count > 1) && this.IsImported ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Gets the visibility for the single ParameterSetControl displayed when there is only 1 parameter set.
        /// </summary>
        public Visibility SingleParameterSetControlVisibility
        {
            get { return (this.ParameterSets.Count == 1) ? Visibility.Visible : Visibility.Collapsed; }
        }

        /// <summary>
        /// Gets the CommonParameters parameter.
        /// </summary>
        public ParameterSetViewModel CommonParameters
        {
            get { return this.comonParameters; }
        }

        /// <summary>
        /// Gets the CommonParameterVisibility parameter.
        /// </summary>
        public Visibility CommonParameterVisibility
        {
            get { return this.noCommonParameters || (this.CommonParameters.Parameters.Count == 0) ? Visibility.Collapsed : Visibility.Visible; }
        }

        /// <summary>
        /// Gets or sets the height for common parameters that will depend on CommonParameterVisibility.
        /// </summary>
        public GridLength CommonParametersHeight
        {
            get
            {
                return this.commonParametersHeight;
            }

            set
            {
                if (this.commonParametersHeight == value)
                {
                    return;
                }

                this.commonParametersHeight = value;
                this.OnNotifyPropertyChanged("CommonParametersHeight");
            }
        }

        /// <summary>
        /// Gets the visibility for the control displayed when the module is not imported.
        /// </summary>
        public Visibility NotImportedVisibility
        {
            get
            {
                return this.IsImported ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Gets the visibility for the control displayed when there are no parameters.
        /// </summary>
        public Visibility NoParameterVisibility
        {
            get
            {
                bool hasNoParameters = this.ParameterSets.Count == 0 || (this.ParameterSets.Count == 1 && this.ParameterSets[0].Parameters.Count == 0);
                return this.IsImported && hasNoParameters ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the cmdlet comes from a module which is imported.
        /// </summary>
        public bool IsImported
        {
            get
            {
                return this.commandInfo.Module == null || this.ParentModule.IsModuleImported;
            }
        }

        /// <summary>
        /// Gets the Name parameter.
        /// </summary>
        public string Name
        {
            get
            {
                if (this.commandInfo != null)
                {
                    return this.commandInfo.Name;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the module path if it is not null or empty, or the name otherwise.
        /// </summary>
        public string ModuleName
        {
            get
            {
                if (this.commandInfo != null && this.commandInfo.ModuleName != null)
                {
                    return this.commandInfo.ModuleName;
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the module containing this cmdlet in the GUI.
        /// </summary>
        public ModuleViewModel ParentModule
        {
            get
            {
                return this.parentModule;
            }
        }

        /// <summary>
        /// Gets Tooltip string for the cmdlet.
        /// </summary>
        public string ToolTip
        {
            get
            {
                return string.Format(
                     CultureInfo.CurrentCulture,
                     ShowCommandResources.CmdletTooltipFormat,
                     this.Name,
                     this.ParentModule.DisplayName,
                     this.IsImported ? ShowCommandResources.Imported : ShowCommandResources.NotImported);
            }
        }

        /// <summary>
        /// Gets the message to be displayed when the cmdlet belongs to a module that is not imported.
        /// </summary>
        public string ImportModuleMessage
        {
            get
            {
                return string.Format(
                     CultureInfo.CurrentCulture,
                     ShowCommandResources.NotImportedFormat,
                     this.ModuleName,
                     this.Name,
                     ShowCommandResources.ImportModuleButtonText);
            }
        }

        /// <summary>
        /// Gets the title for the cmdlet details.
        /// </summary>
        public string DetailsTitle
        {
            get
            {
                if (this.IsImported)
                {
                    return string.Format(
                         CultureInfo.CurrentCulture,
                         ShowCommandResources.DetailsParameterTitleFormat,
                         this.Name);
                }
                else
                {
                    return string.Format(
                         CultureInfo.CurrentCulture,
                         ShowCommandResources.NameLabelFormat,
                         this.Name);
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets a Grid length constant.
        /// </summary>
        internal static GridLength Star
        {
            get { return CommandViewModel.star; }
        }

        /// <summary>
        /// Gets the builded PowerShell script.
        /// </summary>
        /// <returns>Return script as string.</returns>
        public string GetScript()
        {
            StringBuilder builder = new StringBuilder();

            string commandName = this.commandInfo.CommandType == CommandTypes.ExternalScript ? this.commandInfo.Definition : this.Name;

            if (this.ModuleQualifyCommandName && !string.IsNullOrEmpty(this.ModuleName))
            {
                commandName = this.ModuleName + "\\" + commandName;
            }

            if (commandName.Contains(' '))
            {
                builder.Append($"& \"{commandName}\"");
            }
            else
            {
                builder.Append(commandName);
            }

            builder.Append(' ');

            if (this.SelectedParameterSet != null)
            {
                builder.Append(this.SelectedParameterSet.GetScript());
                builder.Append(' ');
            }

            if (this.CommonParameters != null)
            {
                builder.Append(this.CommonParameters.GetScript());
            }

            string script = builder.ToString();

            return script.Trim();
        }

        /// <summary>
        /// Showing help information for current active cmdlet.
        /// </summary>
        public void OpenHelpWindow()
        {
            this.OnHelpNeeded();
        }

        /// <summary>
        /// Determines whether current command name and a specified ParameterSetName have same name.
        /// </summary>
        /// <param name="name">The name of ShareParameterSet.</param>
        /// <returns>Return true is ShareParameterSet. Else return false.</returns>
        internal static bool IsSharedParameterSetName(string name)
        {
            return name.Equals(CommandViewModel.SharedParameterSetName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a new CommandViewModel out the <paramref name="commandInfo"/>.
        /// </summary>
        /// <param name="module">Module to which the CommandViewModel will belong to.</param>
        /// <param name="commandInfo">Will showing command.</param>
        /// <param name="noCommonParameters">True to ommit displaying common parameter.</param>
        /// <exception cref="ArgumentNullException">If commandInfo is null</exception>
        /// <exception cref="RuntimeException">
        /// If could not create the CommandViewModel. For instance the ShowCommandCommandInfo corresponding to
        /// the following function will throw a RuntimeException when the ShowCommandCommandInfo Parameters
        /// are retrieved:
        /// function CrashMe ([I.Am.A.Type.That.Does.Not.Exist]$name) {}
        /// </exception>
        /// <returns>The CommandViewModel corresponding to commandInfo.</returns>
        internal static CommandViewModel GetCommandViewModel(ModuleViewModel module, ShowCommandCommandInfo commandInfo, bool noCommonParameters)
        {
            ArgumentNullException.ThrowIfNull(commandInfo);

            CommandViewModel returnValue = new CommandViewModel();
            returnValue.commandInfo = commandInfo;
            returnValue.noCommonParameters = noCommonParameters;
            returnValue.parentModule = module;

            Dictionary<string, ParameterViewModel> commonParametersTable = new Dictionary<string, ParameterViewModel>();

            foreach (ShowCommandParameterSetInfo parameterSetInfo in commandInfo.ParameterSets)
            {
                if (parameterSetInfo.IsDefault)
                {
                    returnValue.defaultParameterSetName = parameterSetInfo.Name;
                }

                List<ParameterViewModel> parametersForParameterSet = new List<ParameterViewModel>();
                foreach (ShowCommandParameterInfo parameterInfo in parameterSetInfo.Parameters)
                {
                    bool isCommon = Cmdlet.CommonParameters.Contains(parameterInfo.Name);

                    if (isCommon)
                    {
                        if (!commonParametersTable.ContainsKey(parameterInfo.Name))
                        {
                            commonParametersTable.Add(parameterInfo.Name, new ParameterViewModel(parameterInfo, parameterSetInfo.Name));
                        }

                        continue;
                    }

                    parametersForParameterSet.Add(new ParameterViewModel(parameterInfo, parameterSetInfo.Name));
                }

                if (parametersForParameterSet.Count != 0)
                {
                    returnValue.ParameterSets.Add(new ParameterSetViewModel(parameterSetInfo.Name, parametersForParameterSet));
                }
            }

            List<ParameterViewModel> commonParametersList = commonParametersTable.Values.ToList<ParameterViewModel>();
            returnValue.comonParameters = new ParameterSetViewModel(string.Empty, commonParametersList);

            returnValue.parameterSets.Sort(returnValue.Compare);

            if (returnValue.parameterSets.Count > 0)
            {
                // Setting SelectedParameterSet will also set SelectedParameterSetAllMandatoryParametersHaveValues
                returnValue.SelectedParameterSet = returnValue.ParameterSets[0];
            }
            else
            {
                returnValue.SelectedParameterSetAllMandatoryParametersHaveValues = true;
            }

            returnValue.SetCommonParametersHeight();

            return returnValue;
        }

        /// <summary>
        /// Called to trigger the event fired when help is needed for the command.
        /// </summary>
        internal void OnHelpNeeded()
        {
            EventHandler<HelpNeededEventArgs> handler = this.HelpNeeded;
            if (handler != null)
            {
                handler(this, new HelpNeededEventArgs(this.Name));
            }
        }

        /// <summary>
        /// Called to trigger the event fired when a module needs to be imported.
        /// </summary>
        internal void OnImportModule()
        {
            EventHandler<EventArgs> handler = this.ImportModule;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #region Private Methods
        /// <summary>
        /// Called to set the height for common parameters initially or when the AreCommonParametersExpanded changes.
        /// </summary>
        private void SetCommonParametersHeight()
        {
            this.CommonParametersHeight = this.AreCommonParametersExpanded ? CommandViewModel.Star : GridLength.Auto;
        }

        /// <summary>
        /// Compares source and target by being the default parameter set and then by name.
        /// </summary>
        /// <param name="source">Source paremeterset.</param>
        /// <param name="target">Target parameterset.</param>
        /// <returns>0 if they are the same, -1 if source is smaller, 1 if source is larger.</returns>
        private int Compare(ParameterSetViewModel source, ParameterSetViewModel target)
        {
            if (this.defaultParameterSetName != null)
            {
                if (source.Name.Equals(this.defaultParameterSetName) && target.Name.Equals(this.defaultParameterSetName))
                {
                    return 0;
                }

                if (source.Name.Equals(this.defaultParameterSetName, StringComparison.Ordinal))
                {
                    return -1;
                }

                if (target.Name.Equals(this.defaultParameterSetName, StringComparison.Ordinal))
                {
                    return 1;
                }
            }

            return string.CompareOrdinal(source.Name, target.Name);
        }

        /// <summary>
        /// If property changed will be notify.
        /// </summary>
        /// <param name="propertyName">The changed property.</param>
        private void OnNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Called when the PropertyChanged event is triggered on the SelectedParameterSet.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedParameterSet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("AllMandatoryParametersHaveValues"))
            {
                return;
            }

            this.SelectedParameterSetAllMandatoryParametersHaveValues = this.SelectedParameterSet.AllMandatoryParametersHaveValues;
        }

        #endregion
    }
}
