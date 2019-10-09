// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "List", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113302")]
    public class FormatListCommand : OuterFormatTableAndListBase
    {
        /// <summary>
        /// Constructor to set the inner command.
        /// </summary>
        public FormatListCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.List);
        }
    }
}

