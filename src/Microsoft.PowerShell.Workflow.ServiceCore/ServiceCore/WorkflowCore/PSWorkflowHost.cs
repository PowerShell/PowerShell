/*
 * Copyright (c) 2010 Microsoft Corporation. All rights reserved
 */
using System;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{

    #region PSWorkflowHost

    /// <summary>
    /// Interface that defines the PowerShell workflow host
    /// Workflow host defines the set of services that are
    /// made available to an activity
    /// </summary>
    public abstract class PSWorkflowHost
    {
        /// <summary>
        /// The activity host manager to use for processing
        /// activities
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSActivityHostController PSActivityHostController
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The provider to be used for obtaining runspaces
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual RunspaceProvider RemoteRunspaceProvider
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The provider used to obtain local in-proc
        /// runspaces
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual RunspaceProvider LocalRunspaceProvider
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The provider which will supply an unbounded number of
        /// runspaces - to be used in PowerShell value
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual RunspaceProvider UnboundedLocalRunspaceProvider
        {
            get { throw new NotImplementedException(); }
        }
    }

    #endregion PSWorkflowHost


    #region RunspaceProvider

    /// <summary>
    /// Class that provides a runspace with the specified options
    /// and constraints
    /// </summary>
    public abstract class RunspaceProvider
    {
        /// <summary>
        /// Begin for obtaining a runspace for the specified ConnectionInfo
        /// </summary>
        /// <param name="connectionInfo">connection info to be used for remote connections</param>
        /// <param name="retryCount">number of times to retry</param>
        /// <param name="callback">optional user defined callback</param>
        /// <param name="state">optional user specified state</param>
        /// <param name="retryInterval">time in milliseconds before the next retry has to be attempted</param>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>async result</returns>
        public virtual IAsyncResult BeginGetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// End for obtaining a runspace for the specified connection info
        /// </summary>
        /// <param name="asyncResult">async result to end on</param>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>remote runspace to invoke commands on</returns>
        public virtual Runspace EndGetRunspace(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get runspace for the specified connection info to be
        /// used for running commands
        /// </summary>
        /// <param name="connectionInfo">connection info to use</param>
        /// <param name="retryCount">retry count </param>
        /// <param name="retryInterval">retry interval in ms</param>
        /// <returns>remote runspace to use</returns>
        public virtual Runspace GetRunspace(WSManConnectionInfo connectionInfo, uint retryCount, uint retryInterval)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Release the runspace once the activity is finished using the same
        /// </summary>
        /// <param name="runspace">runspace to release</param>
        public virtual void ReleaseRunspace(Runspace runspace)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Callback to indicate that this runspace been initiated with
        /// a pipeline and can be disconnected
        /// </summary>
        /// <param name="runspace">runspace that needs to be marked as
        /// ready for disconnect</param>
        public virtual void ReadyForDisconnect(Runspace runspace)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request a cleanup to the destination specified in the
        /// connection info. This means no runspaces will be held
        /// to the specified connection info.
        /// </summary>
        /// <param name="connectionInfo">connection info to which
        /// cleanup is desired</param>
        ///<param name="callback">callback to invoke</param>
        /// <param name="state">caller specified state</param>
        public virtual void RequestCleanup(WSManConnectionInfo connectionInfo, WaitCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks to see if the provider intentionally disconnected a runspace
        /// or it went into disconnected state due to network issues
        /// </summary>
        /// <param name="runspace">runspace that needs to be checked</param>
        /// <returns>true - when intentionally disconnected
        ///          false - disconnected due to network issues</returns>
        public virtual bool IsDisconnectedByRunspaceProvider(Runspace runspace)
        {
            throw new NotImplementedException();
        }
    }

    #endregion RunspaceProvider


    #region DefaultWorkflowHost

    /// <summary>
    /// Default workflow host implementation
    /// </summary>
    internal class DefaultWorkflowHost : PSWorkflowHost
    {
        private readonly PSActivityHostController _activityHostController;
        private readonly RunspaceProvider _runspaceProvider;
        private static readonly DefaultWorkflowHost _instance = new DefaultWorkflowHost();
        private readonly RunspaceProvider _localRunspaceProvider;

        private DefaultWorkflowHost()
        {
            const string applicationPrivateData = @"
                <PrivateData>
                    <Param Name='AllowedActivity' Value='PSDefaultActivities' />
                </PrivateData>
";
            var runtime = PSWorkflowRuntime.Instance;
            runtime.Configuration.Populate(applicationPrivateData, "Microsoft.PowerShell.Workflow");
            _activityHostController = runtime.PSActivityHostController;
            _runspaceProvider = runtime.RemoteRunspaceProvider;
            _localRunspaceProvider = runtime.LocalRunspaceProvider;
        }

        /// <summary>
        /// The activity host manager to use for processing
        /// activities
        /// </summary>
        public override PSActivityHostController PSActivityHostController
        {
            get
            {
                return _activityHostController;
            }
        }

        /// <summary>
        /// The provider to be used for obtaining runspaces
        /// </summary>
        public override RunspaceProvider RemoteRunspaceProvider
        {
            get { return _runspaceProvider; }
        }

        /// <summary>
        ///
        /// </summary>
        public override RunspaceProvider LocalRunspaceProvider
        {
            get
            {
                return _localRunspaceProvider;
            }
        }

        /// <summary>
        /// return the singleton instance
        /// </summary>
        internal static DefaultWorkflowHost Instance
        {
            get { return _instance; }
        }

        internal void ResetLocalRunspaceProvider()
        {
            MethodInfo methodInfo = _localRunspaceProvider.GetType().GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo.Invoke(_localRunspaceProvider, new object[]{});
        }
    }

    #endregion DefaultWorkflowHost

}
