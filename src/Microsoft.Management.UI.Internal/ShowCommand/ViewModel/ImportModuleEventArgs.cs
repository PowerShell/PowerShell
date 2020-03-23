// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Arguments for the event triggered when it is necessary to display help for a command.
    /// </summary>
    public class ImportModuleEventArgs : EventArgs
    {
        /// <summary>
        /// the name for the command belonging to the module to be imported.
        /// </summary>
        private string commandName;

        /// <summary>
        /// the module path or name for the module we want to import.
        /// </summary>
        private string parentModuleName;

        /// <summary>
        /// the name of the module that is selected, which can be different from parentModuleName
        /// if "All" is selected
        /// </summary>
        private string selectedModuleName;

        /// <summary>
        /// Initializes a new instance of the ImportModuleEventArgs class.
        /// </summary>
        /// <param name="commandName">The name for the command needing help.</param>
        /// <param name="parentModuleName">The name of the module containing the command.</param>
        /// <param name="selectedModuleName">
        /// the name of the module that is selected, which can be different from parentModuleName
        /// if "All" is selected
        /// </param>
        public ImportModuleEventArgs(string commandName, string parentModuleName, string selectedModuleName)
        {
            this.commandName = commandName;
            this.parentModuleName = parentModuleName;
            this.selectedModuleName = selectedModuleName;
        }

        /// <summary>
        /// Gets the name for the command belonging to the module to be imported.
        /// </summary>
        public string CommandName
        {
            get { return this.commandName; }
        }

        /// <summary>
        /// Gets the module path or name for the module we want to import.
        /// </summary>
        public string ParentModuleName
        {
            get { return this.parentModuleName; }
        }

        /// <summary>
        /// Gets the name of the module that is selected, which can be different from parentModuleName
        /// if "All" is selected
        /// </summary>
        public string SelectedModuleName
        {
            get { return this.selectedModuleName; }
        }
    }
}
