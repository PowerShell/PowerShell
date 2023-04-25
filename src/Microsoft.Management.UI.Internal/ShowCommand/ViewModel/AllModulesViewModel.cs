// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Windows;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Contains all Commands, Parameters, ParameterSet and Common Parameter.
    /// </summary>
    public class AllModulesViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        /// <summary>
        /// Flag indicating a wait message is being displayed.
        /// </summary>
        private bool waitMessageDisplayed;

        /// <summary>
        /// True if this ViewModel is not supposed to show common parameters.
        /// </summary>
        private bool noCommonParameter;

        /// <summary>
        /// the filterName of command.
        /// </summary>
        private string commandNameFilter;

        /// <summary>
        /// Field used for the Modules property.
        /// </summary>
        private List<ModuleViewModel> modules;

        /// <summary>
        /// true if a command can be run.
        /// </summary>
        private bool canRun;

        /// <summary>
        /// true if a command can be copied.
        /// </summary>
        private bool canCopy;

        /// <summary>
        /// the selected module being displayed in the GUI.
        /// </summary>
        private ModuleViewModel selectedModule;

        /// <summary>
        /// the visibility of the refresh button.
        /// </summary>
        private Visibility refreshVisibility = Visibility.Collapsed;

        /// <summary>
        /// Provides an extra viewModel object that allows callers to control certain aspects of the GUI.
        /// </summary>
        private object extraViewModel;

        /// <summary>
        /// private property for ZoomLevel.
        /// </summary>
        private double zoomLevel = 1.0;
        #endregion

        #region Construction and Destructor
        /// <summary>
        /// Initializes a new instance of the AllModulesViewModel class.
        /// </summary>
        /// <param name="importedModules">The loaded modules.</param>
        /// <param name="commands">Commands to show.</param>
        public AllModulesViewModel(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands)
        {
            ArgumentNullException.ThrowIfNull(commands);

            if (!commands.GetEnumerator().MoveNext())
            {
                throw new ArgumentNullException("commands");
            }

            this.Initialization(importedModules, commands, true);
        }

        /// <summary>
        /// Initializes a new instance of the AllModulesViewModel class.
        /// </summary>
        /// <param name="importedModules">The loaded modules.</param>
        /// <param name="commands">All PowerShell commands.</param>
        /// <param name="noCommonParameter">True not to show common parameters.</param>
        public AllModulesViewModel(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameter)
        {
            ArgumentNullException.ThrowIfNull(commands);

            this.Initialization(importedModules, commands, noCommonParameter);
        }

        #endregion

        #region INotifyPropertyChanged Members
        /// <summary>
        /// PropertyChanged Event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// Indicates the selected command in the selected module needs to display the help for a command.
        /// </summary>
        public event EventHandler<HelpNeededEventArgs> SelectedCommandInSelectedModuleNeedsHelp;

        /// <summary>
        /// Indicates the selected command in the selected module needs to import a module for a command.
        /// </summary>
        public event EventHandler<ImportModuleEventArgs> SelectedCommandInSelectedModuleNeedsImportModule;

        /// <summary>
        /// Indicates the selected command in the selected module should be run.
        /// </summary>
        public event EventHandler<CommandEventArgs> RunSelectedCommandInSelectedModule;

        /// <summary>
        /// Indicates we want to refresh the viewModel.
        /// </summary>
        public event EventHandler<EventArgs> Refresh;

        #region Public Properties

        /// <summary>
        /// Get or Sets Zoom level.
        /// </summary>
        public double ZoomLevel
        {
            get
            {
                return this.zoomLevel;
            }

            set
            {
                if (value > 0)
                {
                    this.zoomLevel = value / 100.0;
                    this.OnNotifyPropertyChanged("ZoomLevel");
                }
            }
        }

        /// <summary>
        /// Gets the tooltip for the refresh button.
        /// </summary>
        public static string RefreshTooltip
        {
            get { return string.Format(CultureInfo.CurrentUICulture, ShowCommandResources.RefreshShowCommandTooltipFormat, "import-module"); }
        }

        /// <summary>
        /// Gets or sets the visibility of the refresh button.
        /// </summary>
        public Visibility RefreshVisibility
        {
            get
            {
                return this.refreshVisibility;
            }

            set
            {
                if (this.refreshVisibility == value)
                {
                    return;
                }

                this.refreshVisibility = value;
                this.OnNotifyPropertyChanged("RefreshVisibility");
            }
        }

        /// <summary>
        /// Gets a value indicating whether common parameters are displayed.
        /// </summary>
        public bool NoCommonParameter
        {
            get { return this.noCommonParameter; }
        }

        /// <summary>
        /// Gets or sets the filterName of command.
        /// </summary>
        public string CommandNameFilter
        {
            get
            {
                return this.commandNameFilter;
            }

            set
            {
                if (this.CommandNameFilter == value)
                {
                    return;
                }

                this.commandNameFilter = value;
                if (this.selectedModule != null)
                {
                    this.selectedModule.RefreshFilteredCommands(this.CommandNameFilter);
                    this.selectedModule.SelectedCommand = null;
                }

                this.OnNotifyPropertyChanged("CommandNameFilter");
            }
        }

        /// <summary>
        /// Gets or sets the selected module being displayed in the GUI.
        /// </summary>
        public ModuleViewModel SelectedModule
        {
            get
            {
                return this.selectedModule;
            }

            set
            {
                if (this.selectedModule == value)
                {
                    return;
                }

                if (this.selectedModule != null)
                {
                    this.selectedModule.SelectedCommandNeedsImportModule -= this.SelectedModule_SelectedCommandNeedsImportModule;
                    this.selectedModule.SelectedCommandNeedsHelp -= this.SelectedModule_SelectedCommandNeedsHelp;
                    this.selectedModule.RunSelectedCommand -= this.SelectedModule_RunSelectedCommand;
                    this.selectedModule.PropertyChanged -= this.SelectedModule_PropertyChanged;
                }

                this.selectedModule = value;
                this.SetCanRun();
                this.SetCanCopy();

                if (this.selectedModule != null)
                {
                    this.selectedModule.RefreshFilteredCommands(this.CommandNameFilter);
                    this.selectedModule.SelectedCommandNeedsImportModule += this.SelectedModule_SelectedCommandNeedsImportModule;
                    this.selectedModule.SelectedCommandNeedsHelp += this.SelectedModule_SelectedCommandNeedsHelp;
                    this.selectedModule.RunSelectedCommand += this.SelectedModule_RunSelectedCommand;
                    this.selectedModule.PropertyChanged += this.SelectedModule_PropertyChanged;
                    this.selectedModule.SelectedCommand = null;
                }

                this.OnNotifyPropertyChanged("SelectedModule");
            }
        }

        /// <summary>
        /// Gets a value indicating whether we can run a command.
        /// </summary>
        public bool CanRun
        {
            get
            {
                return this.canRun;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we can copy a command.
        /// </summary>
        public bool CanCopy
        {
            get
            {
                return this.canCopy;
            }
        }

        /// <summary>
        /// Gets the Modules parameter.
        /// </summary>
        public List<ModuleViewModel> Modules
        {
            get { return this.modules; }
        }

        /// <summary>
        /// Gets the visibility of the wait message.
        /// </summary>
        public Visibility WaitMessageVisibility
        {
            get
            {
                return this.waitMessageDisplayed ? Visibility.Visible : Visibility.Hidden;
            }
        }

        /// <summary>
        /// Gets the visibility of the main grid.
        /// </summary>
        public Visibility MainGridVisibility
        {
            get
            {
                return this.waitMessageDisplayed ? Visibility.Hidden : Visibility.Visible;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the main grid is displayed.
        /// </summary>
        public bool MainGridDisplayed
        {
            get
            {
                return !this.waitMessageDisplayed;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the wait message is displayed.
        /// </summary>
        public bool WaitMessageDisplayed
        {
            get
            {
                return this.waitMessageDisplayed;
            }

            set
            {
                if (this.waitMessageDisplayed == value)
                {
                    return;
                }

                this.waitMessageDisplayed = value;
                this.SetCanCopy();
                this.SetCanRun();
                this.OnNotifyPropertyChanged("WaitMessageDisplayed");
                this.OnNotifyPropertyChanged("WaitMessageVisibility");
                this.OnNotifyPropertyChanged("MainGridDisplayed");
                this.OnNotifyPropertyChanged("MainGridVisibility");
            }
        }

        /// <summary>
        /// Gets or sets an extra viewModel object that allows callers to control certain aspects of the GUI.
        /// </summary>
        public object ExtraViewModel
        {
            get
            {
                return this.extraViewModel;
            }

            set
            {
                if (this.extraViewModel == value)
                {
                    return;
                }

                this.extraViewModel = value;
                this.OnNotifyPropertyChanged("ExtraViewModel");
            }
        }
        #endregion

        /// <summary>
        /// Returns the selected script.
        /// </summary>
        /// <returns>The selected script.</returns>
        public string GetScript()
        {
            if (this.SelectedModule == null)
            {
                return null;
            }

            if (this.SelectedModule.SelectedCommand == null)
            {
                return null;
            }

            return this.SelectedModule.SelectedCommand.GetScript();
        }

        /// <summary>
        /// Triggers Refresh.
        /// </summary>
        internal void OnRefresh()
        {
            EventHandler<EventArgs> handler = this.Refresh;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        #region Private Methods
        /// <summary>
        /// If current modules name is ALL, then return true.
        /// </summary>
        /// <param name="name">The modules name.</param>
        /// <returns>Return true is the module name is ALLModulesViewModel.</returns>
        private static bool IsAll(string name)
        {
            return name.Equals(ShowCommandResources.All, StringComparison.Ordinal);
        }

        /// <summary>
        /// Monitors property changes in the selected module to call:
        ///     SetCanRun for IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues
        ///     SetCanCopy for SetCanCopy
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues")
            {
                this.SetCanRun();
            }
            else if (e.PropertyName == "IsThereASelectedCommand")
            {
                this.SetCanCopy();
            }
        }

        /// <summary>
        /// Called to set this.CanRun when:
        ///     The SelectedModule changes, since there will be no selected command in the new module, and CanRun should be false
        ///     WaitMessageDisplayedMessage changes since this being true will cause this.MainGridDisplayed to be false and CanRun should be false
        ///     IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues changes in the selected module
        /// </summary>
        private void SetCanRun()
        {
            bool newValue = this.selectedModule != null && this.MainGridDisplayed &&
                this.selectedModule.IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;

            if (this.canRun == newValue)
            {
                return;
            }

            this.canRun = newValue;
            this.OnNotifyPropertyChanged("CanRun");
        }

        /// <summary>
        /// Called to set this.CanCopy when:
        ///     The SelectedModule changes, since there will be no selected command in the new module, and CanCopy should be false
        ///     WaitMessageDisplayedMessage changes since this being true will cause this.MainGridDisplayed to be false and CanCopy should be false
        ///     IsThereASelectedCommand changes in the selected module
        /// </summary>
        private void SetCanCopy()
        {
            bool newValue = this.selectedModule != null && this.MainGridDisplayed && this.selectedModule.IsThereASelectedCommand;

            if (this.canCopy == newValue)
            {
                return;
            }

            this.canCopy = newValue;
            this.OnNotifyPropertyChanged("CanCopy");
        }

        /// <summary>
        /// Initialize AllModulesViewModel.
        /// </summary>
        /// <param name="importedModules">All loaded modules.</param>
        /// <param name="commands">List of commands in all modules.</param>
        /// <param name="noCommonParameterInModel">Whether showing common parameter.</param>
        private void Initialization(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameterInModel)
        {
            if (commands == null)
            {
                return;
            }

            Dictionary<string, ModuleViewModel> rawModuleViewModels = new Dictionary<string, ModuleViewModel>();

            this.noCommonParameter = noCommonParameterInModel;

            // separates commands in their Modules
            foreach (ShowCommandCommandInfo command in commands)
            {
                ModuleViewModel moduleViewModel;
                if (!rawModuleViewModels.TryGetValue(command.ModuleName, out moduleViewModel))
                {
                    moduleViewModel = new ModuleViewModel(command.ModuleName, importedModules);
                    rawModuleViewModels.Add(command.ModuleName, moduleViewModel);
                }

                CommandViewModel commandViewModel;

                try
                {
                    commandViewModel = CommandViewModel.GetCommandViewModel(moduleViewModel, command, noCommonParameterInModel);
                }
                catch (RuntimeException)
                {
                    continue;
                }

                moduleViewModel.Commands.Add(commandViewModel);
                moduleViewModel.SetAllModules(this);
            }

            // populates this.modules
            this.modules = new List<ModuleViewModel>();

            // if there is just one module then use only it
            if (rawModuleViewModels.Values.Count == 1)
            {
                this.modules.Add(rawModuleViewModels.Values.First());
                this.modules[0].SortCommands(false);
                this.SelectedModule = this.modules[0];
                return;
            }

            // If there are more modules, create an additional module to aggregate all commands
            ModuleViewModel allCommandsModule = new ModuleViewModel(ShowCommandResources.All, null);
            this.modules.Add(allCommandsModule);
            allCommandsModule.SetAllModules(this);

            if (rawModuleViewModels.Values.Count > 0)
            {
                foreach (ModuleViewModel module in rawModuleViewModels.Values)
                {
                    module.SortCommands(false);
                    this.modules.Add(module);

                    allCommandsModule.Commands.AddRange(module.Commands);
                }
            }

            allCommandsModule.SortCommands(true);

            this.modules.Sort(this.Compare);
            this.SelectedModule = this.modules.Count == 0 ? null : this.modules[0];
        }

        /// <summary>
        /// Compare two ModuleViewModel target and source.
        /// </summary>
        /// <param name="source">The source ModuleViewModel.</param>
        /// <param name="target">The target ModuleViewModel.</param>
        /// <returns>Compare result.</returns>
        private int Compare(ModuleViewModel source, ModuleViewModel target)
        {
            if (AllModulesViewModel.IsAll(source.Name) && !AllModulesViewModel.IsAll(target.Name))
            {
                return -1;
            }

            if (!AllModulesViewModel.IsAll(source.Name) && AllModulesViewModel.IsAll(target.Name))
            {
                return 1;
            }

            return string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Called when the SelectedCommandNeedsHelp event is triggered in the Selected Module.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_SelectedCommandNeedsHelp(object sender, HelpNeededEventArgs e)
        {
            this.OnSelectedCommandInSelectedModuleNeedsHelp(e);
        }

        /// <summary>
        /// Called when the SelectedCommandNeedsImportModule event is triggered in the Selected Module.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_SelectedCommandNeedsImportModule(object sender, ImportModuleEventArgs e)
        {
            this.OnSelectedCommandInSelectedModuleNeedsImportModule(e);
        }

        /// <summary>
        /// Triggers SelectedCommandInSelectedModuleNeedsHelp.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnSelectedCommandInSelectedModuleNeedsHelp(HelpNeededEventArgs e)
        {
            EventHandler<HelpNeededEventArgs> handler = this.SelectedCommandInSelectedModuleNeedsHelp;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Triggers SelectedCommandInSelectedModuleNeedsImportModule.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnSelectedCommandInSelectedModuleNeedsImportModule(ImportModuleEventArgs e)
        {
            EventHandler<ImportModuleEventArgs> handler = this.SelectedCommandInSelectedModuleNeedsImportModule;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Called when the RunSelectedCommand is triggered in the selected module.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedModule_RunSelectedCommand(object sender, CommandEventArgs e)
        {
            this.OnRunSelectedCommandInSelectedModule(e);
        }

        /// <summary>
        /// Triggers RunSelectedCommandInSelectedModule.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnRunSelectedCommandInSelectedModule(CommandEventArgs e)
        {
            EventHandler<CommandEventArgs> handler = this.RunSelectedCommandInSelectedModule;
            if (handler != null)
            {
                handler(this, e);
            }
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
        #endregion
    }
}
