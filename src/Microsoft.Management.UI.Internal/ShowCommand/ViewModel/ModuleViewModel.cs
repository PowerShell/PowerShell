// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Windows;

using Microsoft.Management.UI.Internal;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// ModuleViewModel Contains information about a PowerShell module.
    /// </summary>
    public class ModuleViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// True if the module is imported.
        /// </summary>
        private bool isModuleImported;

        /// <summary>
        /// Field used for the Name parameter.
        /// </summary>
        private string name;

        /// <summary>
        ///  Filter commands property of this module.
        /// </summary>
        private ObservableCollection<CommandViewModel> filteredCommands;

        /// <summary>
        /// The selected command property of this module.
        /// </summary>
        private CommandViewModel selectedCommand;

        /// <summary>
        /// Field used for the Commands parameter.
        /// </summary>
        private List<CommandViewModel> commands;

        /// <summary>
        /// value indicating whether there is a selected command which belongs to an imported module,
        /// with no parameter sets or with a selected parameter set where all mandatory parameters have values
        /// </summary>
        private bool isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;

        /// <summary>
        /// value indicating whether there is a selected command.
        /// </summary>
        private bool isThereASelectedCommand;

        /// <summary>
        /// The AllModulesViewModel containing this, if any.
        /// </summary>
        private AllModulesViewModel allModules;

        #region Construction and Destructor
        /// <summary>
        /// Initializes a new instance of the ModuleViewModel class.
        /// </summary>
        /// <param name="name">Module name.</param>
        /// <param name="importedModules">All loaded modules.</param>
        public ModuleViewModel(string name, Dictionary<string, ShowCommandModuleInfo> importedModules)
        {
            ArgumentNullException.ThrowIfNull(name);

            this.name = name;
            this.commands = new List<CommandViewModel>();
            this.filteredCommands = new ObservableCollection<CommandViewModel>();

            // This check looks to see if the given module name shows up in
            // the set of modules that are known to be imported in the current
            // session.  In remote PowerShell sessions, the core cmdlet module
            // Microsoft.PowerShell.Core doesn't appear as being imported despite
            // always being loaded by default.  To make sure we don't incorrectly
            // mark this module as not imported, check for it by name.
            this.isModuleImported =
                importedModules == null ? true : name.Length == 0 ||
                importedModules.ContainsKey(name) ||
                string.Equals("Microsoft.PowerShell.Core", name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <summary>
        /// PropertyChanged Event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// Indicates the selected command in needs to display the help for a command.
        /// </summary>
        public event EventHandler<HelpNeededEventArgs> SelectedCommandNeedsHelp;

        /// <summary>
        /// Indicates the selected command needs to import a module.
        /// </summary>
        public event EventHandler<ImportModuleEventArgs> SelectedCommandNeedsImportModule;

        /// <summary>
        /// Indicates the selected command should be run.
        /// </summary>
        public event EventHandler<CommandEventArgs> RunSelectedCommand;

        #region Public Property
        /// <summary>
        /// Gets the name property of this ModuleView.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the GUI friendly module name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.name))
                {
                    return this.name;
                }

                return ShowCommandResources.NoModuleName;
            }
        }

        /// <summary>
        /// Gets CommandControl is visibility or not.
        /// </summary>
        public Visibility CommandControlVisibility
        {
            get { return this.selectedCommand == null ? Visibility.Collapsed : Visibility.Visible; }
        }

        /// <summary>
        ///  Gets CommandControl Height.
        /// </summary>
        public GridLength CommandRowHeight
        {
            get { return this.selectedCommand == null ? GridLength.Auto : CommandViewModel.Star; }
        }

        /// <summary>
        /// Gets the commands under in this module.
        /// </summary>
        public List<CommandViewModel> Commands
        {
            get { return this.commands; }
        }

        /// <summary>
        ///  Gets the filter commands of this module.
        /// </summary>
        public ObservableCollection<CommandViewModel> FilteredCommands
        {
            get { return this.filteredCommands; }
        }

        /// <summary>
        /// Gets or sets the selected commands of this module.
        /// </summary>
        public CommandViewModel SelectedCommand
        {
            get
            {
                return this.selectedCommand;
            }

            set
            {
                if (value == this.selectedCommand)
                {
                    return;
                }

                if (this.selectedCommand != null)
                {
                    this.selectedCommand.PropertyChanged -= this.SelectedCommand_PropertyChanged;
                    this.selectedCommand.HelpNeeded -= this.SelectedCommand_HelpNeeded;
                    this.selectedCommand.ImportModule -= this.SelectedCommand_ImportModule;
                }

                this.selectedCommand = value;

                this.SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues();

                if (this.selectedCommand != null)
                {
                    this.selectedCommand.PropertyChanged += this.SelectedCommand_PropertyChanged;
                    this.selectedCommand.HelpNeeded += this.SelectedCommand_HelpNeeded;
                    this.selectedCommand.ImportModule += this.SelectedCommand_ImportModule;
                    this.IsThereASelectedCommand = true;
                }
                else
                {
                    this.IsThereASelectedCommand = false;
                }

                this.OnNotifyPropertyChanged("SelectedCommand");
                this.OnNotifyPropertyChanged("CommandControlVisibility");
                this.OnNotifyPropertyChanged("CommandRowHeight");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there is a selected command.
        /// </summary>
        public bool IsThereASelectedCommand
        {
            get
            {
                return this.isThereASelectedCommand;
            }

            set
            {
                if (value == this.isThereASelectedCommand)
                {
                    return;
                }

                this.isThereASelectedCommand = value;
                this.OnNotifyPropertyChanged("IsThereASelectedCommand");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there is a selected command which belongs
        /// to an imported module, with no parameter sets or with a selected parameter set
        /// where all mandatory parameters have values
        /// </summary>
        public bool IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues
        {
            get
            {
                return this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues;
            }

            set
            {
                if (value == this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues)
                {
                    return;
                }

                this.isThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues = value;

                this.OnNotifyPropertyChanged("IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues");
            }
        }

        /// <summary>
        /// Gets the AllModulesViewModel containing this, if any.
        /// </summary>
        public AllModulesViewModel AllModules
        {
            get
            {
                return this.allModules;
            }
        }
        #endregion

        /// <summary>
        /// Gets a value indicating whether the module is imported.
        /// </summary>
        internal bool IsModuleImported
        {
            get
            {
                return this.isModuleImported;
            }
        }

        /// <summary>
        /// Sets the AllModulesViewModel containing this.
        /// </summary>
        /// <param name="parentAllModules">The AllModulesViewModel containing this.</param>
        internal void SetAllModules(AllModulesViewModel parentAllModules)
        {
            this.allModules = parentAllModules;
        }

        /// <summary>
        /// Sorts commands and optionally sets ModuleQualifyCommandName.
        /// </summary>
        /// <param name="markRepeatedCmdlets">True to mark repeated commands with a flag that will produce a module qualified name in GetScript.</param>
        internal void SortCommands(bool markRepeatedCmdlets)
        {
            this.commands.Sort(this.Compare);

            if (!markRepeatedCmdlets || this.commands.Count == 0)
            {
                return;
            }

            CommandViewModel reference = this.commands[0];
            for (int i = 1; i < this.commands.Count; i++)
            {
                CommandViewModel command = this.commands[i];
                if (reference.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase))
                {
                    reference.ModuleQualifyCommandName = true;
                    command.ModuleQualifyCommandName = true;
                }
                else
                {
                    reference = command;
                }
            }
        }

        /// <summary>
        /// According commandNameFilter to filter command,and added the filter commands into filteredCommands property.
        /// </summary>
        /// <param name="filter">Current filter.</param>
        internal void RefreshFilteredCommands(string filter)
        {
            this.filteredCommands.Clear();
            if (string.IsNullOrEmpty(filter))
            {
                foreach (CommandViewModel command in this.Commands)
                {
                    this.filteredCommands.Add(command);
                }

                return;
            }

            WildcardPattern filterPattern = null;
            if (WildcardPattern.ContainsWildcardCharacters(filter))
            {
                filterPattern = new WildcardPattern(filter, WildcardOptions.IgnoreCase);
            }

            foreach (CommandViewModel command in this.Commands)
            {
                if (ModuleViewModel.Matches(filterPattern, command.Name, filter))
                {
                    this.filteredCommands.Add(command);
                    continue;
                }

                if (filterPattern != null)
                {
                    continue;
                }

                string[] textSplit = filter.Split(' ');
                if (textSplit.Length != 2)
                {
                    continue;
                }

                if (ModuleViewModel.Matches(filterPattern, command.Name, textSplit[0] + "-" + textSplit[1]))
                {
                    this.filteredCommands.Add(command);
                }
            }
        }

        /// <summary>
        /// Called in response to a GUI event that requires the command to be run.
        /// </summary>
        internal void OnRunSelectedCommand()
        {
            EventHandler<CommandEventArgs> handler = this.RunSelectedCommand;
            if (handler != null)
            {
                handler(this, new CommandEventArgs(this.SelectedCommand));
            }
        }

        /// <summary>
        /// Triggers the SelectedCommandNeedsHelp event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        internal void OnSelectedCommandNeedsHelp(HelpNeededEventArgs e)
        {
            EventHandler<HelpNeededEventArgs> handler = this.SelectedCommandNeedsHelp;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Triggers the SelectedCommandNeedsImportModule event.
        /// </summary>
        internal void OnSelectedCommandNeedsImportModule()
        {
            EventHandler<ImportModuleEventArgs> handler = this.SelectedCommandNeedsImportModule;
            if (handler != null)
            {
                handler(this, new ImportModuleEventArgs(this.SelectedCommand.Name, this.SelectedCommand.ModuleName, this.Name));
            }
        }
        #region Private Method

        /// <summary>
        /// Uses pattern matching if pattern is not null or calls MatchesEvenIfInPlural otherwise.
        /// </summary>
        /// <param name="filterPattern">Pattern corresponding to filter.</param>
        /// <param name="commandName">Command name string.</param>
        /// <param name="filter">Filter string.</param>
        /// <returns>True if comparisonText matches str or pattern.</returns>
        private static bool Matches(WildcardPattern filterPattern, string commandName, string filter)
        {
            if (filterPattern != null)
            {
                return filterPattern.IsMatch(commandName);
            }

            return ModuleViewModel.MatchesEvenIfInPlural(commandName, filter);
        }

        /// <summary>
        /// Returns true if filter matches commandName, even when filter is in the plural.
        /// </summary>
        /// <param name="commandName">Command name string.</param>
        /// <param name="filter">Filter string.</param>
        /// <returns>Return match result.</returns>
        private static bool MatchesEvenIfInPlural(string commandName, string filter)
        {
            if (commandName.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (filter.Length > 5 && filter.EndsWith("es", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> filterSpan = filter.AsSpan(0, filter.Length - 2);
                return commandName.AsSpan().Contains(filterSpan, StringComparison.OrdinalIgnoreCase);
            }

            if (filter.Length > 4 && filter.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> filterSpan = filter.AsSpan(0, filter.Length - 1);
                return commandName.AsSpan().Contains(filterSpan, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Handles the HelpNeeded event in the selected command and triggers the SelectedCommandNeedsHelp event.
        /// </summary>
        /// <param name="sender">HelpNeeded event sender.</param>
        /// <param name="e">HelpNeeded event argument.</param>
        private void SelectedCommand_HelpNeeded(object sender, HelpNeededEventArgs e)
        {
            this.OnSelectedCommandNeedsHelp(e);
        }

        /// <summary>
        /// Handles the ImportModule event in the selected command and triggers the SelectedCommandNeedsImportModule event.
        /// </summary>
        /// <param name="sender">HelpNeeded event sender.</param>
        /// <param name="e">HelpNeeded event argument.</param>
        private void SelectedCommand_ImportModule(object sender, EventArgs e)
        {
            this.OnSelectedCommandNeedsImportModule();
        }

        /// <summary>
        /// Called when the SelectedCommand property changes to update IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SelectedCommand_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("SelectedParameterSetAllMandatoryParametersHaveValues"))
            {
                return;
            }

            this.SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues();
        }

        /// <summary>
        /// Called to set IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues when
        /// SelectedParameterSetAllMandatoryParametersHaveValues changes in the SelectedCommand or
        /// when the SelectedCommand changes
        /// </summary>
        private void SetIsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues()
        {
            this.IsThereASelectedImportedCommandWhereAllMandatoryParametersHaveValues =
                this.selectedCommand != null &&
                this.selectedCommand.IsImported &&
                this.selectedCommand.SelectedParameterSetAllMandatoryParametersHaveValues;
        }

        /// <summary>
        /// Compare source commandmodule is equal like target commandmodule.
        /// </summary>
        /// <param name="source">Source commandmodule.</param>
        /// <param name="target">Target commandmodule.</param>
        /// <returns>Return compare result.</returns>
        private int Compare(CommandViewModel source, CommandViewModel target)
        {
            return string.Compare(source.Name, target.Name, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

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
    }
}
