/*
 * Copyright (c) 2010 Microsoft Corporation. All rights reserved
 */

using Microsoft.PowerShell.Activities;
using System.Management.Automation.PerformanceData;
using System.Management.Automation.Tracing;
using System.Threading;
using System;
using System.Management.Automation;
using System.Globalization;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// PowerShell Workflow host runtime.
    /// </summary>
    public class PSWorkflowRuntime : PSWorkflowHost, IDisposable
    {
        private static PSWorkflowRuntime powerShellWorkflowHostInstance;
        private static readonly object syncLock = new object();
        private RunspaceProvider _runspaceProvider;
        private RunspaceProvider _localRunspaceProvider;
        private RunspaceProvider _unboundedLocalRunspaceProvider;

	    private static readonly Tracer _tracer = new Tracer();
        private static readonly PSPerfCountersMgr _psPerfCountersMgrInst = PSPerfCountersMgr.Instance;
        private readonly PSWorkflowConfigurationProvider _configuration;
        private readonly object _syncObject = new object();

        private PSWorkflowJobManager jobManager;
        private PSActivityHostController activityHostController;

        private bool _isDisposed;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PSWorkflowRuntime()
        {
            _configuration = new PSWorkflowConfigurationProvider();
            _configuration.Runtime = this;
            PSCounterSetRegistrar registrar = 
                new PSCounterSetRegistrar(
                    PSWorkflowPerformanceCounterSetInfo.ProviderId,
                    PSWorkflowPerformanceCounterSetInfo.CounterSetId,
                    PSWorkflowPerformanceCounterSetInfo.CounterSetType,
                    PSWorkflowPerformanceCounterSetInfo.CounterInfoArray);
            _psPerfCountersMgrInst.AddCounterSetInstance(registrar);

            // Enable caching module paths appdomain-wide.
            System.Management.Automation.PSModuleInfo.UseAppDomainLevelModuleCache = true;
        }

        /// <summary>
        /// Constructs runtime based on configuration
        /// </summary>
        /// <param name="configuration"></param>
        public PSWorkflowRuntime(PSWorkflowConfigurationProvider configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            // Only allow FullLanguage or ConstraintLanguage or it can be null in that case system wide default will take an affect
            PSLanguageMode? langMode = configuration.LanguageMode;
            if (langMode != null && langMode.HasValue && (langMode.Value == PSLanguageMode.NoLanguage || langMode.Value == PSLanguageMode.RestrictedLanguage))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.NotSupportedLanguageMode, langMode.Value.ToString()));
            }

            _configuration = configuration;
            _configuration.Runtime = this;
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            lock (_syncObject)
            {
                if (_isDisposed)
                    return;

                // Suspend all running jobs then dispose all runspace providers
                if (jobManager != null)
                    jobManager.Dispose();
                jobManager = null;

                var remoteRunspaceProvider = _runspaceProvider as ConnectionManager;
                if (remoteRunspaceProvider != null)
                    remoteRunspaceProvider.Dispose();
                _runspaceProvider = null;

                var localRunspaceProvider = _localRunspaceProvider as LocalRunspaceProvider;
                if (localRunspaceProvider != null)
                    localRunspaceProvider.Dispose();
                _localRunspaceProvider = null;

                var unboundedLocalRunspaceProvider = _localRunspaceProvider as LocalRunspaceProvider;
                if (unboundedLocalRunspaceProvider != null)
                    unboundedLocalRunspaceProvider.Dispose();
                _unboundedLocalRunspaceProvider = null;

                activityHostController = null;

                _isDisposed = true;
            }
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("PSWorkflowRuntime");
            }
        }

        internal static PSWorkflowRuntime Instance
        {
            get
            {
                if (powerShellWorkflowHostInstance == null)
                {
                    lock (syncLock)
                    {
                        if (powerShellWorkflowHostInstance == null)
                        {
                            powerShellWorkflowHostInstance = new PSWorkflowRuntime();
                        }
                    }
                }
                return powerShellWorkflowHostInstance;
            }
        }

        internal Tracer Tracer
        {
            get
            {
                return _tracer;
            }
        }

        #region Test Helpers

        internal void SetWorkflowJobManager(PSWorkflowJobManager wfJobManager)
        {
            lock (syncLock)
            {
                jobManager = wfJobManager;
            }
        }

        #endregion Test Helpers

        /// <summary>
        /// The runtime configuration.
        /// </summary>
        public virtual PSWorkflowConfigurationProvider Configuration
        {
            get
            {
                return _configuration;
            }
        }

        /// <summary>
        /// JobManager
        /// </summary>
        public virtual PSWorkflowJobManager JobManager
        {
            get
            {
                if (jobManager != null)
                    return jobManager;

                lock (syncLock)
                {
                    if (jobManager != null)
                        return jobManager;

                    AssertNotDisposed();
                    jobManager = new PSWorkflowJobManager(this, _configuration.MaxRunningWorkflows);
                }
                return jobManager;
            }
        }


        /// <summary>
        /// PSActivityHostController (PSWorkflowHost)
        /// </summary>
        public override PSActivityHostController PSActivityHostController
        {
            get
            {
                if (activityHostController != null)
                    return activityHostController;

                lock (syncLock)
                {
                    if (activityHostController != null)
                        return activityHostController;

                    AssertNotDisposed();
                    activityHostController = _configuration.CreatePSActivityHostController();
                }
                return activityHostController;
            }
        }

        /// <summary>
        /// RemoteRunspaceProvider (PSWorkflowHost)
        /// </summary>
        public override RunspaceProvider RemoteRunspaceProvider
        {
            get
            {
                if (_runspaceProvider == null)
                {
                    lock (_syncObject)
                    {
                        if (_runspaceProvider == null)
                        {
                            AssertNotDisposed();
                            _runspaceProvider = _configuration.CreateRemoteRunspaceProvider();
                        }
                    }
                }
                return _runspaceProvider;
            }
        }

        /// <summary>
        /// LocalRunspaceProvider (PSWorkflowHost)
        /// </summary>
        public override RunspaceProvider LocalRunspaceProvider
        {
            get
            {
                if (_localRunspaceProvider == null)
                {
                    lock (_syncObject)
                    {
                        if (_localRunspaceProvider == null)
                        {
                            AssertNotDisposed();
                            _localRunspaceProvider = _configuration.CreateLocalRunspaceProvider(false);
                        }
                    }
                }

                return _localRunspaceProvider;
            }
        }

        /// <summary>
        /// The provider which will supply an unbounded number of
        /// runspaces - to be used in PowerShell value
        /// </summary>
        public override RunspaceProvider UnboundedLocalRunspaceProvider
        {
            get
            {
                if (_unboundedLocalRunspaceProvider == null)
                {
                    lock(_syncObject)
                    {
                        if (_unboundedLocalRunspaceProvider == null)
                        {
                            AssertNotDisposed();
                            _unboundedLocalRunspaceProvider = _configuration.CreateLocalRunspaceProvider(true);
                        }
                    }
                }

                return _unboundedLocalRunspaceProvider;
            }
        }
    }
}
