/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// implementation for the format-default command
    /// </summary>
    [Cmdlet("Format", "Default")]
    public class FormatDefaultCommand : FrontEndCommandBase
    {
        /// <summary>
        /// constructor to set the inner command
        /// </summary>
        public FormatDefaultCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Undefined);
        }
    }
}


