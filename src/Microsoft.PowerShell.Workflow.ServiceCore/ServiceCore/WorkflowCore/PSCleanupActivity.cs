/*
 * Copyright (c) 2011 Microsoft Corporation. All rights reserved
 */
using System;
using System.Activities;
using System.Threading;
using System.Management.Automation.Runspaces;
using System.ComponentModel;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Base activity for cleanup activities
    /// </summary>
    public abstract class PSCleanupActivity : PSRemotingActivity
    {
        /// <summary>
        /// Creates and returns an empty powershell
        /// </summary>
        /// <param name="context">activity context</param>
        /// <returns>A new activity implementation context</returns>
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            ActivityImplementationContext implementationContext = new ActivityImplementationContext
                                                                      {
                                                                          PowerShellInstance =
                                                                              System.Management.Automation.PowerShell.
                                                                              Create()
                                                                      };

            return implementationContext;
        }

        /// <summary>
        /// Method that needs to be overridden to perform the actual
        /// cleanup action
        /// </summary>
        /// <param name="args">RunCommandsArguments</param>
        /// <param name="callback">callback to call when cleanup 
        /// is done</param>
        /// <remarks>The signature forces this method to be internal</remarks>
        internal virtual void DoCleanup(RunCommandsArguments args, WaitCallback callback)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Activity to cleanup all Runspaces (PSRP connections) to
    /// a remote machine
    /// </summary>
    public class DisablePSWorkflowConnection : PSCleanupActivity
    {
        // Arguments

        /// <summary>
        /// Provides access to the Authentication parameter.
        /// </summary>
        [RequiredArgument]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public InArgument<int> TimeoutSec
        {
            get;
            set;
        }
        private int timeout = DefaultCleanupWaitTimerIntervalMs;
        private const int DefaultCleanupWaitTimerIntervalMs = 5*60*1000;

        /// <summary>
        /// Set the display name of the activity
        /// </summary>
        public DisablePSWorkflowConnection()
        {
            DisplayName = "Disable-PSWorkflowConnection";
        }

        /// <summary>
        /// Creates and returns an empty powershell
        /// </summary>
        /// <param name="context">activity context</param>
        /// <returns>A new activity implementation context</returns>
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            if (TimeoutSec.Expression != null)
            {
                timeout = TimeoutSec.Get(context);
            }

            return base.GetPowerShell(context);
        }

        /// <summary>
        /// Method that needs to be overridden to perform the actual
        /// cleanup action
        /// </summary>
        /// <param name="args">RunCommandsArguments</param>
        /// <param name="callback">callback to call when cleanup 
        /// is done</param>
        /// <remarks>The signature forces this method to be internal</remarks>
        internal override void DoCleanup(RunCommandsArguments args, WaitCallback callback)
        {
            PSWorkflowHost workflowHost = args.WorkflowHost;
            WSManConnectionInfo connectionInfo =
                args.ImplementationContext.PowerShellInstance.Runspace.ConnectionInfo as WSManConnectionInfo;

            args.CleanupTimeout = timeout;

            if (connectionInfo == null)
            {
                if (callback != null)
                    callback(args);
            }
            else
            {
                workflowHost.RemoteRunspaceProvider.RequestCleanup(connectionInfo, callback, args);
            }
        }
    }
}
