// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Arguments for the event triggered when something happens at the cmdlet level.
    /// </summary>
    public class CommandEventArgs : EventArgs
    {
        /// <summary>
        /// the command targeted by the event.
        /// </summary>
        private CommandViewModel command;

        /// <summary>
        /// Initializes a new instance of the CommandEventArgs class.
        /// </summary>
        /// <param name="command">The command targeted by the event.</param>
        public CommandEventArgs(CommandViewModel command)
        {
            this.command = command;
        }

        /// <summary>
        /// Gets the command targeted by the event.
        /// </summary>
        public CommandViewModel Command
        {
            get { return this.command; }
        }
    }
}
