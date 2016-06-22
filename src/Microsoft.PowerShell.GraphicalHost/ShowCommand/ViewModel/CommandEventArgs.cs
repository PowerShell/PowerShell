//-----------------------------------------------------------------------
// <copyright file="CommandEventArgs.cs" company="Microsoft">
//     Copyright © Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    using System;

    /// <summary>
    /// Arguments for the event triggered when something happens at the cmdlet level
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        /// <summary>
        /// the command targeted by the event
        /// </summary>
        private CommandViewModel command;

        /// <summary>
        /// Initializes a new instance of the CommandEventArgs class.
        /// </summary>
        /// <param name="command">the command targeted by the event</param>
        public CommandEventArgs(CommandViewModel command)
        {
            this.command = command;
        }

        /// <summary>
        /// Gets the command targeted by the event
        /// </summary>
        public CommandViewModel Command
        {
            get { return this.command; }
        }
    }
}