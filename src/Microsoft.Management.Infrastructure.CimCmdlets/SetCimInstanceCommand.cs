// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Enables the user to Set properties and keys on a specific <see cref="CimInstance"/>
    /// CimInstance must have values of all [KEY] properties.
    /// </para>
    /// </summary>
    [Cmdlet(
        VerbsCommon.Set,
        "CimInstance",
        SupportsShouldProcess = true,
        DefaultParameterSetName = CimBaseCommand.CimInstanceComputerSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227962")]
    public class SetCimInstanceCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public SetCimInstanceCommand()
            : base(parameters, parameterSets)
        {
        }

        #endregion

        #region parameters
        /// <summary>
        /// The following is the definition of the input parameter "Session".
        /// CIM session used to set the CIM Instance.
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CimSession[] CimSession
        {
            get { return cimSession; }

            set
            {
                cimSession = value;
                base.SetParameter(value, nameCimSession);
            }
        }

        private CimSession[] cimSession;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get { return computername; }

            set
            {
                computername = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computername;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ResourceUri".
        /// Define the Resource Uri for which the instances are retrieved.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        public Uri ResourceUri
        {
            get { return resourceUri; }

            set
            {
                this.resourceUri = value;
                base.SetParameter(value, nameResourceUri);
            }
        }

        private Uri resourceUri;

        /// <summary>
        /// The following is the definition of the input parameter "Namespace".
        /// The Namespace used to look for the Class instances under.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        public string Namespace
        {
            get { return nameSpace; }

            set
            {
                nameSpace = value;
                base.SetParameter(value, nameNamespace);
            }
        }

        private string nameSpace;

        /// <summary>
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Used to set the invocation operation time out. This value overrides the
        /// CimSession operation timeout.
        /// </summary>
        [Alias(AliasOT)]
        [Parameter]
        public UInt32 OperationTimeoutSec
        {
            get { return operationTimeout; }

            set { operationTimeout = value; }
        }

        private UInt32 operationTimeout;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Used to get a CimInstance using Get-CimInstance | Set-CimInstance.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Alias(CimBaseCommand.AliasCimInstance)]
        public CimInstance InputObject
        {
            get { return cimInstance; }

            set
            {
                cimInstance = value;
                base.SetParameter(value, nameCimInstance);
            }
        }

        /// <summary>
        /// Property for internal usage purpose.
        /// </summary>
        internal CimInstance CimInstance
        {
            get { return cimInstance; }
        }

        private CimInstance cimInstance;

        /// <summary>
        /// The following is the definition of the input parameter "Query".
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public string Query
        {
            get { return query; }

            set
            {
                query = value;
                base.SetParameter(value, nameQuery);
            }
        }

        private string query;

        /// <summary>
        /// The following is the definition of the input parameter "QueryDialect".
        /// Specifies the dialect used by the query Engine that interprets the Query
        /// string.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QueryComputerSet)]
        public string QueryDialect
        {
            get { return querydialect; }

            set
            {
                querydialect = value;
                base.SetParameter(value, nameQueryDialect);
            }
        }

        private string querydialect;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Property",
        /// defines the value to be changed.
        /// </para>
        /// <para>
        /// The key properties will be ignored. Any invalid property will cause
        /// termination of the cmdlet execution.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("Arguments")]
        public IDictionary Property
        {
            get { return property; }

            set
            {
                property = value;
                base.SetParameter(value, nameProperty);
            }
        }

        private IDictionary property;

        /// <summary>
        /// The following is the definition of the input parameter "PassThru",
        /// indicate whether Set-CimInstance should output modified result instance or not.
        /// <para>
        /// True indicates output the result instance, otherwise output nothing as by default
        /// behavior.
        /// </para>
        /// </summary>
        [Parameter]
        [ValidateNotNull]
        public SwitchParameter PassThru
        {
            set
            {
                this.passThru = value;
            }

            get
            {
                return this.passThru;
            }
        }

        private SwitchParameter passThru;

        #endregion

        #region cmdlet methods

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            CimSetCimInstance cimSetCimInstance = this.GetOperationAgent();
            if (cimSetCimInstance == null)
            {
                cimSetCimInstance = CreateOperationAgent();
            }

            this.CmdletOperation = new CmdletOperationSetCimInstance(this, cimSetCimInstance);
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            CimSetCimInstance cimSetCimInstance = this.GetOperationAgent();
            cimSetCimInstance.SetCimInstance(this);
            cimSetCimInstance.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            CimSetCimInstance cimSetCimInstance = this.GetOperationAgent();
            if (cimSetCimInstance != null)
            {
                cimSetCimInstance.ProcessRemainActions(this.CmdletOperation);
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Get <see cref="CimSetCimInstance"/> object, which is
        /// used to delegate all Set-CimInstance operations.
        /// </para>
        /// </summary>
        CimSetCimInstance GetOperationAgent()
        {
            return (this.AsyncOperation as CimSetCimInstance);
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimSetCimInstance"/> object, which is
        /// used to delegate all Set-CimInstance operations.
        /// </para>
        /// </summary>
        /// <returns></returns>
        CimSetCimInstance CreateOperationAgent()
        {
            CimSetCimInstance cimSetCimInstance = new CimSetCimInstance();
            this.AsyncOperation = cimSetCimInstance;
            return cimSetCimInstance;
        }

        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameNamespace = "Namespace";
        internal const string nameCimInstance = "InputObject";
        internal const string nameQuery = "Query";
        internal const string nameQueryDialect = "QueryDialect";
        internal const string nameProperty = "Property";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        static Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new Dictionary<string, HashSet<ParameterDefinitionEntry>>
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                 }
            },
            {
                nameCimInstance, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                 }
            },
            {
                nameQuery, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                 }
            },
            {
                nameQueryDialect, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                 }
            },
            {
                nameProperty, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, false),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        static Dictionary<string, ParameterSetEntry> parameterSets = new Dictionary<string, ParameterSetEntry>
        {
            {   CimBaseCommand.QuerySessionSet, new ParameterSetEntry(3)     },
            {   CimBaseCommand.QueryComputerSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimInstanceSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimInstanceComputerSet, new ParameterSetEntry(1, true)     },
        };
        #endregion
    }
}
