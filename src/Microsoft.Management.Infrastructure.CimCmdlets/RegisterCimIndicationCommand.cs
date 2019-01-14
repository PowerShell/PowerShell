// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Enables the user to subscribe to indications using Filter Expression or
    /// Query Expression.
    /// -SourceIdentifier is a name given to the subscription
    /// The Cmdlet should return a PS EventSubscription object that can be used to
    /// cancel the subscription
    /// Should we have the second parameter set with a -Query?
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "CimIndicationEvent", DefaultParameterSetName = CimBaseCommand.ClassNameComputerSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227960")]
    public class RegisterCimIndicationCommand : ObjectEventRegistrationBase
    {
        #region parameters

        /// <summary>
        /// <para>
        /// The following is the definition of the input parameter "Namespace".
        /// Specifies the NameSpace under which to look for the specified class name.
        /// </para>
        /// <para>
        /// Default value is root\cimv2
        /// </para>
        /// </summary>
        [Parameter]
        public string Namespace
        {
            get { return nameSpace; }

            set { nameSpace = value; }
        }

        private string nameSpace;

        /// <summary>
        /// The following is the definition of the input parameter "ClassName".
        /// Specifies the Class Name to register the indication on.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        [Parameter(Mandatory = true,
            Position = 0,
            ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        public string ClassName
        {
            get { return className; }

            set
            {
                className = value;
                this.SetParameter(value, nameClassName);
            }
        }

        private string className;

        /// <summary>
        /// The following is the definition of the input parameter "Query".
        /// The Query Expression to pass.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = CimBaseCommand.QueryExpressionSessionSet)]
        [Parameter(
            Mandatory = true,
            Position = 0,
            ParameterSetName = CimBaseCommand.QueryExpressionComputerSet)]
        public string Query
        {
            get { return query; }

            set
            {
                query = value;
                this.SetParameter(value, nameQuery);
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
        [Parameter(ParameterSetName = CimBaseCommand.QueryExpressionComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.QueryExpressionSessionSet)]
        public string QueryDialect
        {
            get { return queryDialect; }

            set
            {
                queryDialect = value;
                this.SetParameter(value, nameQueryDialect);
            }
        }

        private string queryDialect;

        /// <summary>
        /// The following is the definition of the input parameter "OperationTimeoutSec".
        /// Enables the user to specify the operation timeout in Seconds. This value
        /// overwrites the value specified by the CimSession Operation timeout.
        /// </summary>
        [Alias(CimBaseCommand.AliasOT)]
        [Parameter]
        public UInt32 OperationTimeoutSec
        {
            get { return operationTimeout; }

            set { operationTimeout = value; }
        }

        private UInt32 operationTimeout;

        /// <summary>
        /// The following is the definition of the input parameter "Session".
        /// Uses a CimSession context.
        /// </summary>
        [Parameter(
            Mandatory = true,
            ParameterSetName = CimBaseCommand.QueryExpressionSessionSet)]
        [Parameter(
            Mandatory = true,
            ParameterSetName = CimBaseCommand.ClassNameSessionSet)]
        public CimSession CimSession
        {
            get { return cimSession; }

            set
            {
                cimSession = value;
                this.SetParameter(value, nameCimSession);
            }
        }

        private CimSession cimSession;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Specifies the computer on which the commands associated with this session
        /// will run. The default value is LocalHost.
        /// </summary>
        [Alias(CimBaseCommand.AliasCN, CimBaseCommand.AliasServerName)]
        [Parameter(ParameterSetName = CimBaseCommand.QueryExpressionComputerSet)]
        [Parameter(ParameterSetName = CimBaseCommand.ClassNameComputerSet)]
        public string ComputerName
        {
            get { return computername; }

            set
            {
                computername = value;
                this.SetParameter(value, nameComputerName);
            }
        }

        private string computername;

        #endregion

        /// <summary>
        /// Returns the object that generates events to be monitored.
        /// </summary>
        protected override object GetSourceObject()
        {
            CimIndicationWatcher watcher = null;
            string parameterSetName = null;
            try
            {
                parameterSetName = this.parameterBinder.GetParameterSet();
            }
            finally
            {
                this.parameterBinder.reset();
            }

            string tempQueryExpression = string.Empty;
            switch (parameterSetName)
            {
                case CimBaseCommand.QueryExpressionSessionSet:
                case CimBaseCommand.QueryExpressionComputerSet:
                    tempQueryExpression = this.Query;
                    break;
                case CimBaseCommand.ClassNameSessionSet:
                case CimBaseCommand.ClassNameComputerSet:
                    // validate the classname
                    this.CheckArgument();
                    tempQueryExpression = string.Format(CultureInfo.CurrentCulture, "Select * from {0}", this.ClassName);
                    break;
            }

            switch (parameterSetName)
            {
                case CimBaseCommand.QueryExpressionSessionSet:
                case CimBaseCommand.ClassNameSessionSet:
                    {
                        watcher = new CimIndicationWatcher(this.CimSession, this.Namespace, this.QueryDialect, tempQueryExpression, this.OperationTimeoutSec);
                    }

                    break;
                case CimBaseCommand.QueryExpressionComputerSet:
                case CimBaseCommand.ClassNameComputerSet:
                    {
                        watcher = new CimIndicationWatcher(this.ComputerName, this.Namespace, this.QueryDialect, tempQueryExpression, this.OperationTimeoutSec);
                    }

                    break;
            }

            if (watcher != null)
            {
                watcher.SetCmdlet(this);
            }

            return watcher;
        }

        /// <summary>
        /// Returns the event name to be monitored on the input object.
        /// </summary>
        protected override string GetSourceObjectEventName()
        {
            return "CimIndicationArrived";
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            DebugHelper.WriteLogEx();

            base.EndProcessing();

            // Register for the "Unsubscribed" event so that we can stop the
            // Cimindication event watcher.
            PSEventSubscriber newSubscriber = NewSubscriber;
            if (newSubscriber != null)
            {
                DebugHelper.WriteLog("RegisterCimIndicationCommand::EndProcessing subscribe to Unsubscribed event", 4);
                newSubscriber.Unsubscribed += new PSEventUnsubscribedEventHandler(newSubscriber_Unsubscribed);
            }
        }

        /// <summary>
        /// <para>
        /// Handler to handle unsubscribe event
        /// </para>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void newSubscriber_Unsubscribed(
            object sender, PSEventUnsubscribedEventArgs e)
        {
            DebugHelper.WriteLogEx();

            CimIndicationWatcher watcher = sender as CimIndicationWatcher;
            if (watcher != null)
            {
                watcher.Stop();
            }
        }

        #region private members
        /// <summary>
        /// Check argument value.
        /// </summary>
        private void CheckArgument()
        {
            this.className = ValidationHelper.ValidateArgumentIsValidName(nameClassName, this.className);
        }

        /// <summary>
        /// Parameter binder used to resolve parameter set name.
        /// </summary>
        private ParameterBinder parameterBinder = new ParameterBinder(
            parameters, parameterSets);

        /// <summary>
        /// Set the parameter.
        /// </summary>
        /// <param name="parameterName"></param>
        private void SetParameter(object value, string parameterName)
        {
            if (value == null)
            {
                return;
            }

            this.parameterBinder.SetParameter(parameterName, true);
        }

        #region const string of parameter names
        internal const string nameClassName = "ClassName";
        internal const string nameQuery = "Query";
        internal const string nameQueryDialect = "QueryDialect";
        internal const string nameCimSession = "CimSession";
        internal const string nameComputerName = "ComputerName";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        static Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new Dictionary<string, HashSet<ParameterDefinitionEntry>>
        {
            {
                nameClassName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, true),
                                 }
            },
            {
                nameQuery, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionComputerSet, true),
                                 }
            },
            {
                nameQueryDialect, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionSessionSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionComputerSet, false),
                                 }
            },
            {
                nameCimSession, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionSessionSet, true),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameSessionSet, true),
                                 }
            },
            {
                nameComputerName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.QueryExpressionComputerSet, false),
                                    new ParameterDefinitionEntry(CimBaseCommand.ClassNameComputerSet, false),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        static Dictionary<string, ParameterSetEntry> parameterSets = new Dictionary<string, ParameterSetEntry>
        {
            {   CimBaseCommand.QueryExpressionSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.QueryExpressionComputerSet, new ParameterSetEntry(1)     },
            {   CimBaseCommand.ClassNameSessionSet, new ParameterSetEntry(2)     },
            {   CimBaseCommand.ClassNameComputerSet, new ParameterSetEntry(1, true)     },
        };

        #endregion
    }
}
