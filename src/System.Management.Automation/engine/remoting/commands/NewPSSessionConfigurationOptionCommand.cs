//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Management.Automation;
using System.Globalization;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementing type for WSManConfigurationOption
    /// </summary>
    public class WSManConfigurationOption : PSTransportOption
    {
        private const string Token = " {0}='{1}'";
        private const string QuotasToken = "<Quotas {0} />";

        internal const string AttribOutputBufferingMode = "OutputBufferingMode";
        internal static System.Management.Automation.Runspaces.OutputBufferingMode? DefaultOutputBufferingMode = System.Management.Automation.Runspaces.OutputBufferingMode.Block;
        private System.Management.Automation.Runspaces.OutputBufferingMode? outputBufferingMode = null;

        private const string AttribProcessIdleTimeout = "ProcessIdleTimeoutSec";
        internal static readonly int? DefaultProcessIdleTimeout_ForPSRemoting = 0; // in seconds
        internal static readonly int? DefaultProcessIdleTimeout_ForWorkflow = 1209600; // in seconds
        private int? _processIdleTimeoutSec = null;

        internal const string AttribMaxIdleTimeout = "MaxIdleTimeoutms";
        internal static readonly int? DefaultMaxIdleTimeout = int.MaxValue;
        private int? _maxIdleTimeoutSec = null;

        internal const string AttribIdleTimeout = "IdleTimeoutms";
        internal static readonly int? DefaultIdleTimeout = 7200; // 2 hours in seconds
        private int? _idleTimeoutSec = null;

        private const string AttribMaxConcurrentUsers = "MaxConcurrentUsers";
        internal static readonly int? DefaultMaxConcurrentUsers = int.MaxValue;
        private int? maxConcurrentUsers = null;

        private const string AttribMaxProcessesPerSession = "MaxProcessesPerShell";
        internal static readonly int? DefaultMaxProcessesPerSession = int.MaxValue;
        private int? maxProcessesPerSession = null;

        private const string AttribMaxMemoryPerSessionMB = "MaxMemoryPerShellMB";
        internal static readonly int? DefaultMaxMemoryPerSessionMB = int.MaxValue;
        private int? maxMemoryPerSessionMB = null;

        private const string AttribMaxSessions = "MaxShells";
        internal static readonly int? DefaultMaxSessions = int.MaxValue;
        private int? maxSessions = null;

        private const string AttribMaxSessionsPerUser = "MaxShellsPerUser";
        internal static readonly int? DefaultMaxSessionsPerUser = int.MaxValue;
        private int? maxSessionsPerUser = null;

        private const string AttribMaxConcurrentCommandsPerSession = "MaxConcurrentCommandsPerShell";
        internal static readonly int? DefaultMaxConcurrentCommandsPerSession = int.MaxValue;
        private int? maxConcurrentCommandsPerSession = null;

        /// <summary>
        /// Constructor that instantiates with default values
        /// </summary>
        internal WSManConfigurationOption()
        {

        }

        /// <summary>
        /// LoadFromDefaults
        /// </summary>
        /// <param name="sessionType"></param>
        /// <param name="keepAssigned"></param>
        protected internal override void LoadFromDefaults(PSSessionType sessionType, bool keepAssigned)
        {
            if (!keepAssigned || !outputBufferingMode.HasValue)
            {
                outputBufferingMode = DefaultOutputBufferingMode;
            }
            if (!keepAssigned || !_processIdleTimeoutSec.HasValue)
            {
                _processIdleTimeoutSec 
                    = sessionType == PSSessionType.Workflow 
                    ? DefaultProcessIdleTimeout_ForWorkflow 
                    : DefaultProcessIdleTimeout_ForPSRemoting;
            }
            if (!keepAssigned || !_maxIdleTimeoutSec.HasValue)
            {
                _maxIdleTimeoutSec = DefaultMaxIdleTimeout;
            }
            if (!keepAssigned || !_idleTimeoutSec.HasValue)
            {
                _idleTimeoutSec = DefaultIdleTimeout;
            }
            if (!keepAssigned || !maxConcurrentUsers.HasValue)
            {
                maxConcurrentUsers = DefaultMaxConcurrentUsers;
            }
            if (!keepAssigned || !maxProcessesPerSession.HasValue)
            {
                maxProcessesPerSession = DefaultMaxProcessesPerSession;
            }
            if (!keepAssigned || !maxMemoryPerSessionMB.HasValue)
            {
                maxMemoryPerSessionMB = DefaultMaxMemoryPerSessionMB;
            }
            if (!keepAssigned || !maxSessions.HasValue)
            {
                maxSessions = DefaultMaxSessions;
            }
            if (!keepAssigned || !maxSessionsPerUser.HasValue)
            {
                maxSessionsPerUser = DefaultMaxSessionsPerUser;
            }
            if (!keepAssigned || !maxConcurrentCommandsPerSession.HasValue)
            {
                maxConcurrentCommandsPerSession = DefaultMaxConcurrentCommandsPerSession;
            }
        }

        /// <summary>
        /// ProcessIdleTimeout in Seconds
        /// </summary>
        public int? ProcessIdleTimeoutSec
        {
            get
            {
                return _processIdleTimeoutSec;
            }
            internal set
            {
                _processIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// MaxIdleTimeout in Seconds
        /// </summary>
        public int? MaxIdleTimeoutSec
        {
            get
            {
                return _maxIdleTimeoutSec;
            }
            internal set
            {
                _maxIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// MaxSessions
        /// </summary>
        public int? MaxSessions
        {
            get
            {
                return maxSessions;
            }
            internal set
            {
                maxSessions = value;
            }
        }

        /// <summary>
        /// MaxConcurrentCommandsPerSession
        /// </summary>
        public int? MaxConcurrentCommandsPerSession
        {
            get
            {
                return maxConcurrentCommandsPerSession;
            }
            internal set
            {
                maxConcurrentCommandsPerSession = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerUser
        /// </summary>
        public int? MaxSessionsPerUser
        {
            get
            {
                return maxSessionsPerUser;
            }
            internal set
            {
                maxSessionsPerUser = value;
            }
        }

        /// <summary>
        /// MaxMemoryPerSessionMB
        /// </summary>
        public int? MaxMemoryPerSessionMB
        {
            get
            {
                return maxMemoryPerSessionMB;
            }
            internal set
            {
                maxMemoryPerSessionMB = value;
            }
        }

        /// <summary>
        /// MaxProcessesPerSession
        /// </summary>
        public int? MaxProcessesPerSession
        {
            get
            {
                return maxProcessesPerSession;
            }
            internal set
            {
                maxProcessesPerSession = value;
            }
        }

        /// <summary>
        /// MaxConcurrentUsers
        /// </summary>
        public int? MaxConcurrentUsers
        {
            get
            {
                return maxConcurrentUsers;
            }
            internal set
            {
                maxConcurrentUsers = value;
            }
        }

        /// <summary>
        /// IdleTimeout in Seconds
        /// </summary>
        public int? IdleTimeoutSec
        {
            get
            {
                return _idleTimeoutSec;
            }
            internal set
            {
                _idleTimeoutSec = value;
            }
        }

        /// <summary>
        /// OutputBufferingMode
        /// </summary>
        public System.Management.Automation.Runspaces.OutputBufferingMode? OutputBufferingMode
        {
            get
            {
                return outputBufferingMode;
            }
            internal set
            {
                outputBufferingMode = value;
            }
        }

        internal override Hashtable ConstructQuotasAsHashtable()
        {
            Hashtable quotas = new Hashtable();

            if (_idleTimeoutSec.HasValue)
            {
                quotas[AttribIdleTimeout] = (1000 * _idleTimeoutSec.Value).ToString(CultureInfo.InvariantCulture);
            }
            if (maxConcurrentUsers.HasValue)
            {
                quotas[AttribMaxConcurrentUsers] = maxConcurrentUsers.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (maxProcessesPerSession.HasValue)
            {
                quotas[AttribMaxProcessesPerSession] = maxProcessesPerSession.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (maxMemoryPerSessionMB.HasValue)
            {
                quotas[AttribMaxMemoryPerSessionMB] = maxMemoryPerSessionMB.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (maxSessionsPerUser.HasValue)
            {
                quotas[AttribMaxSessionsPerUser] = maxSessionsPerUser.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (maxConcurrentCommandsPerSession.HasValue)
            {
                quotas[AttribMaxConcurrentCommandsPerSession] = maxConcurrentCommandsPerSession.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (maxSessions.HasValue)
            {
                quotas[AttribMaxSessions] = maxSessions.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (_maxIdleTimeoutSec.HasValue)
            {
                quotas[AttribMaxIdleTimeout] = (1000 * _maxIdleTimeoutSec.Value).ToString(CultureInfo.InvariantCulture);
            }
            return quotas;
        }

        /// <summary>
        /// ConstructQuotas
        /// </summary>
        /// <returns></returns>
        internal override string ConstructQuotas()
        {
            StringBuilder sb = new StringBuilder();

            if (_idleTimeoutSec.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribIdleTimeout, 1000 * _idleTimeoutSec));
            }
            if (maxConcurrentUsers.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxConcurrentUsers, maxConcurrentUsers));
            }

            if (maxProcessesPerSession.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxProcessesPerSession, maxProcessesPerSession));
            }
            if (maxMemoryPerSessionMB.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxMemoryPerSessionMB, maxMemoryPerSessionMB));
            }
            if (maxSessionsPerUser.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxSessionsPerUser, maxSessionsPerUser));
            }
            if (maxConcurrentCommandsPerSession.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxConcurrentCommandsPerSession, maxConcurrentCommandsPerSession));
            }
            if (maxSessions.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxSessions, maxSessions));
            }
            if (_maxIdleTimeoutSec.HasValue)
            {
                // Special case max int value for unbounded default.
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxIdleTimeout, 
                    (_maxIdleTimeoutSec == int.MaxValue) ? _maxIdleTimeoutSec : (1000 * _maxIdleTimeoutSec)));
            }

            return sb.Length > 0
                ? string.Format(CultureInfo.InvariantCulture, QuotasToken, sb.ToString())
                : string.Empty;
        }

        /// <summary>
        /// ConstructOptionsXmlAttributes
        /// </summary>
        /// <returns></returns>
        internal override string ConstructOptionsAsXmlAttributes()
        {
            StringBuilder sb = new StringBuilder();
            if (outputBufferingMode.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribOutputBufferingMode, outputBufferingMode.ToString()));
            }

            if (_processIdleTimeoutSec.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribProcessIdleTimeout, _processIdleTimeoutSec));
            }

            return sb.ToString();
        }

        /// <summary>
        /// ConstructOptionsXmlAttributes
        /// </summary>
        /// <returns></returns>
        internal override Hashtable ConstructOptionsAsHashtable()
        {
            Hashtable table = new Hashtable();
            if (outputBufferingMode.HasValue)
            {
                table[AttribOutputBufferingMode] = outputBufferingMode.ToString();
            }

            if (_processIdleTimeoutSec.HasValue)
            {
                table[AttribProcessIdleTimeout] = _processIdleTimeoutSec;
            }

            return table;
        }
    }

    /// <summary>
    /// Command to create an object for WSManConfigurationOption
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSTransportOption", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=210608", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(WSManConfigurationOption))]
    public sealed class NewPSTransportOptionCommand : PSCmdlet
    {
        private WSManConfigurationOption option = new WSManConfigurationOption();

        /// <summary>
        /// MaxIdleTimeoutSec
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(60, 2147483)]
        public int? MaxIdleTimeoutSec
        {
            get
            {
                return option.MaxIdleTimeoutSec;
            }
            set
            {
                option.MaxIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// ProcessIdleTimeoutSec
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(0, 1209600)]
        public int? ProcessIdleTimeoutSec
        {
            get
            {
                return option.ProcessIdleTimeoutSec;
            }
            set
            {
                option.ProcessIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// MaxSessions
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxSessions
        {
            get
            {
                return option.MaxSessions;
            }
            set
            {
                option.MaxSessions = value;
            }
        }

        /// <summary>
        /// MaxConcurrentCommandsPerSession
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxConcurrentCommandsPerSession
        {
            get
            {
                return option.MaxConcurrentCommandsPerSession;
            }
            set
            {
                option.MaxConcurrentCommandsPerSession = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerUser
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxSessionsPerUser
        {
            get
            {
                return option.MaxSessionsPerUser;
            }
            set
            {
                option.MaxSessionsPerUser = value;
            }
        }

        /// <summary>
        /// MaxMemoryPerSessionMB
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(5, int.MaxValue)]
        public int? MaxMemoryPerSessionMB
        {
            get
            {
                return option.MaxMemoryPerSessionMB;
            }
            set
            {
                option.MaxMemoryPerSessionMB = value;
            }
        }

        /// <summary>
        /// MaxProcessesPerSession
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxProcessesPerSession
        {
            get
            {
                return option.MaxProcessesPerSession;
            }
            set
            {
                option.MaxProcessesPerSession = value;
            }
        }

        /// <summary>
        /// MaxConcurrentUsers
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, 100)]
        public int? MaxConcurrentUsers
        {
            get
            {
                return option.MaxConcurrentUsers;
            }
            set
            {
                option.MaxConcurrentUsers = value;
            }
        }

        /// <summary>
        /// IdleTimeoutMs
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(60, 2147483)]
        public int? IdleTimeoutSec
        {
            get
            {
                return option.IdleTimeoutSec;
            }
            set
            {
                option.IdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// OutputBufferingMode
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.Runspaces.OutputBufferingMode? OutputBufferingMode
        {
            get
            {
                return option.OutputBufferingMode;
            }
            set
            {
                option.OutputBufferingMode = value;
            }
        }

        /// <summary>
        /// Overriding the base method
        /// </summary>
        protected override void ProcessRecord()
        {
            this.WriteObject(option);
        }
    }
}
