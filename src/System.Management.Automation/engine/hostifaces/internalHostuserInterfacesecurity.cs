// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal.Host
{
    internal partial
    class InternalHostUserInterface : PSHostUserInterface
    {
        /// <summary>
        /// See base class.
        /// </summary>

        public override
        PSCredential
        PromptForCredential
        (
            string caption,
            string message,
            string userName,
            string targetName
        )
        {
            return PromptForCredential(caption, message, userName,
                                         targetName,
                                         PSCredentialTypes.Default,
                                         PSCredentialUIOptions.Default);
        }

        /// <summary>
        /// See base class.
        /// </summary>

        public override
        PSCredential
        PromptForCredential
        (
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options
        )
        {
            if (_externalUI == null)
            {
                ThrowPromptNotInteractive(message);
            }

            PSCredential result = null;
            try
            {
                result = _externalUI.PromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options);
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }
    }
}

