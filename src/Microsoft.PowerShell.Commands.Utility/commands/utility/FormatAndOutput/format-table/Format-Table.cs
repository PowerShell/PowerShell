// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Table", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096703")]
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

