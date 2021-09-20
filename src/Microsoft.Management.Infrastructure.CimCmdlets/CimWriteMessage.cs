// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using Microsoft.Management.Infrastructure.Options;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Write message to message channel
    /// </para>
    /// </summary>
    internal sealed class CimWriteMessage : CimBaseAction
    {
        #region members

        /// <summary>
        /// Channel id.
        /// </summary>
        #endregion

        #region Properties

        internal uint Channel { get; }

        internal string Message { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CimWriteMessage"/> class.
        /// </summary>
        public CimWriteMessage(uint channel,
            string message)
        {
            this.Channel = channel;
            this.Message = message;
        }

        /// <summary>
        /// <para>
        /// Write message to the target channel
        /// </para>
        /// </summary>
        /// <param name="cmdlet"></param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");

            switch ((CimWriteMessageChannel)Channel)
            {
                case CimWriteMessageChannel.Verbose:
                    cmdlet.WriteVerbose(Message);
                    break;
                case CimWriteMessageChannel.Warning:
                    cmdlet.WriteWarning(Message);
                    break;
                case CimWriteMessageChannel.Debug:
                    cmdlet.WriteDebug(Message);
                    break;
                default:
                    break;
            }
        }
    }
}
