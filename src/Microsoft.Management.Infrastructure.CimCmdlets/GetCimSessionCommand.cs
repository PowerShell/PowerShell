// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// The command returns zero, one or more CimSession objects that represent
    /// connections with remote computers established from the current PS Session.
    /// </summary>

    [Cmdlet(VerbsCommon.Get, "CimSession", DefaultParameterSetName = ComputerNameSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227966")]
    [OutputType(typeof(CimSession))]
    public sealed class GetCimSessionCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public GetCimSessionCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ComputerName".
        /// Specifies one or more connections by providing their ComputerName(s). The
        /// Cmdlet then gets CimSession(s) opened with those connections. This parameter
        /// is an alternative to using CimSession(s) that also identifies the remote
        /// computer(s).
        /// </para>
        /// <para>
        /// This is the only optional parameter of the Cmdlet. If not provided, the
        /// Cmdlet returns all CimSession(s) live/active in the runspace.
        /// </para>
        /// <para>
        /// If an instance of CimSession is pipelined to Get-CimSession, the
        /// ComputerName property of the instance is bound by name with this parameter.
        /// </para>
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ComputerNameSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get { return computername;}

            set
            {
                computername = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computername;

        /// <summary>
        /// The following is the definition of the input parameter "Id".
        /// Specifies one or more numeric Id(s) for which to get CimSession(s).
        /// </summary>
        [Parameter(Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = SessionIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public UInt32[] Id
        {
            get { return id;}

            set
            {
                id = value;
                base.SetParameter(value, nameId);
            }
        }

        private UInt32[] id;

        /// <summary>
        /// The following is the definition of the input parameter "InstanceID".
        /// Specifies one or Session Instance IDs.
        /// </summary>
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = InstanceIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Guid[] InstanceId
        {
            get { return instanceid;}

            set
            {
                instanceid = value;
                base.SetParameter(value, nameInstanceId);
            }
        }

        private Guid[] instanceid;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies one or more session Name(s)  for which to get CimSession(s). The
        /// argument may contain wildcard characters.
        /// </summary>
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = NameSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return name;}

            set
            {
                name = value;
                base.SetParameter(value, nameName);
            }
        }

        private string[] name;

        #endregion

        #region cmdlet processing methods
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            cimGetSession = new CimGetSession();
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            cimGetSession.GetCimSession(this);
        }

        #endregion

        #region private members
        /// <summary>
        /// <see cref="CimGetSession"/> object used to search CimSession from cache.
        /// </summary>
        private CimGetSession cimGetSession;

        #region const string of parameter names
        internal const string nameComputerName = "ComputerName";
        internal const string nameId = "Id";
        internal const string nameInstanceId = "InstanceId";
        internal const string nameName = "Name";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        static Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new Dictionary<string, HashSet<ParameterDefinitionEntry>>
        {
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerNameSet, false),
                                 }
            },
            {
                nameId, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionIdSet, true),
                                 }
            },
            {
                nameInstanceId, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.InstanceIdSet, true),
                                 }
            },
            {
                nameName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.NameSet, true),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        static Dictionary<string, ParameterSetEntry> parameterSets = new Dictionary<string, ParameterSetEntry>
        {
            {   CimBaseCommand.ComputerNameSet, new ParameterSetEntry(0, true)     },
            {   CimBaseCommand.SessionIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.InstanceIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.NameSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
