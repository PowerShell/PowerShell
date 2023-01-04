// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the Format-List command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "List", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096928")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
    public class FormatListCommand : OuterFormatTableAndListBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatListCommand"/> class
        /// and sets the inner command.
        /// </summary>
        public FormatListCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.List);
        }
    }
}
