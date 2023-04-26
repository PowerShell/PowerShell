// Copyright (c) Microsoft Corporation.
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
    /// This Cmdlet creates an instance of a CIM class based on the class
    /// definition, which is an instance factory
    /// </para>
    /// <para>
    /// If -ClientOnly is not specified, New-CimInstance will create a new instance
    /// on the server, otherwise just create client in-memory instance
    /// </para>
    /// </summary>
    [Alias("ncim")]
    [Cmdlet(VerbsCommon.New, "CimInstance", DefaultParameterSetName = CimBaseCommand.ClassNameComputerSet, SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227963")]
    [OutputType(typeof(CimInstance))]
    public class NewCimInstanceCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="NewCimInstanceCommand"/> class.
        /// </summary>
        public NewCimInstanceCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        /// <summary>
        /// The following is the definition of the input parameter "ClassName".
        /// Name of the Class to use to create Instance.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            Mandatory = true,
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
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
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
        /// <para>
        /// The following is the definition of the input parameter "Key".
        /// Enables the user to specify list of key property name.
        /// </para>
        /// <para>
        /// Example: -Key {"K1", "K2"}
        /// </para>
        /// </summary>
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Key
        {
            get
            {
                return key;
            }

            set
            {
                key = value;
                base.SetParameter(value, nameKey);
            }
        }

        private string[] key;

        /// <summary>
        /// The following is the definition of the input parameter "CimClass".
        /// The CimClass is used to create Instance.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassSessionSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ParameterSetName = CimClassComputerSet)]
        public CimClass CimClass
        {
            get
            {
                return cimClass;
            }

            set
            {
                cimClass = value;
                base.SetParameter(value, nameCimClass);
            }
        }

        private CimClass cimClass;

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Property".
        /// Enables the user to specify instances with specific property values.
        /// </para>
        /// <para>
        /// Example: -Property @{P1="Value1";P2="Value2"}
        /// </para>
        /// </summary>
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("Arguments")]
        public IDictionary Property { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Namespace".
        /// Namespace used to look for the classes under to store the instances.
        /// Default namespace is 'root\cimv2'
        /// </summary>
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CimBaseCommand.ResourceUriComputerSet)]
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
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Operation Timeout of the cmdlet in seconds. Overrides the value in the Cim
        /// Session.
        /// </summary>
        [Alias(AliasOT)]
        [Parameter]
        public uint OperationTimeoutSec { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "CimSession".
        /// Identifies the CimSession which is to be used to create the instances.
        /// </para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimBaseCommand.ResourceUriSessionSet)]
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = CimClassSessionSet)]
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
        /// <para>The following is the definition of the input parameter "ComputerName".
        /// Provides the name of the computer from which to create the instances.
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
            ParameterSetName = CimClassComputerSet)]
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
        /// The following is the definition of the input parameter "ClientOnly".
        /// Indicates to create a client only ciminstance object, NOT on the server.
        /// </para>
        /// </summary>
        [Alias("Local")]
        [Parameter(
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimClassComputerSet)]
        [Parameter(
            ParameterSetName = CimBaseCommand.CimClassSessionSet)]
        public SwitchParameter ClientOnly
        {
            get
            {
                return clientOnly;
            }

            set
            {
                clientOnly = value;
                base.SetParameter(value, nameClientOnly);
            }
        }

        private SwitchParameter clientOnly;

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
            if (this.ClientOnly)
            {
                string conflictParameterName = null;
                if (this.ComputerName != null)
                {
                    conflictParameterName = @"ComputerName";
                }
                else if (this.CimSession != null)
                {
                    conflictParameterName = @"CimSession";
                }

                if (conflictParameterName != null)
                {
                    ThrowConflictParameterWasSet(@"New-CimInstance", conflictParameterName, @"ClientOnly");
                    return;
                }
            }

            CimNewCimInstance cimNewCimInstance = this.GetOperationAgent() ?? CreateOperationAgent();

            cimNewCimInstance.NewCimInstance(this);
            cimNewCimInstance.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            CimNewCimInstance cimNewCimInstance = this.GetOperationAgent();
            cimNewCimInstance?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Get <see cref="CimNewCimInstance"/> object, which is
        /// used to delegate all New-CimInstance operations.
        /// </para>
        /// </summary>
        private CimNewCimInstance GetOperationAgent()
        {
            return this.AsyncOperation as CimNewCimInstance;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimNewCimInstance"/> object, which is
        /// used to delegate all New-CimInstance operations.
        /// </para>
        /// </summary>
        /// <returns></returns>
        private CimNewCimInstance CreateOperationAgent()
        {
            CimNewCimInstance cimNewCimInstance = new();
            this.AsyncOperation = cimNewCimInstance;
            return cimNewCimInstance;
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
        internal const string nameResourceUri = "ResourceUri";
        internal const string nameKey = "Key";
        internal const string nameCimClass = "CimClass";
        internal const string nameProperty = "Property";
        internal const string nameNamespace = "Namespace";
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        internal const string nameClientOnly = "ClientOnly";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameClassName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                 }
            },
            {
                nameResourceUri, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, true),
                                 }
            },
            {
                nameKey, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                 }
            },
            {
                nameCimClass, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                 }
            },
            {
                nameNamespace, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, false),
                                 }
            },
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ResourceUriComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, false),
                                 }
            },
            {
                nameClientOnly, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.CimClassComputerSet, true),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.ClassNameSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ClassNameComputerSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.CimClassSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.CimClassComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ResourceUriSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ResourceUriComputerSet, new ParameterSetEntry(1)     },
        };
        #endregion
    }
}
