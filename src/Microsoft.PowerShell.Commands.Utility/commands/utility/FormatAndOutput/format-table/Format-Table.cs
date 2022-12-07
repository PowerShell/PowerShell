// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the Format-Table command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Table", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096703")]
    [OutputType(typeof(FormatStartData), typeof(FormatEntryData), typeof(FormatEndData), typeof(GroupStartData), typeof(GroupEndData))]
    public class FormatTableCommand : OuterFormatTableBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatTableCommand"/> class
        /// and sets the inner command.
        /// </summary>
        public FormatTableCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Table);
        }
    }
}
