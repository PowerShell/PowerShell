// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the format-default command.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Default")]
    public class FormatDefaultCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Constructor to set the inner command.
        /// </summary>
        public FormatDefaultCommand()
        {
            this.implementation = new InnerFormatShapeCommand(FormatShape.Undefined);
        }

        /// <summary>
        /// Hook up the AutoSize override, if requested, before handing off to the
        /// implementation.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (PSStyle.Instance.AutoSizeDefaultFormatting)
            {
                var parameters = new FormattingCommandLineParameters { autosize = true };
                ((InnerFormatShapeCommand)this.implementation).SetCommandLineParameters(parameters);
            }

            base.BeginProcessing();
        }
    }
}
