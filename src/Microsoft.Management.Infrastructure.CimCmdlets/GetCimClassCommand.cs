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
    /// <para>
    /// Enables the user to enumerate the list of CIM Classes under a specific
    /// Namespace. If no list of classes is given, the Cmdlet returns all
    /// classes in the given namespace.
    /// </para>
    /// <para>
    /// NOTES: The class instance contains the Namespace properties
    /// Should the class remember what Session it came from? No.
    /// </para>
    /// </summary>
    [Alias("gcls")]
    [Cmdlet(VerbsCommon.Get, GetCimClassCommand.Noun, DefaultParameterSetName = ComputerSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227959")]
    [OutputType(typeof(CimClass))]
    public class GetCimClassCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCimClassCommand"/> class.
        /// </summary>
        public GetCimClassCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region parameters

        /// <summary>
        /// Gets or sets flag to retrieve a localized data for WMI class.
        /// </summary>
        [Parameter]
        public SwitchParameter Amended { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "ClassName".
        /// </para>
        /// <para>
        /// Wildcard expansion should be allowed.
        /// </para>
        /// </summary>
        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        public string ClassName { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Namespace".
        /// Specifies the Namespace under which to look for the specified class name.
        /// If no class name is specified, the cmdlet should return all classes under
        /// the specified Namespace.
        /// </para>
        /// <para>
        /// Default namespace is root\cimv2
        /// </para>
        /// </summary>
        [Parameter(
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public string Namespace { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Enables the user to specify the operation timeout in Seconds. This value
        /// overwrites the value specified by the CimSession Operation timeout.
        /// </summary>
        [Alias(AliasOT)]
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public uint OperationTimeoutSec { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Session".
        /// Uses a CimSession context.
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = SessionSetName)]
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
        /// Provides the name of the computer from which to retrieve the <see cref="CimClass"/>
        /// </para>
        /// <para>
        /// If no ComputerName is specified the default value is "localhost"
        /// </para>
        /// </summary>
        [Alias(AliasCN, AliasServerName)]
        [Parameter(
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ComputerSetName)]
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
        /// The following is the definition of the input parameter "MethodName",
        /// Which may contains wildchar.
        /// Then Filter the <see cref="CimClass"/> by given methodname
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string MethodName { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "PropertyName",
        /// Which may contains wildchar.
        /// Filter the <see cref="CimClass"/> by given property name.
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string PropertyName { get; set; }

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "QualifierName",
        /// Which may contains wildchar.
        /// Filter the <see cref="CimClass"/> by given methodname
        /// </para>
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string QualifierName { get; set; }

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
            CimGetCimClass cimGetCimClass = this.GetOperationAgent() ?? CreateOperationAgent();

            cimGetCimClass.GetCimClass(this);
            cimGetCimClass.ProcessActions(this.CmdletOperation);
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            CimGetCimClass cimGetCimClass = this.GetOperationAgent();
            cimGetCimClass?.ProcessRemainActions(this.CmdletOperation);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// <para>
        /// Get <see cref="CimNewCimInstance"/> object, which is
        /// used to delegate all New-CimInstance operations.
        /// </para>
        /// </summary>
        private CimGetCimClass GetOperationAgent()
        {
            return this.AsyncOperation as CimGetCimClass;
        }

        /// <summary>
        /// <para>
        /// Create <see cref="CimGetCimClass"/> object, which is
        /// used to delegate all Get-CimClass operations.
        /// </para>
        /// </summary>
        /// <returns></returns>
        private CimGetCimClass CreateOperationAgent()
        {
            CimGetCimClass cimGetCimClass = new();
            this.AsyncOperation = cimGetCimClass;
            return cimGetCimClass;
        }

        #endregion

        #region internal const strings

        /// <summary>
        /// Noun of current cmdlet.
        /// </summary>
        internal const string Noun = @"CimClass";

        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        private static readonly Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new()
        {
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.SessionSetName, true),
                                 }
            },

            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ComputerSetName, false),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        private static readonly Dictionary<string, ParameterSetEntry> parameterSets = new()
        {
            {   CimBaseCommand.SessionSetName, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ComputerSetName, new ParameterSetEntry(0, true)     },
        };
        #endregion
    }
}
