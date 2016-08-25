/*
 * Copyright (c) 2010 Microsoft Corporation. All rights reserved
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using Microsoft.PowerShell.Commands;
using System.Activities;
using System.Collections.Concurrent;
using Microsoft.PowerShell.Activities;
using System.Management.Automation.Remoting;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// PSWorkflowConfigurationProvider 
    /// </summary>
    public class PSWorkflowConfigurationProvider
    {
        #region Public Methods
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public PSWorkflowConfigurationProvider()
        {
            _wfOptions = new PSWorkflowExecutionOption();
            LoadFromDefaults();
        }

        #endregion

        #region Private Members

        private string _configProviderId;
        private String _privateData;
        private readonly object _syncObject = new object();
        private bool _isPopulated;
        private const string PrivateDataToken = "PrivateData";
        private const string NameToken = "Name";
        private const string ParamToken = "Param";
        private const string ValueToken = "Value";

        internal const string PSDefaultActivities = "PSDefaultActivities";

        private PSWorkflowExecutionOption _wfOptions;

        internal const string TokenPersistencePath = "persistencepath";
        internal static readonly string DefaultPersistencePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\PowerShell\WF\PS");

        internal const string TokenPersistWithEncryption = "persistwithencryption";
        internal static readonly bool DefaultPersistWithEncryption = false;

        internal const string TokenMaxPersistenceStoreSizeGB = "maxpersistencestoresizegb";
        internal static readonly long DefaultMaxPersistenceStoreSizeGB = 10; // In GB

        internal const string TokenMaxRunningWorkflows = "maxrunningworkflows";
        internal static readonly int DefaultMaxRunningWorkflows = 30;
        internal const int MinMaxRunningWorkflows = 1;
        internal const int MaxMaxRunningWorkflows = int.MaxValue;

        internal const string TokenAllowedActivity = "allowedactivity";
        internal static readonly IEnumerable<string> DefaultAllowedActivity = new string[] { PSDefaultActivities };

        internal const string TokenOutOfProcessActivity = "outofprocessactivity";
        internal static readonly IEnumerable<string> DefaultOutOfProcessActivity = new string[] { "InlineScript" };

        internal const string TokenEnableValidation = "enablevalidation";
        internal static readonly bool DefaultEnableValidation = false;

        internal const string TokenMaxDisconnectedSessions = "maxdisconnectedsessions";
        internal static readonly int DefaultMaxDisconnectedSessions = 1000;
        internal const int MinMaxDisconnectedSessions = 1;
        internal const int MaxMaxDisconnectedSessions = int.MaxValue;

        internal const string TokenMaxConnectedSessions = "maxconnectedsessions";
        internal static readonly int DefaultMaxConnectedSessions = 100;
        internal const int MinMaxConnectedSessions = 1;
        internal const int MaxMaxConnectedSessions = int.MaxValue;

        internal const string TokenMaxSessionsPerWorkflow = "maxsessionsperworkflow";
        internal static readonly int DefaultMaxSessionsPerWorkflow = 5;
        internal const int MinMaxSessionsPerWorkflow = 1;
        internal const int MaxMaxSessionsPerWorkflow = int.MaxValue;

        internal const string TokenMaxSessionsPerRemoteNode = "maxsessionsperremotenode";
        internal static readonly int DefaultMaxSessionsPerRemoteNode = 5;
        internal const int MinMaxSessionsPerRemoteNode = 1;
        internal const int MaxMaxSessionsPerRemoteNode = int.MaxValue;

        internal const string TokenMaxActivityProcesses = "maxactivityprocesses";
        internal static readonly int DefaultMaxActivityProcesses = 5;
        internal const int MinMaxActivityProcesses = 1;
        internal const int MaxMaxActivityProcesses = int.MaxValue;

        internal const string TokenActivityProcessIdleTimeoutSec = "activityprocessidletimeoutsec";
        internal static readonly int DefaultActivityProcessIdleTimeoutSec = 60;
        internal const int MinActivityProcessIdleTimeoutSec = 1;
        internal const int MaxActivityProcessIdleTimeoutSec = int.MaxValue;

        internal const string TokenWorkflowApplicationPersistUnloadTimeoutSec = "workflowapplicationpersistunloadtimeoutsec";
        internal static readonly int DefaultWorkflowApplicationPersistUnloadTimeoutSec = 5;
        internal const int MinWorkflowApplicationPersistUnloadTimeoutSec = 0;
        internal const int MaxWorkflowApplicationPersistUnloadTimeoutSec = int.MaxValue;

        internal const string TokenWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = "wsmanpluginreportcompletiononzeroactivesessionswaitintervalmsec";
        internal static readonly int DefaultWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = 30 * 1000;
        internal const int MinWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = 100;
        internal const int MaxWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = int.MaxValue;

        internal const string TokenActivitiesCacheCleanupIntervalMSec = "activitiescachecleanupintervalmsec";
        internal static readonly int DefaultActivitiesCacheCleanupIntervalMSec = 5 * 60 * 1000;
        internal const int MinActivitiesCacheCleanupIntervalMSec = 100;
        internal const int MaxActivitiesCacheCleanupIntervalMSec = int.MaxValue;

        internal const string TokenRemoteNodeSessionIdleTimeoutSec = "remotenodesessionidletimeoutsec";
        internal static readonly int DefaultRemoteNodeSessionIdleTimeoutSec = 60;
        internal const int MinRemoteNodeSessionIdleTimeoutSec = 30;
        internal const int MaxRemoteNodeSessionIdleTimeoutSec = 30000;
        private int _remoteNodeSessionIdleTimeoutSec;

        internal const string TokenSessionThrottleLimit = "sessionthrottlelimit";
        internal static readonly int DefaultSessionThrottleLimit = 100;
        internal const int MinSessionThrottleLimit = 1;
        internal const int MaxSessionThrottleLimit = int.MaxValue;

        internal const string TokenValidationCacheLimit = "validationcachelimit";
        internal static readonly int DefaultValidationCacheLimit = 10000;
        private int _validationCacheLimit;

        internal const string TokenCompiledAssemblyCacheLimit = "compiledassemblycachelimit";
        internal static readonly int DefaultCompiledAssemblyCacheLimit = 10000;
        private int _compiledAssemblyCacheLimit;

        internal const string TokenOutOfProcessActivityCacheLimit = "outofprocessactivitycachelimit";
        internal static readonly int DefaultOutOfProcessActivityCacheLimit = 10000;
        private int _outOfProcessActivityCacheLimit;

        internal static readonly int DefaultPSPersistInterval = 30; // in seconds

        internal const string TokenWorkflowShutdownTimeoutMSec = "workflowshutdowntimeoutmsec";
        internal static readonly int DefaultWorkflowShutdownTimeoutMSec = 500; // 0.5 secs
        internal const int MinWorkflowShutdownTimeoutMSec = 0;
        internal const int MaxWorkflowShutdownTimeoutMSec = 5 * 1000; // 5 secs

        internal const string TokenMaxInProcRunspaces = "maxinprocrunspaces";
        internal static readonly int DefaultMaxInProcRunspaces = DefaultMaxRunningWorkflows * 2;
        private int _maxInProcRunspaces = DefaultMaxInProcRunspaces;

        private readonly ConcurrentDictionary<Type, bool> outOfProcessActivityCache = new ConcurrentDictionary<Type, bool>();

        #endregion Private Members

        #region Private Methods

        internal static PSWorkflowExecutionOption LoadConfig(string privateData, PSWorkflowConfigurationProvider configuration)
        {
            PSWorkflowExecutionOption target = new PSWorkflowExecutionOption();
            if (String.IsNullOrEmpty(privateData))
            {
                return target;
            }

            XmlReaderSettings readerSettings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = 10000,
                XmlResolver = null,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (XmlReader reader = XmlReader.Create(new StringReader(privateData), readerSettings))
            {
                // read the header <PrivateData>
                if (reader.ReadToFollowing(PrivateDataToken))
                {
                    HashSet<string> assignedParams = new HashSet<string>();
                    bool found = reader.ReadToDescendant(ParamToken);
                    while (found)
                    {
                        if (!reader.MoveToAttribute(NameToken))
                        {
                            throw new PSArgumentException(Resources.NameNotSpecifiedForParam);
                        }

                        string optionName = reader.Value;

                        if (!reader.MoveToAttribute(ValueToken))
                        {
                            throw new PSArgumentException(Resources.ValueNotSpecifiedForParam);
                        }

                        if (assignedParams.Contains(optionName.ToLower(CultureInfo.InvariantCulture)))
                        {
                            throw new PSArgumentException(Resources.ParamSpecifiedMoreThanOnce, optionName);
                        }

                        string optionValue = reader.Value;
                        Update(optionName, optionValue, target, configuration);

                        assignedParams.Add(optionName.ToLower(CultureInfo.InvariantCulture));
                        found = reader.ReadToFollowing(ParamToken);
                    }
                }
            }
            return target;
        }

        /// <summary>
        /// 
        /// </summary>
        public PSWorkflowRuntime Runtime
        {
            get;
            internal set;
        }

        private bool? _powerShellActivitiesAreAllowed;
        internal bool PSDefaultActivitiesAreAllowed
        {
            get
            {
                if (_powerShellActivitiesAreAllowed == null)
                {
					lock (_syncObject) {
						if (_powerShellActivitiesAreAllowed == null)
						{
							bool allowed = (AllowedActivity ?? new string[0]).Any(a => string.Equals(a, PSDefaultActivities, StringComparison.OrdinalIgnoreCase));
						    _powerShellActivitiesAreAllowed = allowed;
						}
					}
                }
                return _powerShellActivitiesAreAllowed.Value;
            }
        }

        /// <summary>
        /// Using optionName and optionValue updates the current object
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="value"></param>
        ///  <param name="target"></param>
        ///  <param name="configuration"></param>
        private static void Update(string optionName, string value, PSWorkflowExecutionOption target, PSWorkflowConfigurationProvider configuration)
        {
            switch (optionName.ToLower(CultureInfo.InvariantCulture))
            {
                case TokenWorkflowApplicationPersistUnloadTimeoutSec:
                    target.WorkflowApplicationPersistUnloadTimeoutSec = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec:
                    target.WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = (int)LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenActivitiesCacheCleanupIntervalMSec:
                    target.ActivitiesCacheCleanupIntervalMSec = (int)LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenActivityProcessIdleTimeoutSec:
                    target.ActivityProcessIdleTimeoutSec = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenAllowedActivity:
                    string[] activities = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    target.AllowedActivity = activities.Select(activity => activity.Trim()).ToArray();
                    break;
                case TokenEnableValidation:
                    bool enableValidation;
                    if (bool.TryParse(value, out enableValidation))
                    {
                        target.EnableValidation = enableValidation;
                    }
                    break;
                case TokenPersistencePath:
                    target.PersistencePath = Environment.ExpandEnvironmentVariables(value);
                    break;
                case TokenMaxPersistenceStoreSizeGB:
                    target.MaxPersistenceStoreSizeGB = (long)LanguagePrimitives.ConvertTo(value, typeof(long), CultureInfo.InvariantCulture);
                    break;
                case TokenPersistWithEncryption:
                    bool persistWithEncryption;
                    if (bool.TryParse(value, out persistWithEncryption))
                    {
                        target.PersistWithEncryption = persistWithEncryption;
                    }
                    break;
                case TokenRemoteNodeSessionIdleTimeoutSec:
                    target.RemoteNodeSessionIdleTimeoutSec = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    if (configuration != null)
                    {
                        configuration._remoteNodeSessionIdleTimeoutSec = target.RemoteNodeSessionIdleTimeoutSec;
                    }
                    break;
                case TokenMaxActivityProcesses:
                    target.MaxActivityProcesses = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenMaxConnectedSessions:
                    target.MaxConnectedSessions = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenMaxDisconnectedSessions:
                    target.MaxDisconnectedSessions = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenMaxRunningWorkflows:
                    target.MaxRunningWorkflows = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    configuration._maxInProcRunspaces = target.MaxRunningWorkflows*2;
                    break;
                case TokenMaxSessionsPerRemoteNode:
                    target.MaxSessionsPerRemoteNode = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenMaxSessionsPerWorkflow:
                    target.MaxSessionsPerWorkflow = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenOutOfProcessActivity:
                    string[] outofProcActivities = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    target.OutOfProcessActivity = outofProcActivities.Select(activity => activity.Trim()).ToArray();
                    break;

                case TokenSessionThrottleLimit:
                    {
                        target.SessionThrottleLimit = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    }
                    break;
                case TokenValidationCacheLimit:
                    if (configuration != null)
                    {
                        configuration._validationCacheLimit = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    }
                    break;
                case TokenCompiledAssemblyCacheLimit:
                    if (configuration != null)
                    {
                        configuration._compiledAssemblyCacheLimit = (int)LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    }
                    break;
                case TokenOutOfProcessActivityCacheLimit:
                    if (configuration != null)
                    {
                        configuration._outOfProcessActivityCacheLimit = (int) LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    }
                    break;
                case TokenWorkflowShutdownTimeoutMSec:
                    target.WorkflowShutdownTimeoutMSec = (int)LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.InvariantCulture);
                    break;
                case TokenMaxInProcRunspaces:
                    {
                        if (configuration != null)
                        {
                            configuration._maxInProcRunspaces =
                                (int) LanguagePrimitives.ConvertTo(value, typeof (int), CultureInfo.InvariantCulture);
                        }
                    }
                    break;
            }
        }

        #endregion Private Methods

        #region Internal Methods

        /// <summary>
        /// PSWorkflowConfigurationProvider
        /// </summary>
        /// <param name="applicationPrivateData"></param>
        /// <param name="configProviderId"></param>
        public PSWorkflowConfigurationProvider(string applicationPrivateData, string configProviderId) 
        {
            Populate(applicationPrivateData, configProviderId);
        }

        private void LoadFromDefaults()
        {
            _wfOptions.PersistencePath = DefaultPersistencePath;
            _wfOptions.MaxPersistenceStoreSizeGB = DefaultMaxPersistenceStoreSizeGB;
            _wfOptions.PersistWithEncryption = DefaultPersistWithEncryption;
            _wfOptions.MaxRunningWorkflows = DefaultMaxRunningWorkflows;
            _wfOptions.AllowedActivity = new List<string>(DefaultAllowedActivity).ToArray();
            _wfOptions.OutOfProcessActivity = new List<string>(DefaultOutOfProcessActivity).ToArray();
            _wfOptions.EnableValidation = DefaultEnableValidation;
            _wfOptions.MaxDisconnectedSessions = DefaultMaxDisconnectedSessions;
            _wfOptions.MaxConnectedSessions = DefaultMaxConnectedSessions;
            _wfOptions.MaxSessionsPerWorkflow = DefaultMaxSessionsPerWorkflow;
            _wfOptions.MaxSessionsPerRemoteNode = DefaultMaxSessionsPerRemoteNode;
            _wfOptions.MaxActivityProcesses = DefaultMaxActivityProcesses;
            _wfOptions.ActivityProcessIdleTimeoutSec = DefaultActivityProcessIdleTimeoutSec;
            _wfOptions.WorkflowApplicationPersistUnloadTimeoutSec = DefaultWorkflowApplicationPersistUnloadTimeoutSec;
            _wfOptions.WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = DefaultWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec;
            _wfOptions.ActivitiesCacheCleanupIntervalMSec = DefaultActivitiesCacheCleanupIntervalMSec;
            _wfOptions.RemoteNodeSessionIdleTimeoutSec = DefaultRemoteNodeSessionIdleTimeoutSec;
            _wfOptions.SessionThrottleLimit = DefaultSessionThrottleLimit;
            _validationCacheLimit = DefaultValidationCacheLimit;
            _compiledAssemblyCacheLimit = DefaultCompiledAssemblyCacheLimit;
            _outOfProcessActivityCacheLimit = DefaultOutOfProcessActivityCacheLimit;
            _wfOptions.WorkflowShutdownTimeoutMSec = DefaultWorkflowShutdownTimeoutMSec;

            ResetCaching();
        }

        private void ResetCaching()
        {
            _powerShellActivitiesAreAllowed = null;
            outOfProcessActivityCache.Clear();
        }

        internal PSSenderInfo _senderInfo = null;
        internal void Populate(string applicationPrivateData, string configProviderId, PSSenderInfo senderInfo)
        {
            _senderInfo = senderInfo;
            Populate(applicationPrivateData, configProviderId);
        }

        /// <summary>
        /// Populate the global configuration object with
        /// information from the configuration xml
        /// </summary>
        /// <param name="applicationPrivateData">private data
        /// associated with the endpoint</param>
        /// <param name="configProviderId"></param>
        public void Populate(string applicationPrivateData, string configProviderId)
        {
            if (_isPopulated)
            {
                return;
            }

            lock (_syncObject)
            {
                if (!_isPopulated)
                {
                    _privateData = applicationPrivateData;

                    string[] tokens = configProviderId.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    _configProviderId = tokens.Length > 0 ? tokens[tokens.Length - 1] : configProviderId;
                    _isPopulated = true;
                    _wfOptions = LoadConfig(_privateData, this);
                    ResetCaching();
                }
                else
                {
                    Debug.Assert(false, "Populate should only be called once");
                }
            }
        }

        #region PSWorkflowConfigurationProvider Implementation

        /// <summary>
        /// CreatePSActivityHostController
        /// </summary>
        /// <returns></returns>
        public virtual PSActivityHostController CreatePSActivityHostController()
        {
            return new PSOutOfProcessActivityController(Runtime);
        }

        /// <summary>
        /// CreatePSWorkflowInstance
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="metadata"></param>
        /// <param name="pipelineInput"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        public virtual PSWorkflowInstance CreatePSWorkflowInstance(PSWorkflowDefinition definition, PSWorkflowContext metadata, PSDataCollection<PSObject> pipelineInput, PSWorkflowJob job)
        {
            return new PSWorkflowApplicationInstance(Runtime, definition, metadata, pipelineInput, job);
        }

        /// <summary>
        /// CreatePSWorkflowInstance
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public virtual PSWorkflowInstance CreatePSWorkflowInstance(PSWorkflowId instanceId)
        {
            return new PSWorkflowApplicationInstance(Runtime, instanceId);
        }

        /// <summary>
        /// CreatePSWorkflowInstanceStore
        /// </summary>
        /// <param name="workflowInstance"></param>
        /// <returns></returns>
        public virtual PSWorkflowInstanceStore CreatePSWorkflowInstanceStore(PSWorkflowInstance workflowInstance)
        {
            return new PSWorkflowFileInstanceStore(this, workflowInstance);
        }

        /// <summary>
        /// CreateWorkflowExtensions
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<object> CreateWorkflowExtensions()
        {
            if (PSWorkflowExtensions.CustomHandler != null)
            {
                return PSWorkflowExtensions.CustomHandler();
            }

            return null;
        }

        /// <summary>
        /// CreateWorkflowExtensionCreationFunctions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the Workflow.")]
        public virtual IEnumerable<Func<T>> CreateWorkflowExtensionCreationFunctions<T>()
        {
            return null;
        }

        /// <summary>
        /// CreateRemoteRunspaceProvider
        /// </summary>
        /// <returns></returns>
        public virtual RunspaceProvider CreateRemoteRunspaceProvider()
        {
            return new ConnectionManager(RemoteNodeSessionIdleTimeoutSec * 1000,
                                              MaxSessionsPerRemoteNode,
                                              SessionThrottleLimit,
                                              MaxConnectedSessions,
                                              MaxDisconnectedSessions);
        }

        /// <summary>
        /// Local runspace provider
        /// </summary>
        /// <param name="isUnbounded"></param>
        /// <returns></returns>
        public virtual RunspaceProvider CreateLocalRunspaceProvider(bool isUnbounded)
        {
            if(isUnbounded)
                return new LocalRunspaceProvider(RemoteNodeSessionIdleTimeoutSec, LanguageMode);
            else
                return new LocalRunspaceProvider(RemoteNodeSessionIdleTimeoutSec, MaxInProcRunspaces, LanguageMode);
        }

        #endregion

        /// <summary>
        /// CurrentUserIdentity returns the current user 
        /// </summary>
        internal WindowsIdentity CurrentUserIdentity
        {
            get { return _currentIdentity ?? (_currentIdentity = WindowsIdentity.GetCurrent()); }
        }
        private WindowsIdentity _currentIdentity;

        /// <summary>
        /// To be called only by test code
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is called from the tests")]
        internal void ResetPopulate()
        {
            lock (_syncObject)
            {
                _isPopulated = false;
                LoadFromDefaults();
            }
        }

        #endregion Internal Methods

        #region Configuration Properties

        /// <summary>
        /// ConfigProviderId
        /// </summary>
        internal string ConfigProviderId
        {
            get
            {
                return _configProviderId;
            }
        }

        /// <summary>
        /// PersistencePath
        /// </summary>
        internal string PersistencePath
        {
            get
            {
                return _wfOptions.PersistencePath;
            }
        }

        internal bool IsDefaultStorePath;
        private string _instanceStorePath;
        internal string InstanceStorePath
        {
            get
            {
                if (_instanceStorePath != null)
                    return _instanceStorePath;

                lock (_syncObject)
                {
                    if (_instanceStorePath != null)
                        return _instanceStorePath;

                    if (CurrentUserIdentity.User != null)
                    {
                        string path = Path.Combine(_wfOptions.PersistencePath ?? DefaultPersistencePath, _configProviderId ?? "default", CurrentUserIdentity.User.Value);
                    
                        WindowsPrincipal principal = new WindowsPrincipal(CurrentUserIdentity);

                        bool elevated = false;
                        bool nonInteractive = false;
                        bool credssp = false;

                    
                        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                            elevated = true;

                        if (PSSessionConfigurationData.IsServerManager == false) // In order to avoid breaking change (SM+ might get affected)
                        {
                            if (!principal.IsInRole(new SecurityIdentifier(WellKnownSidType.InteractiveSid, null)))
                                nonInteractive = true;

                            if (_senderInfo != null &&
                                _senderInfo.UserInfo != null &&
                                _senderInfo.UserInfo.Identity != null &&
                                _senderInfo.UserInfo.Identity.AuthenticationType != null &&
                                _senderInfo.UserInfo.Identity.AuthenticationType.Equals("credssp", StringComparison.OrdinalIgnoreCase))
                            {
                                credssp = true;
                            }
                        }

                        if (credssp)
                        {
                            path += "_CP"; // the authentication type is credSSP
                        }
                        else
                        {
                            if (elevated)
                                path += "_EL"; // token is elevated

                            if (nonInteractive)
                                path += "_NI"; // token is not interactive
                        }

                        _instanceStorePath = path;
                    }
                    IsDefaultStorePath = string.IsNullOrEmpty(_configProviderId) ? true : false; 
                }
                return _instanceStorePath;
            }
        }

        /// <summary>
        /// MaxPersistenceStoreSizeGB
        /// </summary>
        internal long MaxPersistenceStoreSizeGB
        {
            get
            {
                return _wfOptions.MaxPersistenceStoreSizeGB;
            }
        }

        /// <summary>
        /// PersistWithEncryption
        /// </summary>
        internal bool PersistWithEncryption
        {
            get
            {
                return _wfOptions.PersistWithEncryption;
            }
        }

        /// <summary>
        /// MaxRunningWorkflows
        /// </summary>
        public virtual int MaxRunningWorkflows
        {
            get
            {
                return _wfOptions.MaxRunningWorkflows;
            }
        }

        /// <summary>
        /// AllowedActivity
        /// </summary>
        public virtual IEnumerable<string> AllowedActivity
        {
            get
            {
                return _wfOptions.AllowedActivity;
            }
        }

        /// <summary>
        /// OutOfProcActivity
        /// </summary>
        public virtual IEnumerable<string> OutOfProcessActivity
        {
            get
            {
                return _wfOptions.OutOfProcessActivity;
            }
        }


        /// <summary>
        /// EnableValidation
        /// </summary>
        public virtual bool EnableValidation
        {
            get
            {
                return _wfOptions.EnableValidation;
            }
        }

        /// <summary>
        /// MaxDisconnectedSessions
        /// </summary>
        public virtual int MaxDisconnectedSessions
        {
            get
            {
                return _wfOptions.MaxDisconnectedSessions;
            }
        }

        /// <summary>
        /// MaxConnectedSessions
        /// </summary>
        public virtual int MaxConnectedSessions
        {
            get
            {
                return _wfOptions.MaxConnectedSessions;
            }
        }

        /// <summary>
        /// MaxSessionsPerWorkflow
        /// </summary>
        internal int MaxSessionsPerWorkflow
        {
            get
            {
                return _wfOptions.MaxSessionsPerWorkflow;
            }
        }

        /// <summary>
        /// MaxSessionsPerRemoteNode
        /// </summary>
        public virtual int MaxSessionsPerRemoteNode
        {
            get
            {
                return _wfOptions.MaxSessionsPerRemoteNode;
            }
        }

        /// <summary>
        /// MaxActivityProcesses
        /// </summary>
        public virtual int MaxActivityProcesses
        {
            get
            {
                return _wfOptions.MaxActivityProcesses;
            }
        }

        /// <summary>
        /// WorkflowApplicationPersistUnloadTimeoutSec
        /// </summary>
        public virtual int PSWorkflowApplicationPersistUnloadTimeoutSec
        {
            get
            {
                return _wfOptions.WorkflowApplicationPersistUnloadTimeoutSec;
            }
        }

        /// <summary>
        /// WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec
        /// </summary>
        public virtual int WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec
        {
            get
            {
                return _wfOptions.WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec;
            }
        }

        /// <summary>
        /// ActivitiesCacheCleanupIntervalMSec
        /// </summary>
        public virtual int ActivitiesCacheCleanupIntervalMSec
        {
            get
            {
                return _wfOptions.ActivitiesCacheCleanupIntervalMSec;
            }
        }
        
        /// <summary>
        /// Local Machine Runspace Language Mode
        /// </summary>
        public virtual PSLanguageMode? LanguageMode
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// ActivityProcessIdleTimeOutSec
        /// </summary>
        public virtual int ActivityProcessIdleTimeoutSec
        {
            get
            {
                return _wfOptions.ActivityProcessIdleTimeoutSec;
            }
        }

        /// <summary>
        /// RemoteNodeSessionIdleTimeOutSec
        /// </summary>
        public virtual int RemoteNodeSessionIdleTimeoutSec
        {
            get
            {
                return _wfOptions.RemoteNodeSessionIdleTimeoutSec;
            }
        }

        /// <summary>
        /// SessionThrottleLimit
        /// </summary>
        public virtual int SessionThrottleLimit
        {
            get
            {
                return _wfOptions.SessionThrottleLimit;
            }
        }     

        internal int ValidationCacheLimit
        {
            get { return _validationCacheLimit; }
        }

        internal int CompiledAssemblyCacheLimit
        {
            get { return _compiledAssemblyCacheLimit; }
        }

        internal int OutOfProcessActivityCacheLimit
        {
            get
            {
                return _outOfProcessActivityCacheLimit;
            }
        }

        /// <summary>
        /// WorkflowShutdownTimeoutMSec - the maximum time allowed to suspend the workflows before aborting them.
        /// </summary>
        internal int WorkflowShutdownTimeoutMSec
        {
            get
            {
                return _wfOptions.WorkflowShutdownTimeoutMSec;
            }
        }

        /// <summary>
        /// MaxInProcRunspaces
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public virtual int MaxInProcRunspaces
        {
            get { return _maxInProcRunspaces; }
        }

        /// <summary>
        /// Get Activity Run Mode InProcess or Out of process
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public virtual ActivityRunMode GetActivityRunMode(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException("activity");

            if (outOfProcessActivityCache.Count >= OutOfProcessActivityCacheLimit)
                outOfProcessActivityCache.Clear();

            bool outOfProc = outOfProcessActivityCache.GetOrAdd(activity.GetType(), IsOutOfProcessActivity);
            return outOfProc ? ActivityRunMode.OutOfProcess : ActivityRunMode.InProcess;
        }

        private bool IsOutOfProcessActivity(Type activityType)
        {
            return (OutOfProcessActivity ?? new string[0]).Any(outOfProcessActivity => false || IsMatched(outOfProcessActivity, activityType.Name) || IsMatched(outOfProcessActivity, activityType.FullName) || IsMatched(outOfProcessActivity, activityType.Assembly.GetName().Name + "\\" + activityType.Name) || IsMatched(outOfProcessActivity, activityType.Assembly.GetName().Name + "\\" + activityType.FullName) || IsMatched(outOfProcessActivity, activityType.Assembly.GetName().FullName + "\\" + activityType.Name) || IsMatched(outOfProcessActivity, activityType.Assembly.GetName().FullName + "\\" + activityType.FullName));
        }

        private static bool IsMatched(string allowedActivity, string match)
        {
            return (WildcardPattern.ContainsWildcardCharacters(allowedActivity)
                ? new WildcardPattern(allowedActivity, WildcardOptions.IgnoreCase).IsMatch(match)
                : string.Equals(allowedActivity, match, StringComparison.OrdinalIgnoreCase));
        }
    
        #endregion Configuration Properties
    }
}
