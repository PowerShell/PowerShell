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
    /// This cmdlet enables the user to invoke a static method on a CIM class using
    /// the arguments passed as a list of name value pair dictionary.
    /// </summary>

    [Cmdlet(
        "Invoke",
        "CimMethod",
        SupportsShouldProcess = true,
        DefaultParameterSetName = CimBaseCommand.ClassNameComputerSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227965")]
    public class InvokeCimMethodCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public InvokeCimMethodCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        /// <summary>
        /// The following is the definition of the input parameter "ClassName".
        /// Specifies the Class Name, on which to invoke static method.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Alias("Class")]
        public string ClassName
        {
            get { return className; }

            set
            {
                className = value;
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
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.CimInstanceSessionSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
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
        /// The following is the definition of the input parameter "CimClass".
        /// Specifies the <see cref="CimClass"/> object, on which to invoke static method.
        /// </summary>
        [Parameter(Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassComputerSet)]
        [Parameter(Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassSessionSet)]
        public CimClass CimClass
        {
            get { return cimClass; }

            set
            {
                cimClass = value;
                base.SetParameter(value, nameCimClass);
            }
        }

        private CimClass cimClass;

        /// <summary>
        /// The following is the definition of the input parameter "Query".
        /// Specifies the <see cref="CimClass"/> object, on which to invoke static method.
        /// </summary>
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(Mandatory = true,
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
        public string QueryDialect
        {
            get { return queryDialect; }

            set
            {
                queryDialect = value;
                base.SetParameter(value, nameQueryDialect);
            }
        }

        private string queryDialect;

        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Takes a CimInstance object retrieved by a Get-CimInstance call.
        /// Invoke the method against the given instance.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(Mandatory = true,
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
        /// <para>The following is the definition of the input parameter "ComputerName".
        /// Provides the name of the computer from which to invoke the method. The
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
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimInstanceComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.CimClassComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName
        {
            get { return computerName; }

            set
            {
                DebugHelper.WriteLogEx();
                computerName = value;
                base.SetParameter(value, nameComputerName);
            }
        }

        private string[] computerName;

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
            ParameterSetName = CimClassSessionSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
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
        /// The following is the definition of the input parameter "Arguments".
        /// Specifies the parameter arguments for the static method using a name value
        /// pair.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary Arguments
        {
            get { return arguments; }

            set { arguments = value; }
        }

        private IDictionary arguments;

        /// <summary>
        /// The following is the definition of the input parameter "MethodName".
        /// Name of the Static Method to use.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 2,
                   ValueFromPipelineByPropertyName = true)]
        [Alias("Name")]
        public string MethodName
        {
            get { return methodName; }

            set
            {
                methodName = value;
                base.SetParameter(value, nameMethodName);
            }
        }

        private string methodName;

        /// <summary>
        /// The following is the definition of the input parameter "Namespace".
        /// Specifies the NameSpace in which the class or instance lives under.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QueryComputerSet)]
        [Parameter(ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.QuerySessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
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
        /// Enables the user to specify the operation timeout in Seconds. This value
        /// overwrites the value specified by the CimSession Operation timeout.
        /// </summary>
        [Alias(AliasOT)]
        [Parameter]
        public UInt32 OperationTimeoutSec
        {
            get { return operationTimeout; }

            set { operationTimeout = value; }
        }

        private UInt32 operationTimeout;

        #endregion

        #region cmdlet methods

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            CimInvokeCimMethod cimInvokeMethod = this.GetOperationAgent();
            if (cimInvokeMethod == null)
            {
                cimInvokeMethod = CreateOperationAgent();
            }

            this.CmdletOperation = new CmdletOperationInvokeCimMethod(this, cimInvokeMethod);
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            this.CheckArgument();
            CimInvokeCimMethod cimInvokeMethod = this.GetOperationAgent();
            cimInvokeMethod.InvokeCimMethod(this);
            cimInvokeMethod.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            CimInvokeCimMethod cimInvokeMethod = this.GetOperationAgent();
            if (cimInvokeMethod != null)
            {
                cimInvokeMethod.ProcessRemainActions(this.CmdletOperation);
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Get <see cref="CimInvokeCimMethod"/> object, which is
        /// used to delegate all Invoke-CimMethod operations.
        /// </para>
        /// </summary>
        CimInvokeCimMethod GetOperationAgent()
        {
            return this.AsyncOperation as CimInvokeCimMethod;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimInvokeCimMethod"/> object, which is
        /// used to delegate all Invoke-CimMethod operations.
        /// </para>
        /// </summary>
        /// <returns></returns>
        CimInvokeCimMethod CreateOperationAgent()
        {
            CimInvokeCimMethod cimInvokeMethod = new CimInvokeCimMethod();
            this.AsyncOperation = cimInvokeMethod;
            return cimInvokeMethod;
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
                    // validate the classname
                    this.className = ValidationHelper.ValidateArgumentIsValidName(nameClassName, this.className);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameClassName = "ClassName";
        internal const string nameCimClass = "CimClass";
        internal const string nameQuery = "Query";
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameQueryDialect = "QueryDialect";
        internal const string nameCimInstance = "InputObject";
        internal const string nameComputerName = "ComputerName";
        internal const string nameCimSession = "CimSession";
        internal const string nameArguments = "Arguments";
        internal const string nameMethodName = "MethodName";
        internal const string nameNamespace = "Namespace";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        static Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new Dictionary<string, HashSet<ParameterDefinitionEntry>>
        {
            {
                nameClassName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                 }
            },
            {
                nameCimClass, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
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
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                 }
            },
            {
                nameMethodName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, true),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QuerySessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimInstanceSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, true),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        static Dictionary<string, ParameterSetEntry> parameterSets = new Dictionary<string, ParameterSetEntry>
        {
            {   CimBaseCommand.ClassNameComputerSet, new ParameterSetEntry(2, true)     },
            {   CimBaseCommand.ResourceUriSessionSet, new ParameterSetEntry(3)     },
            {   CimBaseCommand.ResourceUriComputerSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ClassNameSessionSet, new ParameterSetEntry(3)     },
            {   CimBaseCommand.QueryComputerSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.QuerySessionSet, new ParameterSetEntry(3)     },
            {   CimBaseCommand.CimInstanceComputerSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimInstanceSessionSet, new ParameterSetEntry(3)     },
            {   CimBaseCommand.CimClassComputerSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimClassSessionSet, new ParameterSetEntry(3)     },
        };
        #endregion
    }
}
