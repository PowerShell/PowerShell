// Copyright (c) Microsoft Corporation.
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
    /// Returns zero, one or more CIM (dynamic) instances with the properties
    /// specified in the Property parameter, KeysOnly parameter or the Select clause
    /// of the Query parameter.
    /// </summary>
    [Alias("gcim")]
    [Cmdlet(VerbsCommon.Get, "CimInstance", DefaultParameterSetName = CimBaseCommand.ClassNameComputerSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227961")]
    [OutputType(typeof(CimInstance))]
    public class GetCimInstanceCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCimInstanceCommand"/> class.
        /// Constructor.
        /// </summary>
        public GetCimInstanceCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "CimSession".
        /// Identifies the CimSession which is to be used to retrieve the instances.
        /// </para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CimSession[] CimSession
        {
            get
            {
                return cimSession;
            }

            set
            {
                cimSession = value;
                base.SetParameter(value, nameCimSession);
            }
        }

        private CimSession[] cimSession;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ClassName".
        /// Define the class name for which the instances are retrieved.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        public string ClassName
        {
            get
            {
                return className;
            }

            set
            {
                this.className = value;
                base.SetParameter(value, nameClassName);
            }
        }

        private string className;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ResourceUri".
        /// Define the Resource Uri for which the instances are retrieved.
        /// </para>
        /// </summary>
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public Uri ResourceUri
        {
            get
            {
                return resourceUri;
            }

            set
            {
                this.resourceUri = value;
                base.SetParameter(value, nameResourceUri);
            }
        }

        private Uri resourceUri;

        /// <summary>
        /// <para>The following is the definition of the input parameter "ComputerName".
        /// Provides the name of the computer from which to retrieve the instances. The
        /// ComputerName is used to create a temporary CimSession with default parameter
        /// values, which is then used to retrieve the instances.
        /// </para>
        /// <para>
        /// If no ComputerName is specified the default value is "localhost"
        /// </para>
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get
            {
                return computerName;
            }

            set
            {
                computerName = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computerName;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "KeyOnly".
        /// Indicates that only key properties of the retrieved instances should be
        /// returned to the client.
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        public SwitchParameter KeyOnly
        {
            get
            {
                return keyOnly;
            }

            set
            {
                keyOnly = value;
                base.SetParameter(value, nameKeyOnly);
            }
        }

        private SwitchParameter keyOnly;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Namespace".
        /// Identifies the Namespace in which the class, indicated by ClassName, is
        /// registered.
        /// </para>
        /// <para>
        /// Default namespace is 'root\cimv2' if this property is not specified.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public string Namespace
        {
            get
            {
                return nameSpace;
            }

            set
            {
                nameSpace = value;
                base.SetParameter(value, nameNamespace);
            }
        }

        private string nameSpace;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Specifies the operation timeout after which the client operation should be
        /// canceled. The default is the CimSession operation timeout. If this parameter
        /// is specified, then this value takes precedence over the CimSession
        /// OperationTimeout.
        /// </para>
        /// </summary>
        [Alias(AliasOT)]
        [Parameter]
        public uint OperationTimeoutSec { get; set; }

        /// <summary>
        /// <para>The following is the definition of the input parameter "InputObject".
        /// Provides the <see cref="CimInstance"/> that containing the [Key] properties,
        /// based on the key properties to retrieve the <see cref="CimInstance"/>.
        /// </para>
        /// <para>
        /// User can call New-CimInstance to create the CimInstance with key only
        /// properties, for example:
        /// New-CimInstance -ClassName C -Namespace root\cimv2
        ///  -Property @{CreationClassName="CIM_VirtualComputerSystem";Name="VM3358"}
        ///  -Keys {"CreationClassName", "Name"} -Local
        /// </para>
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
            get
            {
                return CimInstance;
            }

            set
            {
                CimInstance = value;
                base.SetParameter(value, nameCimInstance);
            }
        }

        /// <summary>
        /// Property for internal usage purpose.
        /// </summary>
        internal CimInstance CimInstance { get; private set; }

        /// <summary>
        /// The following is the definition of the input parameter "Query".
        /// Specifies the query string for what instances, and what properties of those
        /// instances, should be retrieve.
        /// </summary>
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public string Query
        {
            get
            {
                return query;
            }

            set
            {
                query = value;
                base.SetParameter(value, nameQuery);
            }
        }

        private string query;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "QueryDialect".
        /// Specifies the dialect used by the query Engine that interprets the Query
        /// string.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameComputerSet)]

        public string QueryDialect
        {
            get
            {
                return queryDialect;
            }

            set
            {
                queryDialect = value;
                base.SetParameter(value, nameQueryDialect);
            }
        }

        private string queryDialect;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Shallow".
        /// If the switch is set to True, only instance of the class identified by
        /// Namespace + ClassName will be returned. If the switch is not set, instances
        /// of the above class and of all of its descendents will be returned (the
        /// enumeration will cascade the class inheritance hierarchy).
        /// </para>
        /// </summary>
        [Parameter(ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.QuerySessionSet)]
        public SwitchParameter Shallow
        {
            get
            {
                return shallow;
            }

            set
            {
                shallow = value;
                base.SetParameter(value, nameShallow);
            }
        }

        private SwitchParameter shallow;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Filter".
        /// Specifies the where clause of the query.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        public string Filter
        {
            get
            {
                return filter;
            }

            set
            {
                filter = value;
                base.SetParameter(value, nameFilter);
            }
        }

        private string filter;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Property".
        /// Specifies the selected properties of result instances.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("SelectProperties")]
        public string[] Property
        {
            get
            {
                return SelectProperties;
            }

            set
            {
                SelectProperties = value;
                base.SetParameter(value, nameSelectProperties);
            }
        }
        /// <summary>
        /// Property for internal usage.
        /// </summary>
        internal string[] SelectProperties { get; private set; }

        #endregion

        #region cmdlet methods

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.CmdletOperation = new CmdletOperationBase(this);
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            this.CheckArgument();
            CimGetInstance cimGetInstance = this.GetOperationAgent() ?? CreateOperationAgent();

            cimGetInstance.GetCimInstance(this);
            cimGetInstance.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            CimGetInstance cimGetInstance = this.GetOperationAgent();
            cimGetInstance?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Get <see cref="CimGetInstance"/> object, which is
        /// used to delegate all Get-CimInstance operations, such
        /// as enumerate instances, get instance, query instance.
        /// </para>
        /// </summary>
        private CimGetInstance GetOperationAgent()
        {
            return this.AsyncOperation as CimGetInstance;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimGetInstance"/> object, which is
        /// used to delegate all Get-CimInstance operations, such
        /// as enumerate instances, get instance, query instance.
        /// </para>
        /// </summary>
        /// <returns></returns>
        private CimGetInstance CreateOperationAgent()
        {
            CimGetInstance cimGetInstance = new();
            this.AsyncOperation = cimGetInstance;
            return cimGetInstance;
        }

        /// <summary>
        /// Check argument value.
        /// </summary>
        private void CheckArgument()
        {
            switch (this.ParameterSetName)
            {
                case CimBaseCommand.ClassNameComputerSet:
                case CimBaseCommand.ClassNameSessionSet:
                    // validate the classname & property
                    this.className = ValidationHelper.ValidateArgumentIsValidName(nameClassName, this.className);
                    this.SelectProperties = ValidationHelper.ValidateArgumentIsValidName(nameSelectProperties, this.SelectProperties);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimInstance = "InputObject";
        internal const string nameCimSession = "CimSession";
        internal const string nameClassName = "ClassName";
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameComputerName = "ComputerName";
        internal const string nameFilter = "Filter";
        internal const string nameKeyOnly = "KeyOnly";
        internal const string nameNamespace = "Namespace";
        internal const string nameOperationTimeoutSec = "OperationTimeoutSec";
        internal const string nameQuery = "Query";
        internal const string nameQueryDialect = "QueryDialect";
        internal const string nameSelectProperties = "Property";
        internal const string nameShallow = "Shallow";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                 }
            },
            {
                nameClassName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
            {
                nameKeyOnly, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
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
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, true),
                                 }
            },
            {
                nameQueryDialect, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                 }
            },
            {
                nameShallow, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                 }
            },
            {
                nameFilter, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
            {
                nameSelectProperties, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.CimInstanceComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.CimInstanceSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ClassNameComputerSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.ClassNameSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.QueryComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ResourceUriSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ResourceUriComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.QuerySessionSet, new ParameterSetEntry(2)     }
        };
        #endregion
    }
}
