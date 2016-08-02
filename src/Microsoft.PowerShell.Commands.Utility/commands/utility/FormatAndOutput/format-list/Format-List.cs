/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// implementation for the format-table command
    /// </summary>
    [Cmdlet("Format", "List", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113302")]
    public class FormatListCommand : OuterFormatTableAndListBase
    {
        /// <summary>
        /// constructor to set the inner command
        /// </summary>
        public FormatListCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.List);
        }
    }
}

