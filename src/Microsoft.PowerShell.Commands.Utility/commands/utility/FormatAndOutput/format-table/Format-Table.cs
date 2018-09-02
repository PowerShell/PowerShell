// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Table", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113303")]
    public class FormatTableCommand : OuterFormatTableBase
    {
        /// <summary>
        /// Constructor to set the inner command.
        /// </summary>
        public FormatTableCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Table);
        }
    }
}

