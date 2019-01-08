// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Collections;
using System.Management.Automation;
using System.Globalization;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementing type for WSManConfigurationOption.
    /// </summary>
    public class WSManConfigurationOption : PSTransportOption
    {
        private const string Token = " {0}='{1}'";
        private const string QuotasToken = "<Quotas {0} />";

        internal const string AttribOutputBufferingMode = "OutputBufferingMode";
        internal static System.Management.Automation.Runspaces.OutputBufferingMode? DefaultOutputBufferingMode = System.Management.Automation.Runspaces.OutputBufferingMode.Block;
        private System.Management.Automation.Runspaces.OutputBufferingMode? _outputBufferingMode = null;

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
        private int? _maxConcurrentUsers = null;

        private const string AttribMaxProcessesPerSession = "MaxProcessesPerShell";
        internal static readonly int? DefaultMaxProcessesPerSession = int.MaxValue;
        private int? _maxProcessesPerSession = null;

        private const string AttribMaxMemoryPerSessionMB = "MaxMemoryPerShellMB";
        internal static readonly int? DefaultMaxMemoryPerSessionMB = int.MaxValue;
        private int? _maxMemoryPerSessionMB = null;

        private const string AttribMaxSessions = "MaxShells";
        internal static readonly int? DefaultMaxSessions = int.MaxValue;
        private int? _maxSessions = null;

        private const string AttribMaxSessionsPerUser = "MaxShellsPerUser";
        internal static readonly int? DefaultMaxSessionsPerUser = int.MaxValue;
        private int? _maxSessionsPerUser = null;

        private const string AttribMaxConcurrentCommandsPerSession = "MaxConcurrentCommandsPerShell";
        internal static readonly int? DefaultMaxConcurrentCommandsPerSession = int.MaxValue;
        private int? _maxConcurrentCommandsPerSession = null;

        /// <summary>
        /// Constructor that instantiates with default values.
        /// </summary>
        internal WSManConfigurationOption()
        {
        }

        /// <summary>
        /// LoadFromDefaults.
        /// </summary>
        /// <param name="sessionType"></param>
        /// <param name="keepAssigned"></param>
        protected internal override void LoadFromDefaults(PSSessionType sessionType, bool keepAssigned)
        {
            if (!keepAssigned || !_outputBufferingMode.HasValue)
            {
                _outputBufferingMode = DefaultOutputBufferingMode;
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

            if (!keepAssigned || !_maxConcurrentUsers.HasValue)
            {
                _maxConcurrentUsers = DefaultMaxConcurrentUsers;
            }

            if (!keepAssigned || !_maxProcessesPerSession.HasValue)
            {
                _maxProcessesPerSession = DefaultMaxProcessesPerSession;
            }

            if (!keepAssigned || !_maxMemoryPerSessionMB.HasValue)
            {
                _maxMemoryPerSessionMB = DefaultMaxMemoryPerSessionMB;
            }

            if (!keepAssigned || !_maxSessions.HasValue)
            {
                _maxSessions = DefaultMaxSessions;
            }

            if (!keepAssigned || !_maxSessionsPerUser.HasValue)
            {
                _maxSessionsPerUser = DefaultMaxSessionsPerUser;
            }

            if (!keepAssigned || !_maxConcurrentCommandsPerSession.HasValue)
            {
                _maxConcurrentCommandsPerSession = DefaultMaxConcurrentCommandsPerSession;
            }
        }

        /// <summary>
        /// ProcessIdleTimeout in Seconds.
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
        /// MaxIdleTimeout in Seconds.
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
        /// MaxSessions.
        /// </summary>
        public int? MaxSessions
        {
            get
            {
                return _maxSessions;
            }

            internal set
            {
                _maxSessions = value;
            }
        }

        /// <summary>
        /// MaxConcurrentCommandsPerSession.
        /// </summary>
        public int? MaxConcurrentCommandsPerSession
        {
            get
            {
                return _maxConcurrentCommandsPerSession;
            }

            internal set
            {
                _maxConcurrentCommandsPerSession = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerUser.
        /// </summary>
        public int? MaxSessionsPerUser
        {
            get
            {
                return _maxSessionsPerUser;
            }

            internal set
            {
                _maxSessionsPerUser = value;
            }
        }

        /// <summary>
        /// MaxMemoryPerSessionMB.
        /// </summary>
        public int? MaxMemoryPerSessionMB
        {
            get
            {
                return _maxMemoryPerSessionMB;
            }

            internal set
            {
                _maxMemoryPerSessionMB = value;
            }
        }

        /// <summary>
        /// MaxProcessesPerSession.
        /// </summary>
        public int? MaxProcessesPerSession
        {
            get
            {
                return _maxProcessesPerSession;
            }

            internal set
            {
                _maxProcessesPerSession = value;
            }
        }

        /// <summary>
        /// MaxConcurrentUsers.
        /// </summary>
        public int? MaxConcurrentUsers
        {
            get
            {
                return _maxConcurrentUsers;
            }

            internal set
            {
                _maxConcurrentUsers = value;
            }
        }

        /// <summary>
        /// IdleTimeout in Seconds.
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
        /// OutputBufferingMode.
        /// </summary>
        public System.Management.Automation.Runspaces.OutputBufferingMode? OutputBufferingMode
        {
            get
            {
                return _outputBufferingMode;
            }

            internal set
            {
                _outputBufferingMode = value;
            }
        }

        internal override Hashtable ConstructQuotasAsHashtable()
        {
            Hashtable quotas = new Hashtable();

            if (_idleTimeoutSec.HasValue)
            {
                quotas[AttribIdleTimeout] = (1000 * _idleTimeoutSec.Value).ToString(CultureInfo.InvariantCulture);
            }

            if (_maxConcurrentUsers.HasValue)
            {
                quotas[AttribMaxConcurrentUsers] = _maxConcurrentUsers.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxProcessesPerSession.HasValue)
            {
                quotas[AttribMaxProcessesPerSession] = _maxProcessesPerSession.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxMemoryPerSessionMB.HasValue)
            {
                quotas[AttribMaxMemoryPerSessionMB] = _maxMemoryPerSessionMB.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxSessionsPerUser.HasValue)
            {
                quotas[AttribMaxSessionsPerUser] = _maxSessionsPerUser.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxConcurrentCommandsPerSession.HasValue)
            {
                quotas[AttribMaxConcurrentCommandsPerSession] = _maxConcurrentCommandsPerSession.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxSessions.HasValue)
            {
                quotas[AttribMaxSessions] = _maxSessions.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (_maxIdleTimeoutSec.HasValue)
            {
                quotas[AttribMaxIdleTimeout] = (1000 * _maxIdleTimeoutSec.Value).ToString(CultureInfo.InvariantCulture);
            }

            return quotas;
        }

        /// <summary>
        /// ConstructQuotas.
        /// </summary>
        /// <returns></returns>
        internal override string ConstructQuotas()
        {
            StringBuilder sb = new StringBuilder();

            if (_idleTimeoutSec.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribIdleTimeout, 1000 * _idleTimeoutSec));
            }

            if (_maxConcurrentUsers.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxConcurrentUsers, _maxConcurrentUsers));
            }

            if (_maxProcessesPerSession.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxProcessesPerSession, _maxProcessesPerSession));
            }

            if (_maxMemoryPerSessionMB.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxMemoryPerSessionMB, _maxMemoryPerSessionMB));
            }

            if (_maxSessionsPerUser.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxSessionsPerUser, _maxSessionsPerUser));
            }

            if (_maxConcurrentCommandsPerSession.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxConcurrentCommandsPerSession, _maxConcurrentCommandsPerSession));
            }

            if (_maxSessions.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribMaxSessions, _maxSessions));
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
        /// ConstructOptionsXmlAttributes.
        /// </summary>
        /// <returns></returns>
        internal override string ConstructOptionsAsXmlAttributes()
        {
            StringBuilder sb = new StringBuilder();
            if (_outputBufferingMode.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribOutputBufferingMode, _outputBufferingMode.ToString()));
            }

            if (_processIdleTimeoutSec.HasValue)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, Token, AttribProcessIdleTimeout, _processIdleTimeoutSec));
            }

            return sb.ToString();
        }

        /// <summary>
        /// ConstructOptionsXmlAttributes.
        /// </summary>
        /// <returns></returns>
        internal override Hashtable ConstructOptionsAsHashtable()
        {
            Hashtable table = new Hashtable();
            if (_outputBufferingMode.HasValue)
            {
                table[AttribOutputBufferingMode] = _outputBufferingMode.ToString();
            }

            if (_processIdleTimeoutSec.HasValue)
            {
                table[AttribProcessIdleTimeout] = _processIdleTimeoutSec;
            }

            return table;
        }
    }

    /// <summary>
    /// Command to create an object for WSManConfigurationOption.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PSTransportOption", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=210608", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(WSManConfigurationOption))]
    public sealed class NewPSTransportOptionCommand : PSCmdlet
    {
        private WSManConfigurationOption _option = new WSManConfigurationOption();

        /// <summary>
        /// MaxIdleTimeoutSec.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(60, 2147483)]
        public int? MaxIdleTimeoutSec
        {
            get
            {
                return _option.MaxIdleTimeoutSec;
            }

            set
            {
                _option.MaxIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// ProcessIdleTimeoutSec.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(0, 1209600)]
        public int? ProcessIdleTimeoutSec
        {
            get
            {
                return _option.ProcessIdleTimeoutSec;
            }

            set
            {
                _option.ProcessIdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// MaxSessions.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxSessions
        {
            get
            {
                return _option.MaxSessions;
            }

            set
            {
                _option.MaxSessions = value;
            }
        }

        /// <summary>
        /// MaxConcurrentCommandsPerSession.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxConcurrentCommandsPerSession
        {
            get
            {
                return _option.MaxConcurrentCommandsPerSession;
            }

            set
            {
                _option.MaxConcurrentCommandsPerSession = value;
            }
        }

        /// <summary>
        /// MaxSessionsPerUser.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxSessionsPerUser
        {
            get
            {
                return _option.MaxSessionsPerUser;
            }

            set
            {
                _option.MaxSessionsPerUser = value;
            }
        }

        /// <summary>
        /// MaxMemoryPerSessionMB.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(5, int.MaxValue)]
        public int? MaxMemoryPerSessionMB
        {
            get
            {
                return _option.MaxMemoryPerSessionMB;
            }

            set
            {
                _option.MaxMemoryPerSessionMB = value;
            }
        }

        /// <summary>
        /// MaxProcessesPerSession.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, int.MaxValue)]
        public int? MaxProcessesPerSession
        {
            get
            {
                return _option.MaxProcessesPerSession;
            }

            set
            {
                _option.MaxProcessesPerSession = value;
            }
        }

        /// <summary>
        /// MaxConcurrentUsers.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(1, 100)]
        public int? MaxConcurrentUsers
        {
            get
            {
                return _option.MaxConcurrentUsers;
            }

            set
            {
                _option.MaxConcurrentUsers = value;
            }
        }

        /// <summary>
        /// IdleTimeoutMs.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true), ValidateRange(60, 2147483)]
        public int? IdleTimeoutSec
        {
            get
            {
                return _option.IdleTimeoutSec;
            }

            set
            {
                _option.IdleTimeoutSec = value;
            }
        }

        /// <summary>
        /// OutputBufferingMode.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public System.Management.Automation.Runspaces.OutputBufferingMode? OutputBufferingMode
        {
            get
            {
                return _option.OutputBufferingMode;
            }

            set
            {
                _option.OutputBufferingMode = value;
            }
        }

        /// <summary>
        /// Overriding the base method.
        /// </summary>
        protected override void ProcessRecord()
        {
            this.WriteObject(_option);
        }
    }
}
