// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

#endregion

/******************************************************************************
 * warning 28750: Banned usage of lstrlen and its variants: lstrlenW is a
 * banned API for improved error handling purposes.
 *****************************************************************************/
namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// This Cmdlet allows the to remove, or terminate, one or more CimSession(s).
    /// </summary>
    [Alias("rcms")]
    [Cmdlet(VerbsCommon.Remove, "CimSession",
             SupportsShouldProcess = true,
             DefaultParameterSetName = CimSessionSet,
             HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227968")]
    public sealed class RemoveCimSessionCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveCimSessionCommand"/> class.
        /// </summary>
        public RemoveCimSessionCommand()
            : base(parameters, parameterSets)
        {
        }

        #endregion

        #region parameters

        /// <summary>
        /// The following is the definition of the input parameter "CimSession".
        /// Specifies one or more CimSession object to be removed from the local PS
        /// session/runspace.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimSessionSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CimSession[] CimSession
        {
            get
            {
                return cimsession;
            }

            set
            {
                cimsession = value;
                base.SetParameter(value, nameCimSession);
            }
        }

        private CimSession[] cimsession;

        /// <summary>
        /// <para>The following is the definition of the input parameter "ComputerName".
        /// Specified one or more computer names for which all CimSession(s)
        /// (connections) should be removed (terminated).</para>
        /// <para>This is the only optional parameter. If no value for this parameter is
        /// provided, all CimSession(s) are terminated.</para>
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ComputerNameSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computername;

        /// <summary>
        /// The following is the definition of the input parameter "Id".
        /// Specifies the friendly Id(s) of the CimSession(s) that should be removed
        /// (terminated).
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = SessionIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public uint[] Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
                base.SetParameter(value, nameId);
            }
        }

        private uint[] id;

        /// <summary>
        /// The following is the definition of the input parameter "InstanceId".
        /// Specifies one or more automatically generated InstanceId(s) (GUIDs) of the
        /// CimSession(s) that should be removed (terminated).
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = InstanceIdSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Guid[] InstanceId
        {
            get
            {
                return instanceid;
            }

            set
            {
                instanceid = value;
                base.SetParameter(value, nameInstanceId);
            }
        }

        private Guid[] instanceid;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies one or more of friendly Names of the CimSession(s) that should be
        /// removed (terminated).
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = NameSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                base.SetParameter(value, nameName);
            }
        }

        private string[] name;

        #endregion

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.cimRemoveSession = new CimRemoveSession();
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            this.cimRemoveSession.RemoveCimSession(this);
        }

        #region private members
        /// <summary>
        /// <see cref="CimRemoveSession"/> object used to remove the session from
        /// session cache.
        /// </summary>
        private CimRemoveSession cimRemoveSession;

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameId = "Id";
        internal const string nameInstanceId = "InstanceId";
        internal const string nameName = "Name";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimSessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerNameSet, true),
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
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.CimSessionSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.ComputerNameSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.SessionIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.InstanceIdSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.NameSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
