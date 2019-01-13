// Copyright (c) Microsoft Corporation. All rights reserved.
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
        private UInt32 channel;

        /// <summary>
        /// Message to write to the channel.
        /// </summary>
        private string message;
        #endregion

        #region Properties

        internal UInt32 Channel
        {
            get { return channel; }
        }

        internal string Message
        {
            get { return message; }
        }

        #endregion

        /// <summary>
        /// Constructor method.
        /// </summary>
        public CimWriteMessage(UInt32 channel,
            string message)
        {
            this.channel = channel;
            this.message = message;
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

            switch ((CimWriteMessageChannel)channel)
            {
                case CimWriteMessageChannel.Verbose:
                    cmdlet.WriteVerbose(message);
                    break;
                case CimWriteMessageChannel.Warning:
                    cmdlet.WriteWarning(message);
                    break;
                case CimWriteMessageChannel.Debug:
                    cmdlet.WriteDebug(message);
                    break;
                default:
                    break;
            }
        }
    }
}
