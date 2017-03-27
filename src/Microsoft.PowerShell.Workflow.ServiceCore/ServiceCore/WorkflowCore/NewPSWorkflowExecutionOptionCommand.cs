//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Management.Automation;
using System.Collections;
using System.Xml;
using Microsoft.PowerShell.Workflow;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class is used to specify Workflow related options in the Register-PSSessionConfiguration
    /// </summary>
    public sealed class PSWorkflowExecutionOption : PSSessionTypeOption 
    {
        private const string PrivateDataFormat = @"<PrivateData>{0}</PrivateData>";
        private const string ParamToken = @"<Param Name='{0}' Value='{1}' />";

        private string persistencePath = PSWorkflowConfigurationProvider.DefaultPersistencePath;
        private long maxPersistenceStoreSizeGB = PSWorkflowConfigurationProvider.DefaultMaxPersistenceStoreSizeGB;
        private bool persistWithEncryption = PSWorkflowConfigurationProvider.DefaultPersistWithEncryption;
        private int maxRunningWorkflows = PSWorkflowConfigurationProvider.DefaultMaxRunningWorkflows;
        private string[] allowedActivity = new List<string>(PSWorkflowConfigurationProvider.DefaultAllowedActivity).ToArray();
        private bool enableValidation = PSWorkflowConfigurationProvider.DefaultEnableValidation;
        private string[] outOfProcessActivity = new List<string>(PSWorkflowConfigurationProvider.DefaultOutOfProcessActivity).ToArray();
        private int maxDisconnectedSessions = PSWorkflowConfigurationProvider.DefaultMaxDisconnectedSessions;
        private int maxConnectedSessions = PSWorkflowConfigurationProvider.DefaultMaxConnectedSessions;
        private int maxSessionsPerWorkflow = PSWorkflowConfigurationProvider.DefaultMaxSessionsPerWorkflow;
        private int maxSessionsPerRemoteNode = PSWorkflowConfigurationProvider.DefaultMaxSessionsPerRemoteNode;
        private int maxActivityProcesses = PSWorkflowConfigurationProvider.DefaultMaxActivityProcesses;
        private int activityProcessIdleTimeoutSec = PSWorkflowConfigurationProvider.DefaultActivityProcessIdleTimeoutSec;
        private int workflowApplicationPersistUnloadTimeoutSec = PSWorkflowConfigurationProvider.DefaultWorkflowApplicationPersistUnloadTimeoutSec;
        private int wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = PSWorkflowConfigurationProvider.DefaultWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec;
        private int activitiesCacheCleanupIntervalMSec = PSWorkflowConfigurationProvider.DefaultActivitiesCacheCleanupIntervalMSec;
        private int remoteNodeSessionIdleTimeoutSec = PSWorkflowConfigurationProvider.DefaultRemoteNodeSessionIdleTimeoutSec;
        private int sessionThrottleLimit = PSWorkflowConfigurationProvider.DefaultSessionThrottleLimit;
        private int workflowShutdownTimeoutMSec = PSWorkflowConfigurationProvider.DefaultWorkflowShutdownTimeoutMSec;

        /// <summary>
        /// Constructor that loads from default values
        /// </summary>
        internal PSWorkflowExecutionOption()
        {
        }

        private void ValidateRange(int min, int max, int value)
        {
            if (value >= min && value <= max)
                return;

                    string msg = string.Format(CultureInfo.InvariantCulture, Resources.ProvidedValueIsOutOfRange, value, min, max);
                    throw new ArgumentException(msg);
                }

        /// <summary>
        /// SessionThrottleLimit
        /// </summary>
        public int SessionThrottleLimit
        {
            get
            {
                return sessionThrottleLimit;
            }
            set {
                ValidateRange(PSWorkflowConfigurationProvider.MinSessionThrottleLimit, PSWorkflowConfigurationProvider.MaxSessionThrottleLimit, value);
                sessionThrottleLimit = value;
            }
        }

        /// <summary>
        /// PersistencePath
        /// </summary>
        public string PersistencePath
        {
            get
            {
                return persistencePath;
            }
            set
            {
                persistencePath = value;
            }
        }

        /// <summary>
        /// MaxPersistenceStoreSizeGB
        /// </summary>
        public long MaxPersistenceStoreSizeGB
        {
            get
            {
                return maxPersistenceStoreSizeGB;
            }
            set
            {
                maxPersistenceStoreSizeGB = value;
            }
        }
        
        /// <summary>
        /// PersistWithEncryption
        /// </summary>
        public bool PersistWithEncryption
        {
            get {
                return persistWithEncryption;
            }
            set
            {
                persistWithEncryption = value;
            }
        }

        /// <summary>
        /// MaxRunningWorkflows
        /// </summary>
        public int MaxRunningWorkflows {
            get
            {
                return maxRunningWorkflows;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxRunningWorkflows, PSWorkflowConfigurationProvider.MaxMaxRunningWorkflows, value);
                maxRunningWorkflows = value;
            }
        }

        /// <summary>
        /// AllowedActivity
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AllowedActivity
        {
            get
            {
                return allowedActivity;
            }
            set
            {
                allowedActivity = value;
            }
        }

        /// <summary>
        /// OutOfProcActivity
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OutOfProcessActivity
        {
            get
            {
                return outOfProcessActivity;
            }
            set
            {
                outOfProcessActivity = value;
            }
        }


        /// <summary>
        /// EnableValidation
        /// </summary>
        public bool EnableValidation
        {
            get
            {
                 return enableValidation;
            }
            set
            {
                 enableValidation = value;
            }
        }

        /// <summary>
        /// MaxDisconnectedSessions
        /// </summary>
        public int MaxDisconnectedSessions
        {
            get
            {
                return maxDisconnectedSessions;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxDisconnectedSessions, PSWorkflowConfigurationProvider.MaxMaxDisconnectedSessions, value);
                maxDisconnectedSessions = value;
            }
        }

        /// <summary>
        /// MaxConnectedSessions
        /// </summary>
        public int MaxConnectedSessions
        {
            get
            {
                return maxConnectedSessions;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxConnectedSessions, PSWorkflowConfigurationProvider.MaxMaxConnectedSessions, value);
                maxConnectedSessions = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerWorkflow
        /// </summary>
        public int MaxSessionsPerWorkflow
        {
            get
            {
                return maxSessionsPerWorkflow;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxSessionsPerWorkflow, PSWorkflowConfigurationProvider.MaxMaxSessionsPerWorkflow, value);
                maxSessionsPerWorkflow = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerRemoteNode
        /// </summary>
        public int MaxSessionsPerRemoteNode
        {
            get
            {
                return maxSessionsPerRemoteNode;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxSessionsPerRemoteNode, PSWorkflowConfigurationProvider.MaxMaxSessionsPerRemoteNode, value);
                maxSessionsPerRemoteNode = value;
            }
        }

        /// <summary>
        /// MaxActivityProcesses
        /// </summary>
        public int MaxActivityProcesses
        {
            get
            {
                return maxActivityProcesses;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinMaxActivityProcesses, PSWorkflowConfigurationProvider.MaxMaxActivityProcesses, value);
                maxActivityProcesses = value;
            }
        }

        /// <summary>
        /// WorkflowApplicationPersistUnloadTimeoutSec
        /// </summary>
        internal int WorkflowApplicationPersistUnloadTimeoutSec
        {
            get
            {
                return workflowApplicationPersistUnloadTimeoutSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinWorkflowApplicationPersistUnloadTimeoutSec, PSWorkflowConfigurationProvider.MaxWorkflowApplicationPersistUnloadTimeoutSec, value);
                workflowApplicationPersistUnloadTimeoutSec = value;
            }
        }
                
        /// <summary>
        /// WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec
        /// </summary>
        internal int WSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec
        {
            get
            {
                return wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec, PSWorkflowConfigurationProvider.MaxWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec, value);
                wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = value;
            }
        }
                
        /// <summary>
        /// ActivitiesCacheCleanupIntervalMSec
        /// </summary>
        internal int ActivitiesCacheCleanupIntervalMSec
        {
            get
            {
                return activitiesCacheCleanupIntervalMSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinActivitiesCacheCleanupIntervalMSec, PSWorkflowConfigurationProvider.MaxActivitiesCacheCleanupIntervalMSec, value);
                activitiesCacheCleanupIntervalMSec = value;
            }
        }

        /// <summary>
        /// ActivityProcessIdleTimeOutSec
        /// </summary>
        public int ActivityProcessIdleTimeoutSec
        {
            get
            {
                return activityProcessIdleTimeoutSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinActivityProcessIdleTimeoutSec, PSWorkflowConfigurationProvider.MaxActivityProcessIdleTimeoutSec, value);
                activityProcessIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// RemoteNodeSessionIdleTimeOut
        /// </summary>
        public int RemoteNodeSessionIdleTimeoutSec
        {
            get
            {
                return remoteNodeSessionIdleTimeoutSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinRemoteNodeSessionIdleTimeoutSec, PSWorkflowConfigurationProvider.MaxRemoteNodeSessionIdleTimeoutSec, value);
                remoteNodeSessionIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// WorkflowShutdownTimeoutMSec - the maximum time allowed to suspend the workflows before aborting them.
        /// </summary>
        public int WorkflowShutdownTimeoutMSec
        {
            get
            {
                return workflowShutdownTimeoutMSec;
            }
            set
            {
                ValidateRange(PSWorkflowConfigurationProvider.MinWorkflowShutdownTimeoutMSec, PSWorkflowConfigurationProvider.MaxWorkflowShutdownTimeoutMSec, value);
                workflowShutdownTimeoutMSec = value;
            }
        }

        /// <summary>
        /// Copies values from updated.  Only non default values are copies.
        /// </summary>
        /// <param name="updated"></param>
        protected override void CopyUpdatedValuesFrom(PSSessionTypeOption updated)
        {
            if (updated == null)
                throw new ArgumentNullException("updated");

            PSWorkflowExecutionOption modified = updated as PSWorkflowExecutionOption;
            if (modified == null)
                throw new ArgumentNullException("updated");

            if (modified.activityProcessIdleTimeoutSec != PSWorkflowConfigurationProvider.DefaultActivityProcessIdleTimeoutSec)
                this.activityProcessIdleTimeoutSec = modified.activityProcessIdleTimeoutSec;

            if (modified.workflowApplicationPersistUnloadTimeoutSec != PSWorkflowConfigurationProvider.DefaultWorkflowApplicationPersistUnloadTimeoutSec)
                this.workflowApplicationPersistUnloadTimeoutSec = modified.workflowApplicationPersistUnloadTimeoutSec;

            if (modified.wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec != PSWorkflowConfigurationProvider.DefaultWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec)
                this.wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec = modified.wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec;

            if (modified.activitiesCacheCleanupIntervalMSec != PSWorkflowConfigurationProvider.DefaultActivitiesCacheCleanupIntervalMSec)
                this.activitiesCacheCleanupIntervalMSec = modified.activitiesCacheCleanupIntervalMSec;

            if (!ListsMatch(modified.allowedActivity, PSWorkflowConfigurationProvider.DefaultAllowedActivity))
                this.allowedActivity = modified.allowedActivity;

            if (!string.Equals(modified.persistencePath, PSWorkflowConfigurationProvider.DefaultPersistencePath, StringComparison.OrdinalIgnoreCase))
                this.persistencePath = modified.persistencePath;

            if (modified.maxPersistenceStoreSizeGB != PSWorkflowConfigurationProvider.DefaultMaxPersistenceStoreSizeGB)
                this.maxPersistenceStoreSizeGB = modified.maxPersistenceStoreSizeGB;

            if (modified.persistWithEncryption != PSWorkflowConfigurationProvider.DefaultPersistWithEncryption)
                this.persistWithEncryption = modified.persistWithEncryption;

            if (modified.remoteNodeSessionIdleTimeoutSec != PSWorkflowConfigurationProvider.DefaultRemoteNodeSessionIdleTimeoutSec)
                this.remoteNodeSessionIdleTimeoutSec = modified.remoteNodeSessionIdleTimeoutSec;

            if (modified.maxActivityProcesses != PSWorkflowConfigurationProvider.DefaultMaxActivityProcesses)
                this.maxActivityProcesses = modified.maxActivityProcesses;

            if (modified.maxConnectedSessions != PSWorkflowConfigurationProvider.DefaultMaxConnectedSessions)
                this.maxConnectedSessions = modified.maxConnectedSessions;

            if (modified.maxDisconnectedSessions != PSWorkflowConfigurationProvider.DefaultMaxDisconnectedSessions)
                this.maxDisconnectedSessions = modified.maxDisconnectedSessions;

            if (modified.maxRunningWorkflows != PSWorkflowConfigurationProvider.DefaultMaxRunningWorkflows)
                this.maxRunningWorkflows = modified.maxRunningWorkflows;

            if (modified.maxSessionsPerRemoteNode != PSWorkflowConfigurationProvider.DefaultMaxSessionsPerRemoteNode)
                this.maxSessionsPerRemoteNode = modified.maxSessionsPerRemoteNode;

            if (modified.maxSessionsPerWorkflow != PSWorkflowConfigurationProvider.DefaultMaxSessionsPerWorkflow)
                this.maxSessionsPerWorkflow = modified.maxSessionsPerWorkflow;

            if (!ListsMatch(modified.outOfProcessActivity, PSWorkflowConfigurationProvider.DefaultOutOfProcessActivity))
                this.outOfProcessActivity = modified.outOfProcessActivity;

            if (modified.enableValidation != PSWorkflowConfigurationProvider.DefaultEnableValidation)
                this.enableValidation = modified.enableValidation;

            if (modified.sessionThrottleLimit != PSWorkflowConfigurationProvider.DefaultSessionThrottleLimit)
                this.sessionThrottleLimit = modified.sessionThrottleLimit;

            if (modified.workflowShutdownTimeoutMSec != PSWorkflowConfigurationProvider.DefaultWorkflowShutdownTimeoutMSec)
                this.workflowShutdownTimeoutMSec = modified.workflowShutdownTimeoutMSec;
        }

        private static bool ListsMatch(IEnumerable<string> a, IEnumerable<string> b)
        {
            foreach (string strA in a)
            {
                bool found = false;
                foreach (string strB in b)
                {
                    if (string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }

            foreach (string strB in b)
            {
                bool found = false;
                foreach (string strA in a)
                {
                    if (string.Compare(strB, strA, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a new instance constructed from privateData string.
        /// </summary>
        /// <returns></returns>
        protected override PSSessionTypeOption ConstructObjectFromPrivateData(string privateData)
        {
            return PSWorkflowConfigurationProvider.LoadConfig(privateData, null);
        }

        /// <summary>
        /// Implementation of the abstract method
        /// </summary>
        /// <returns></returns>
        protected override string ConstructPrivateData()
        {
            StringBuilder privateDataParams = new StringBuilder();

            bool usesDefaultPath = string.Compare(persistencePath, PSWorkflowConfigurationProvider.DefaultPersistencePath, StringComparison.OrdinalIgnoreCase) != 0;
            if (usesDefaultPath)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenPersistencePath, persistencePath));

            if (maxPersistenceStoreSizeGB != PSWorkflowConfigurationProvider.DefaultMaxPersistenceStoreSizeGB)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxPersistenceStoreSizeGB, maxPersistenceStoreSizeGB));

            if (persistWithEncryption != PSWorkflowConfigurationProvider.DefaultPersistWithEncryption)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenPersistWithEncryption, persistWithEncryption));

            if (maxRunningWorkflows != PSWorkflowConfigurationProvider.DefaultMaxRunningWorkflows)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxRunningWorkflows, maxRunningWorkflows));

            if (enableValidation != PSWorkflowConfigurationProvider.DefaultEnableValidation)
                    privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenEnableValidation, enableValidation));

            StringBuilder allowedAttribute = new StringBuilder();
            foreach (string a in allowedActivity ?? new string[0])
            {
                allowedAttribute.Append(a);
                allowedAttribute.Append(',');
            }
            if (allowedAttribute.Length > 0)
            {
                allowedAttribute.Remove(allowedAttribute.Length - 1, 1);
            }
            privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenAllowedActivity, allowedAttribute.ToString()));

            StringBuilder outOfProcAttribute = new StringBuilder();
            foreach (string a in outOfProcessActivity ?? new string[0])
            {
                outOfProcAttribute.Append(a);
                outOfProcAttribute.Append(',');
            }
            if (outOfProcAttribute.Length > 0)
            {
                outOfProcAttribute.Remove(outOfProcAttribute.Length - 1, 1);
            }
            privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenOutOfProcessActivity, outOfProcAttribute.ToString()));

            if (maxDisconnectedSessions != PSWorkflowConfigurationProvider.DefaultMaxDisconnectedSessions)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxDisconnectedSessions, maxDisconnectedSessions));

            if (maxConnectedSessions != PSWorkflowConfigurationProvider.DefaultMaxConnectedSessions)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxConnectedSessions, maxConnectedSessions));

            if (maxSessionsPerWorkflow != PSWorkflowConfigurationProvider.DefaultMaxSessionsPerWorkflow)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxSessionsPerWorkflow, maxSessionsPerWorkflow));

            if (maxSessionsPerRemoteNode != PSWorkflowConfigurationProvider.DefaultMaxSessionsPerRemoteNode)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxSessionsPerRemoteNode, maxSessionsPerRemoteNode));

            if (maxActivityProcesses != PSWorkflowConfigurationProvider.DefaultMaxActivityProcesses)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenMaxActivityProcesses, maxActivityProcesses));

            if (activityProcessIdleTimeoutSec != PSWorkflowConfigurationProvider.DefaultActivityProcessIdleTimeoutSec)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenActivityProcessIdleTimeoutSec, activityProcessIdleTimeoutSec));

			if (workflowApplicationPersistUnloadTimeoutSec != PSWorkflowConfigurationProvider.DefaultWorkflowApplicationPersistUnloadTimeoutSec)
				privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenWorkflowApplicationPersistUnloadTimeoutSec, workflowApplicationPersistUnloadTimeoutSec));

            if (wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec != PSWorkflowConfigurationProvider.DefaultWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenWSManPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec, wsmanPluginReportCompletionOnZeroActiveSessionsWaitIntervalMSec));

            if (activitiesCacheCleanupIntervalMSec != PSWorkflowConfigurationProvider.DefaultActivitiesCacheCleanupIntervalMSec)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenActivitiesCacheCleanupIntervalMSec, activitiesCacheCleanupIntervalMSec));

            if (remoteNodeSessionIdleTimeoutSec != PSWorkflowConfigurationProvider.DefaultRemoteNodeSessionIdleTimeoutSec)
				privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenRemoteNodeSessionIdleTimeoutSec, remoteNodeSessionIdleTimeoutSec));

            if (sessionThrottleLimit != PSWorkflowConfigurationProvider.DefaultSessionThrottleLimit)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenSessionThrottleLimit, sessionThrottleLimit));

            if (workflowShutdownTimeoutMSec != PSWorkflowConfigurationProvider.DefaultWorkflowShutdownTimeoutMSec)
                privateDataParams.Append(string.Format(CultureInfo.InvariantCulture, ParamToken, PSWorkflowConfigurationProvider.TokenWorkflowShutdownTimeoutMSec, workflowShutdownTimeoutMSec));

            return string.Format(CultureInfo.InvariantCulture, PrivateDataFormat, privateDataParams.ToString());
        }

        internal string ConstructPrivateDataInternal()
        {
            return ConstructPrivateData();
        }
    }

    /// <summary>
    /// Command to create an object for PSWorkflowExecutionOption
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSWorkflowExecutionOption",HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210609")]
    [OutputType(typeof(PSWorkflowExecutionOption))]
    public sealed class NewPSWorkflowExecutionOptionCommand : PSCmdlet
    {
        private PSWorkflowExecutionOption option = new PSWorkflowExecutionOption();
        private bool enableValidationParamSpecified = false;

        /// <summary>
        /// PersistencePath
        /// </summary>
        [Parameter]
        public string PersistencePath
        {
            get
            {
                return option.PersistencePath;
            }
            set
            {
                if (value != null)
                {
                    string rootedPath = value;
                    bool isPathTooLong = false;
                    if (!Path.IsPathRooted(value))
                    {
                        try
                        {
                            rootedPath = Path.GetPathRoot(value);

                            if (String.IsNullOrEmpty(rootedPath))
                            {
                                rootedPath = value;
                            }
                        }
                        catch (PathTooLongException)
                        {
                            isPathTooLong = true;
                        }
                    }

                    if (isPathTooLong || (rootedPath != null && rootedPath.Length > Constants.MaxAllowedPersistencePathLength))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, Resources.PersistencePathToolLong, value, Constants.MaxAllowedPersistencePathLength));
                    }
                }

                option.PersistencePath = value;
            }
        }

        /// <summary>
        /// MaxPersistenceStoreSizeGB 
        /// </summary>
        [Parameter]
        public long MaxPersistenceStoreSizeGB
        {
            get
            {
                return option.MaxPersistenceStoreSizeGB;
            }
            set
            {
                option.MaxPersistenceStoreSizeGB = value;
            }
        }
        
        /// <summary>
        /// UseEncryption
        /// </summary>
        [Parameter]
        public SwitchParameter PersistWithEncryption {
            get
            {
                return option.PersistWithEncryption;
            }
            set
            {
                option.PersistWithEncryption = value;
            }
        }

        /// <summary>
        /// MaxRunningWorkflows
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxRunningWorkflows, PSWorkflowConfigurationProvider.MaxMaxRunningWorkflows)]
        public int MaxRunningWorkflows {
            get
            {
                return option.MaxRunningWorkflows;
            }
            set
            {
                option.MaxRunningWorkflows = value;
            }
        }

        /// <summary>
        /// AllowedActivity
        /// </summary>
        [Parameter, SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] AllowedActivity
        {
            get
            {
                return option.AllowedActivity;
            }
            set
            {
                option.AllowedActivity = value;
            }
        }

        /// <summary>
        /// OutOfProcActivity
        /// </summary>
        [Parameter, SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OutOfProcessActivity
        {
            get
            {
                return option.OutOfProcessActivity;
            }
            set
            {
                option.OutOfProcessActivity = value;
            }
        }

        /// <summary>
        /// EnableValidation
        /// </summary>
        [Parameter]
        public SwitchParameter EnableValidation
        {
            get
            {
                return option.EnableValidation;
            }
            set
            {
                option.EnableValidation = value;
                enableValidationParamSpecified = true;
            }
        }

        /// <summary>
        /// MaxDisconnectedSessions
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxDisconnectedSessions, PSWorkflowConfigurationProvider.MaxMaxDisconnectedSessions)]
        public int MaxDisconnectedSessions
        {
            get
            {
                return option.MaxDisconnectedSessions;
            }
            set
            {
                option.MaxDisconnectedSessions = value;
            }
        }

        /// <summary>
        /// MaxConnectedSessions
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxConnectedSessions, PSWorkflowConfigurationProvider.MaxMaxConnectedSessions)]
        public int MaxConnectedSessions
        {
            get
            {
                return option.MaxConnectedSessions;
            }
            set
            {
                option.MaxConnectedSessions = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerWorkflow
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxSessionsPerWorkflow, PSWorkflowConfigurationProvider.MaxMaxSessionsPerWorkflow)]
        public int MaxSessionsPerWorkflow
        {
            get
            {
                return option.MaxSessionsPerWorkflow;
            }
            set
            {
                option.MaxSessionsPerWorkflow = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerRemoteNode
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxSessionsPerRemoteNode, PSWorkflowConfigurationProvider.MaxMaxSessionsPerRemoteNode)]
        public int MaxSessionsPerRemoteNode
        {
            get
            {
                return option.MaxSessionsPerRemoteNode;
            }
            set
            {
                option.MaxSessionsPerRemoteNode = value;
            }
        }

        /// <summary>
        /// MaxActivityProcess
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinMaxActivityProcesses, PSWorkflowConfigurationProvider.MaxMaxActivityProcesses)]
        public int MaxActivityProcesses
        {
            get
            {
                return option.MaxActivityProcesses;
            }
            set
            {
                option.MaxActivityProcesses = value;
            }
        }

        /// <summary>
        /// ActivityProcessIdleTimeOutSec
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinActivityProcessIdleTimeoutSec, PSWorkflowConfigurationProvider.MaxActivityProcessIdleTimeoutSec)]
        public int ActivityProcessIdleTimeoutSec
        {
            get
            {
                return option.ActivityProcessIdleTimeoutSec;
            }
            set
            {
                option.ActivityProcessIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// RemoteNodeSessionIdleTimeOutSec
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinRemoteNodeSessionIdleTimeoutSec, PSWorkflowConfigurationProvider.MaxRemoteNodeSessionIdleTimeoutSec)]
        public int RemoteNodeSessionIdleTimeoutSec
        {
            get
            {
                return option.RemoteNodeSessionIdleTimeoutSec;
            }
            set
            {
                option.RemoteNodeSessionIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// SessionThrottleLimit
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinSessionThrottleLimit, PSWorkflowConfigurationProvider.MaxSessionThrottleLimit)]
        public int SessionThrottleLimit
        {
            get
            {
                return option.SessionThrottleLimit;
            }
            set
            {
                option.SessionThrottleLimit = value;
            }
        }

        /// <summary>
        /// WorkflowShutdownTimeoutMSec - the maximum time allowed to suspend the workflows before aborting them.
        /// </summary>
        [Parameter, ValidateRange(PSWorkflowConfigurationProvider.MinWorkflowShutdownTimeoutMSec, PSWorkflowConfigurationProvider.MaxWorkflowShutdownTimeoutMSec)]
        public int WorkflowShutdownTimeoutMSec
        {
            get
            {
                return option.WorkflowShutdownTimeoutMSec;
            }
            set
            {
                option.WorkflowShutdownTimeoutMSec = value;
            }
        }
        
        /// <summary>
        /// ProcessRecord
        /// </summary>
        protected override void ProcessRecord()
        {
            // By default EnableValidation should be TRUE for NewPSWorkflowExecutionOption, so that activity validation is done in user created endpoints.
            // 
            if(!enableValidationParamSpecified)
            {
                option.EnableValidation = true; 
            }

            this.WriteObject(option);
        }
    }
}
