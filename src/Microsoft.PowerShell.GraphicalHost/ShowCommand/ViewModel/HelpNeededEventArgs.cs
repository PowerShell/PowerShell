//-----------------------------------------------------------------------
// <copyright file="HelpNeededEventArgs.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System;

    /// <summary>
    /// Arguments for the event triggered when it is necessary to display help for a command
    /// </summary>
    public class HelpNeededEventArgs : EventArgs
    {
        /// <summary>
        /// the name for the command needing help
        /// </summary>
        private string commandName;

        /// <summary>
        /// Initializes a new instance of the HelpNeededEventArgs class.
        /// </summary>
        /// <param name="commandName">the name for the command needing help</param>
        public HelpNeededEventArgs(string commandName)
        {
            this.commandName = commandName;
        }

        /// <summary>
        /// Gets the name for the command needing help
        /// </summary>
        public string CommandName
        {
            get { return this.commandName; }
        }
    }
}
