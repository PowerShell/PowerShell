// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Management.Infrastructure;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// CIM-specific ObjectModelWrapper.
    /// </summary>
    public sealed class CimCmdletAdapter :
        SessionBasedCmdletAdapter<CimInstance, CimSession>,
        IDynamicParameters
    {
        #region Special method and parameter names

        internal const string CreateInstance_MethodName = "cim:CreateInstance";
        internal const string ModifyInstance_MethodName = "cim:ModifyInstance";
        internal const string DeleteInstance_MethodName = "cim:DeleteInstance";

        #endregion

        #region Changing Session parameter to CimSession

        /// <summary>
        /// CimSession to operate on.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Alias("Session")]
        public CimSession[] CimSession
        {
            get
            {
                return base.Session;
            }

            set
            {
                base.Session = value;
            }
        }

        /// <summary>
        /// Maximum number of remote connections that can remain active at any given time.
        /// </summary>
        [Parameter]
        public override int ThrottleLimit
        {
            get
            {
                if (_throttleLimitIsSetExplicitly)
                {
                    return base.ThrottleLimit;
                }

                return this.CmdletDefinitionContext.DefaultThrottleLimit;
            }

            set
            {
                base.ThrottleLimit = value;
                _throttleLimitIsSetExplicitly = true;
            }
        }

        private bool _throttleLimitIsSetExplicitly;

        #endregion

        #region ObjectModelWrapper overrides

        /// <summary>
        /// Creates a query builder for CIM OM.
        /// </summary>
        /// <returns>Query builder for CIM OM.</returns>
        public override QueryBuilder GetQueryBuilder()
        {
            return new CimQuery();
        }

        internal CimCmdletInvocationContext CmdletInvocationContext
        {
            get
            {
                return _cmdletInvocationContext ??
                    (_cmdletInvocationContext = new CimCmdletInvocationContext(
                        this.CmdletDefinitionContext,
                        this.Cmdlet,
                        this.GetDynamicNamespace()));
            }
        }

        private CimCmdletInvocationContext _cmdletInvocationContext;

        internal CimCmdletDefinitionContext CmdletDefinitionContext
        {
            get
            {
                if (_cmdletDefinitionContext == null)
                {
                    _cmdletDefinitionContext = new CimCmdletDefinitionContext(
                            this.ClassName,
                            this.ClassVersion,
                            this.ModuleVersion,
                            this.Cmdlet.CommandInfo.CommandMetadata.SupportsShouldProcess,
                            this.PrivateData);
                }

                return _cmdletDefinitionContext;
            }
        }

        private CimCmdletDefinitionContext _cmdletDefinitionContext;

        internal InvocationInfo CmdletInvocationInfo
        {
            get { return this.CmdletInvocationContext.CmdletInvocationInfo; }
        }

        #endregion ObjectModelWrapper overrides

        #region SessionBasedCmdletAdapter overrides

        private static long s_jobNumber;

        /// <summary>
        /// Returns a new job name to use for the parent job that handles throttling of the child jobs that actually perform querying and method invocation.
        /// </summary>
        /// <returns>Job name.</returns>
        protected override string GenerateParentJobName()
        {
            return "CimJob" + Interlocked.Increment(ref CimCmdletAdapter.s_jobNumber).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns default sessions to use when the user doesn't specify the -Session cmdlet parameter.
        /// </summary>
        /// <returns>Default sessions to use when the user doesn't specify the -Session cmdlet parameter.</returns>
        protected override CimSession DefaultSession
        {
            get
            {
                return this.CmdletInvocationContext.GetDefaultCimSession();
            }
        }

        private CimJobContext CreateJobContext(CimSession session, object targetObject)
        {
            return new CimJobContext(
                this.CmdletInvocationContext,
                session,
                targetObject);
        }

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that performs a query against the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to query.</param>
        /// <param name="baseQuery">Query parameters.</param>
        /// <returns><see cref="System.Management.Automation.Job"/> object that performs a query against the wrapped object model.</returns>
        internal override StartableJob CreateQueryJob(CimSession session, QueryBuilder baseQuery)
        {
            CimQuery query = baseQuery as CimQuery;
            if (query == null)
            {
                throw new ArgumentNullException("baseQuery");
            }

            TerminatingErrorTracker tracker = TerminatingErrorTracker.GetTracker(this.CmdletInvocationInfo, isStaticCmdlet: false);
            if (tracker.IsSessionTerminated(session))
            {
                return null;
            }

            if (!IsSupportedSession(session, tracker))
            {
                return null;
            }

            CimJobContext jobContext = this.CreateJobContext(session, targetObject: null);
            StartableJob queryJob = query.GetQueryJob(jobContext);

            return queryJob;
        }

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that invokes an instance method in the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to invoke the method in.</param>
        /// <param name="objectInstance">The object on which to invoke the method.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><c>true</c> if successful method invocations should emit downstream the <paramref name="objectInstance"/> being operated on.</param>
        /// <returns></returns>
        internal override StartableJob CreateInstanceMethodInvocationJob(CimSession session, CimInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            TerminatingErrorTracker tracker = TerminatingErrorTracker.GetTracker(this.CmdletInvocationInfo, isStaticCmdlet: false);
            if (tracker.IsSessionTerminated(session))
            {
                return null;
            }

            if (!IsSupportedSession(session, tracker))
            {
                return null;
            }

            CimJobContext jobContext = this.CreateJobContext(session, objectInstance);

            Dbg.Assert(objectInstance != null, "Caller should verify objectInstance != null");

            StartableJob result;
            if (methodInvocationInfo.MethodName.Equals(CimCmdletAdapter.DeleteInstance_MethodName, StringComparison.OrdinalIgnoreCase))
            {
                result = new DeleteInstanceJob(
                    jobContext,
                    passThru,
                    objectInstance,
                    methodInvocationInfo);
            }
            else if (methodInvocationInfo.MethodName.Equals(CimCmdletAdapter.ModifyInstance_MethodName, StringComparison.OrdinalIgnoreCase))
            {
                result = new ModifyInstanceJob(
                    jobContext,
                    passThru,
                    objectInstance,
                    methodInvocationInfo);
            }
            else
            {
                result = new InstanceMethodInvocationJob(
                    jobContext,
                    passThru,
                    objectInstance,
                    methodInvocationInfo);
            }

            return result;
        }

        private bool IsSupportedSession(CimSession cimSession, TerminatingErrorTracker terminatingErrorTracker)
        {
            bool confirmSwitchSpecified = this.CmdletInvocationInfo.BoundParameters.ContainsKey("Confirm");
            bool whatIfSwitchSpecified = this.CmdletInvocationInfo.BoundParameters.ContainsKey("WhatIf");
            if (confirmSwitchSpecified || whatIfSwitchSpecified)
            {
                if (cimSession.ComputerName != null && (!cimSession.ComputerName.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
                {
                    PSPropertyInfo protocolProperty = PSObject.AsPSObject(cimSession).Properties["Protocol"];
                    if ((protocolProperty != null) &&
                        (protocolProperty.Value != null) &&
                        (protocolProperty.Value.ToString().Equals("DCOM", StringComparison.OrdinalIgnoreCase)))
                    {
                        bool sessionWasAlreadyTerminated;
                        terminatingErrorTracker.MarkSessionAsTerminated(cimSession, out sessionWasAlreadyTerminated);
                        if (!sessionWasAlreadyTerminated)
                        {
                            string nameOfUnsupportedSwitch;
                            if (confirmSwitchSpecified)
                            {
                                nameOfUnsupportedSwitch = "-Confirm";
                            }
                            else
                            {
                                Dbg.Assert(whatIfSwitchSpecified, "Confirm and WhatIf are the only detected settings");
                                nameOfUnsupportedSwitch = "-WhatIf";
                            }

                            string errorMessage = string.Format(
                                CultureInfo.InvariantCulture,
                                CmdletizationResources.CimCmdletAdapter_RemoteDcomDoesntSupportExtendedSemantics,
                                cimSession.ComputerName,
                                nameOfUnsupportedSwitch);
                            Exception exception = new NotSupportedException(errorMessage);
                            ErrorRecord errorRecord = new ErrorRecord(
                                exception,
                                "NoExtendedSemanticsSupportInRemoteDcomProtocol",
                                ErrorCategory.NotImplemented,
                                cimSession);
                            this.Cmdlet.WriteError(errorRecord);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that invokes a static method
        /// (of the class named by <see cref="Microsoft.PowerShell.Cmdletization.CmdletAdapter&lt;TObjectInstance&gt;.ClassName"/>)
        /// in the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to invoke the method in.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        internal override StartableJob CreateStaticMethodInvocationJob(CimSession session, MethodInvocationInfo methodInvocationInfo)
        {
            TerminatingErrorTracker tracker = TerminatingErrorTracker.GetTracker(this.CmdletInvocationInfo, isStaticCmdlet: true);
            if (tracker.IsSessionTerminated(session))
            {
                return null;
            }

            if (!IsSupportedSession(session, tracker))
            {
                return null;
            }

            CimJobContext jobContext = this.CreateJobContext(session, targetObject: null);

            StartableJob result;
            if (methodInvocationInfo.MethodName.Equals(CimCmdletAdapter.CreateInstance_MethodName, StringComparison.OrdinalIgnoreCase))
            {
                result = new CreateInstanceJob(
                    jobContext,
                    methodInvocationInfo);
            }
            else
            {
                result = new StaticMethodInvocationJob(
                    jobContext,
                    methodInvocationInfo);
            }

            return result;
        }

        #endregion SessionBasedCmdletAdapter overrides

        #region Session affinity management

        private static readonly ConditionalWeakTable<CimInstance, CimSession> s_cimInstanceToSessionOfOrigin = new ConditionalWeakTable<CimInstance, CimSession>();

        internal static void AssociateSessionOfOriginWithInstance(CimInstance cimInstance, CimSession sessionOfOrigin)
        {
            // GetValue adds value to the table, if the key is not present in the table
            s_cimInstanceToSessionOfOrigin.GetValue(cimInstance, _ => sessionOfOrigin);
        }

        internal static CimSession GetSessionOfOriginFromCimInstance(CimInstance instance)
        {
            CimSession result = null;
            if (instance != null)
            {
                s_cimInstanceToSessionOfOrigin.TryGetValue(instance, out result);
            }

            return result;
        }

        internal override CimSession GetSessionOfOriginFromInstance(CimInstance instance)
        {
            return GetSessionOfOriginFromCimInstance(instance);
        }

        #endregion

        #region Handling of dynamic parameters

        private RuntimeDefinedParameterDictionary _dynamicParameters;
        private const string CimNamespaceParameter = "CimNamespace";

        private string GetDynamicNamespace()
        {
            if (_dynamicParameters == null)
            {
                return null;
            }

            RuntimeDefinedParameter runtimeParameter;
            if (!_dynamicParameters.TryGetValue(CimNamespaceParameter, out runtimeParameter))
            {
                return null;
            }

            return runtimeParameter.Value as string;
        }

        object IDynamicParameters.GetDynamicParameters()
        {
            if (_dynamicParameters == null)
            {
                _dynamicParameters = new RuntimeDefinedParameterDictionary();

                if (this.CmdletDefinitionContext.ExposeCimNamespaceParameter)
                {
                    Collection<Attribute> namespaceAttributes = new Collection<Attribute>();
                    namespaceAttributes.Add(new ValidateNotNullOrEmptyAttribute());
                    namespaceAttributes.Add(new ParameterAttribute());
                    RuntimeDefinedParameter namespaceRuntimeParameter = new RuntimeDefinedParameter(
                        CimNamespaceParameter,
                        typeof(string),
                        namespaceAttributes);
                    _dynamicParameters.Add(CimNamespaceParameter, namespaceRuntimeParameter);
                }
            }

            return _dynamicParameters;
        }

        #endregion
    }
}
