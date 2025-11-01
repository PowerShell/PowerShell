// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Cmdletization
{
    /// <summary>
    /// Provides common code for processing session-based object models.  The common code
    /// Session, ThrottleLimit, AsJob parameters and delegates creation of jobs to derived classes.
    /// </summary>
    /// <typeparam name="TObjectInstance">Type that represents instances of objects from the wrapped object model</typeparam>
    /// <typeparam name="TSession">Type representing remote sessions</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1005:AvoidExcessiveParametersOnGenericTypes")]
    public abstract class SessionBasedCmdletAdapter<TObjectInstance, TSession> : CmdletAdapter<TObjectInstance>, IDisposable
        where TObjectInstance : class
        where TSession : class
    {
        internal SessionBasedCmdletAdapter()
        {
        }

        #region Constants

        private const string CIMJobType = "CimJob";

        #endregion

        #region IDisposable Members

        private bool _disposed;

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_parentJob != null)
                    {
                        _parentJob.Dispose();
                        _parentJob = null;
                    }
                }

                _disposed = true;
            }
        }

        #endregion

        #region Common parameters (AsJob, ThrottleLimit, Session)

        /// <summary>
        /// Session to operate on.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        protected TSession[] Session
        {
            get
            {
                return _session ??= new TSession[] { this.DefaultSession };
            }

            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _session = value;
                _sessionWasSpecified = true;
            }
        }

        private TSession[] _session;
        private bool _sessionWasSpecified;

        /// <summary>
        /// Whether to wrap and emit the whole operation as a background job.
        /// </summary>
        [Parameter]
        public SwitchParameter AsJob
        {
            get { return _asJob; }

            set { _asJob = value; }
        }

        private bool _asJob;

        /// <summary>
        /// Maximum number of remote connections that can remain active at any given time.
        /// </summary>
        [Parameter]
        public virtual int ThrottleLimit { get; set; }

        #endregion Common CIM-related parameters

        #region Abstract methods to be overridden in derived classes

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that performs a query against the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to query.</param>
        /// <param name="query">Query parameters.</param>
        /// <remarks>
        /// <para>
        /// This method shouldn't do any processing or interact with the remote session.
        /// Doing so will interfere with ThrottleLimit functionality.
        /// </para>
        /// <para>
        /// <see cref="Job.WriteObject"/> (and other methods returning job results) will block to support throttling and flow-control.
        /// Implementations of Job instance returned from this method should make sure that implementation-specific flow-control mechanism pauses further processing,
        /// until calls from <see cref="Job.WriteObject"/> (and other methods returning job results) return.
        /// </para>
        /// </remarks>
        internal abstract StartableJob CreateQueryJob(TSession session, QueryBuilder query);

        private StartableJob DoCreateQueryJob(TSession sessionForJob, QueryBuilder query, Action<TSession, TObjectInstance> actionAgainstResults)
        {
            StartableJob queryJob = this.CreateQueryJob(sessionForJob, query);

            if (queryJob != null)
            {
                if (actionAgainstResults != null)
                {
                    queryJob.SuppressOutputForwarding = true;
                }

                bool discardNonPipelineResults = (actionAgainstResults != null) || !this.AsJob.IsSpecified;
                HandleJobOutput(
                    queryJob,
                    sessionForJob,
                    discardNonPipelineResults,
                    actionAgainstResults == null
                        ? (Action<PSObject>)null
                        : ((PSObject pso) =>
                        {
                            var objectInstance =
                                (TObjectInstance)LanguagePrimitives.ConvertTo(
                                    pso,
                                    typeof(TObjectInstance),
                                    CultureInfo.InvariantCulture);
                            actionAgainstResults(sessionForJob, objectInstance);
                        }));
            }

            return queryJob;
        }

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that invokes an instance method in the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to invoke the method in.</param>
        /// <param name="objectInstance">The object on which to invoke the method.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the <paramref name="objectInstance"/> being operated on.</param>
        /// <remarks>
        /// <para>
        /// This method shouldn't do any processing or interact with the remote session.
        /// Doing so will interfere with ThrottleLimit functionality.
        /// </para>
        /// <para>
        /// <see cref="Job.WriteObject"/> (and other methods returning job results) will block to support throttling and flow-control.
        /// Implementations of Job instance returned from this method should make sure that implementation-specific flow-control mechanism pauses further processing,
        /// until calls from <see cref="Job.WriteObject"/> (and other methods returning job results) return.
        /// </para>
        /// </remarks>
        internal abstract StartableJob CreateInstanceMethodInvocationJob(TSession session, TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru);

        private StartableJob DoCreateInstanceMethodInvocationJob(TSession sessionForJob, TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru, bool asJob)
        {
            StartableJob methodInvocationJob = this.CreateInstanceMethodInvocationJob(sessionForJob, objectInstance, methodInvocationInfo, passThru);

            if (methodInvocationJob != null)
            {
                bool discardNonPipelineResults = !asJob;
                HandleJobOutput(
                    methodInvocationJob,
                    sessionForJob,
                    discardNonPipelineResults,
                    outputAction: null);
            }

            return methodInvocationJob;
        }

        /// <summary>
        /// Creates a <see cref="System.Management.Automation.Job"/> object that invokes a static method in the wrapped object model.
        /// </summary>
        /// <param name="session">Remote session to invoke the method in.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <remarks>
        /// <para>
        /// This method shouldn't do any processing or interact with the remote session.
        /// Doing so will interfere with ThrottleLimit functionality.
        /// </para>
        /// <para>
        /// <see cref="Job.WriteObject"/> (and other methods returning job results) will block to support throttling and flow-control.
        /// Implementations of Job instance returned from this method should make sure that implementation-specific flow-control mechanism pauses further processing,
        /// until calls from <see cref="Job.WriteObject"/> (and other methods returning job results) return.
        /// </para>
        /// </remarks>
        internal abstract StartableJob CreateStaticMethodInvocationJob(TSession session, MethodInvocationInfo methodInvocationInfo);

        private StartableJob DoCreateStaticMethodInvocationJob(TSession sessionForJob, MethodInvocationInfo methodInvocationInfo)
        {
            StartableJob methodInvocationJob = this.CreateStaticMethodInvocationJob(sessionForJob, methodInvocationInfo);

            if (methodInvocationJob != null)
            {
                bool discardNonPipelineResults = !this.AsJob.IsSpecified;
                HandleJobOutput(
                    methodInvocationJob,
                    sessionForJob,
                    discardNonPipelineResults,
                    outputAction: null);
            }

            return methodInvocationJob;
        }

        private static void HandleJobOutput(Job job, TSession sessionForJob, bool discardNonPipelineResults, Action<PSObject> outputAction)
        {
            Action<PSObject> processOutput =
                    (PSObject pso) =>
                    {
                        if (pso == null)
                        {
                            return;
                        }

                        outputAction?.Invoke(pso);
                    };

            job.Output.DataAdded +=
                    (object sender, DataAddedEventArgs eventArgs) =>
                    {
                        var dataCollection = (PSDataCollection<PSObject>)sender;

                        if (discardNonPipelineResults)
                        {
                            foreach (PSObject pso in dataCollection.ReadAll())
                            {
                                // TODO/FIXME - need to make sure that the dataCollection will be throttled
                                // (i.e. it won't accept more items - it will block in Add method)
                                // until this processOutput call completes
                                processOutput(pso);
                            }
                        }
                        else
                        {
                            PSObject pso = dataCollection[eventArgs.Index];
                            processOutput(pso);
                        }
                    };

            if (discardNonPipelineResults)
            {
                DiscardJobOutputs(job, JobOutputs.NonPipelineResults & (~JobOutputs.Output));
            }
        }

        internal virtual TSession GetSessionOfOriginFromInstance(TObjectInstance instance)
        {
            return null;
        }

        /// <summary>
        /// Returns default sessions to use when the user doesn't specify the -Session cmdlet parameter.
        /// </summary>
        /// <returns>Default sessions to use when the user doesn't specify the -Session cmdlet parameter.</returns>
        protected abstract TSession DefaultSession { get; }

        /// <summary>
        /// A new job name to use for the parent job that handles throttling of the child jobs that actually perform querying and method invocation.
        /// </summary>
        protected abstract string GenerateParentJobName();

        #endregion

        #region Helper methods

        private static void DiscardJobOutputs<T>(PSDataCollection<T> psDataCollection)
        {
            psDataCollection.DataAdded +=
                    (object sender, DataAddedEventArgs e) =>
                    {
                        var localDataCollection = (PSDataCollection<T>)sender;
                        localDataCollection.Clear();
                    };
        }

        [Flags]
        private enum JobOutputs
        {
            Output = 0x1,
            Error = 0x2,
            Warning = 0x4,
            Verbose = 0x8,
            Debug = 0x10,
            Progress = 0x20,
            Results = 0x40,

            NonPipelineResults = Output | Error | Warning | Verbose | Debug | Progress,
            PipelineResults = Results,
        }

        private static void DiscardJobOutputs(Job job, JobOutputs jobOutputsToDiscard)
        {
            if ((jobOutputsToDiscard & JobOutputs.Output) == JobOutputs.Output)
            {
                DiscardJobOutputs(job.Output);
            }

            if ((jobOutputsToDiscard & JobOutputs.Error) == JobOutputs.Error)
            {
                DiscardJobOutputs(job.Error);
            }

            if ((jobOutputsToDiscard & JobOutputs.Warning) == JobOutputs.Warning)
            {
                DiscardJobOutputs(job.Warning);
            }

            if ((jobOutputsToDiscard & JobOutputs.Verbose) == JobOutputs.Verbose)
            {
                DiscardJobOutputs(job.Verbose);
            }

            if ((jobOutputsToDiscard & JobOutputs.Debug) == JobOutputs.Debug)
            {
                DiscardJobOutputs(job.Debug);
            }

            if ((jobOutputsToDiscard & JobOutputs.Progress) == JobOutputs.Progress)
            {
                DiscardJobOutputs(job.Progress);
            }

            if ((jobOutputsToDiscard & JobOutputs.Results) == JobOutputs.Results)
            {
                DiscardJobOutputs(job.Results);
            }
        }

        #endregion Helper methods

        #region Implementation of ObjectModelWrapper functionality

        private ThrottlingJob _parentJob;

        /// <summary>
        /// Queries for object instances in the object model.
        /// </summary>
        /// <param name="query">Query parameters.</param>
        /// <returns>A lazy evaluated collection of object instances.</returns>
        public override void ProcessRecord(QueryBuilder query)
        {
            _parentJob.DisableFlowControlForPendingCmdletActionsQueue();
            foreach (TSession sessionForJob in this.GetSessionsToActAgainst(query))
            {
                StartableJob childJob = this.DoCreateQueryJob(sessionForJob, query, actionAgainstResults: null);
                if (childJob != null)
                {
                    if (!this.AsJob.IsSpecified)
                    {
                        _parentJob.AddChildJobAndPotentiallyBlock(this.Cmdlet, childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                    else
                    {
                        _parentJob.AddChildJobWithoutBlocking(childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                }
            }
        }

        /// <summary>
        /// Queries for instance and invokes an instance method.
        /// </summary>
        /// <param name="query">Query parameters.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the object instance being operated on.</param>
        public override void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            _parentJob.DisableFlowControlForPendingJobsQueue();

            ThrottlingJob closureOverParentJob = _parentJob;
            SwitchParameter closureOverAsJob = this.AsJob;

            foreach (TSession sessionForJob in this.GetSessionsToActAgainst(query))
            {
                StartableJob queryJob = this.DoCreateQueryJob(
                    sessionForJob,
                    query,
                    (TSession sessionForMethodInvocationJob, TObjectInstance objectInstance) =>
                    {
                        StartableJob methodInvocationJob = this.DoCreateInstanceMethodInvocationJob(
                            sessionForMethodInvocationJob,
                            objectInstance,
                            methodInvocationInfo,
                            passThru,
                            closureOverAsJob.IsSpecified);

                        if (methodInvocationJob != null)
                        {
                            closureOverParentJob.AddChildJobAndPotentiallyBlock(methodInvocationJob, ThrottlingJob.ChildJobFlags.None);
                        }
                    });

                if (queryJob != null)
                {
                    if (!this.AsJob.IsSpecified)
                    {
                        _parentJob.AddChildJobAndPotentiallyBlock(this.Cmdlet, queryJob, ThrottlingJob.ChildJobFlags.CreatesChildJobs);
                    }
                    else
                    {
                        _parentJob.AddChildJobWithoutBlocking(queryJob, ThrottlingJob.ChildJobFlags.CreatesChildJobs);
                    }
                }
            }
        }

        private IEnumerable<TSession> GetSessionsToActAgainst(TObjectInstance objectInstance)
        {
            if (_sessionWasSpecified)
            {
                return this.Session;
            }

            TSession associatedSession = this.GetSessionOfOriginFromInstance(objectInstance);
            if (associatedSession != null)
            {
                return new[] { associatedSession };
            }

            return new[] { this.GetImpliedSession() };
        }

        private TSession GetSessionAssociatedWithPipelineObject()
        {
            object inputVariableValue = this.Cmdlet.Context.GetVariableValue(SpecialVariables.InputVarPath, null);
            if (inputVariableValue == null)
            {
                return null;
            }

            IEnumerable inputEnumerable = LanguagePrimitives.GetEnumerable(inputVariableValue);
            if (inputEnumerable == null)
            {
                return null;
            }

            List<object> inputCollection = inputEnumerable.Cast<object>().ToList();
            if (inputCollection.Count != 1)
            {
                return null;
            }

            TObjectInstance inputInstance;
            if (!LanguagePrimitives.TryConvertTo(inputCollection[0], CultureInfo.InvariantCulture, out inputInstance))
            {
                return null;
            }

            TSession associatedSession = this.GetSessionOfOriginFromInstance(inputInstance);
            return associatedSession;
        }

        private IEnumerable<TSession> GetSessionsToActAgainst(QueryBuilder queryBuilder)
        {
            if (_sessionWasSpecified)
            {
                return this.Session;
            }

            if (queryBuilder is ISessionBoundQueryBuilder<TSession> sessionBoundQueryBuilder)
            {
                TSession sessionOfTheQueryBuilder = sessionBoundQueryBuilder.GetTargetSession();
                if (sessionOfTheQueryBuilder != null)
                {
                    return new[] { sessionOfTheQueryBuilder };
                }
            }

            TSession sessionAssociatedWithPipelineObject = this.GetSessionAssociatedWithPipelineObject();
            if (sessionAssociatedWithPipelineObject != null)
            {
                return new[] { sessionAssociatedWithPipelineObject };
            }

            return new[] { this.GetImpliedSession() };
        }

        private IEnumerable<TSession> GetSessionsToActAgainst(MethodInvocationInfo methodInvocationInfo)
        {
            if (_sessionWasSpecified)
            {
                return this.Session;
            }

            var associatedSessions = new HashSet<TSession>();
            foreach (TObjectInstance objectInstance in methodInvocationInfo.GetArgumentsOfType<TObjectInstance>())
            {
                TSession associatedSession = this.GetSessionOfOriginFromInstance(objectInstance);
                if (associatedSession != null)
                {
                    associatedSessions.Add(associatedSession);
                }
            }

            if (associatedSessions.Count == 1)
            {
                return associatedSessions;
            }

            TSession sessionAssociatedWithPipelineObject = this.GetSessionAssociatedWithPipelineObject();
            if (sessionAssociatedWithPipelineObject != null)
            {
                return new[] { sessionAssociatedWithPipelineObject };
            }

            return new[] { this.GetImpliedSession() };
        }

        internal PSModuleInfo PSModuleInfo
        {
            get
            {
                var scriptCommandInfo = this.Cmdlet.CommandInfo as IScriptCommandInfo;
                return scriptCommandInfo.ScriptBlock.Module;
            }
        }

        private TSession GetImpliedSession()
        {
            TSession sessionFromImportModule;
            // When being called from a CIM activity, this will be invoked as
            // a function so there will be no module info
            if (this.PSModuleInfo != null)
            {
                if (PSPrimitiveDictionary.TryPathGet(
                        this.PSModuleInfo.PrivateData as IDictionary,
                        out sessionFromImportModule,
                        ScriptWriter.PrivateDataKey_CmdletsOverObjects,
                        ScriptWriter.PrivateDataKey_DefaultSession))
                {
                    return sessionFromImportModule;
                }
            }

            return this.DefaultSession;
        }

        /// <summary>
        /// Invokes an instance method in the object model.
        /// </summary>
        /// <param name="objectInstance">The object on which to invoke the method.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the <paramref name="objectInstance"/> being operated on.</param>
        public override void ProcessRecord(TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            ArgumentNullException.ThrowIfNull(objectInstance);

            ArgumentNullException.ThrowIfNull(methodInvocationInfo);

            foreach (TSession sessionForJob in this.GetSessionsToActAgainst(objectInstance))
            {
                StartableJob childJob = this.DoCreateInstanceMethodInvocationJob(sessionForJob, objectInstance, methodInvocationInfo, passThru, this.AsJob.IsSpecified);
                if (childJob != null)
                {
                    if (!this.AsJob.IsSpecified)
                    {
                        _parentJob.AddChildJobAndPotentiallyBlock(this.Cmdlet, childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                    else
                    {
                        _parentJob.AddChildJobWithoutBlocking(childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes a static method in the object model.
        /// </summary>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        public override void ProcessRecord(MethodInvocationInfo methodInvocationInfo)
        {
            ArgumentNullException.ThrowIfNull(methodInvocationInfo);

            foreach (TSession sessionForJob in this.GetSessionsToActAgainst(methodInvocationInfo))
            {
                StartableJob childJob = this.DoCreateStaticMethodInvocationJob(sessionForJob, methodInvocationInfo);
                if (childJob != null)
                {
                    if (!this.AsJob.IsSpecified)
                    {
                        _parentJob.AddChildJobAndPotentiallyBlock(this.Cmdlet, childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                    else
                    {
                        _parentJob.AddChildJobWithoutBlocking(childJob, ThrottlingJob.ChildJobFlags.None);
                    }
                }
            }
        }

        /// <summary>
        /// Performs initialization of cmdlet execution.
        /// </summary>
        public override void BeginProcessing()
        {
            if (this.AsJob.IsSpecified)
            {
                MshCommandRuntime commandRuntime = (MshCommandRuntime)this.Cmdlet.CommandRuntime; // PSCmdlet.CommandRuntime is always MshCommandRuntime
                string conflictingParameter = null;
                if (commandRuntime.WhatIf.IsSpecified)
                {
                    conflictingParameter = "WhatIf";
                }
                else if (commandRuntime.Confirm.IsSpecified)
                {
                    conflictingParameter = "Confirm";
                }

                if (conflictingParameter != null)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        CmdletizationResources.SessionBasedWrapper_ShouldProcessVsJobConflict,
                        conflictingParameter);
                    throw new InvalidOperationException(errorMessage);
                }
            }

            _parentJob = new ThrottlingJob(
                command: Job.GetCommandTextFromInvocationInfo(this.Cmdlet.MyInvocation),
                jobName: this.GenerateParentJobName(),
                jobTypeName: CIMJobType,
                maximumConcurrentChildJobs: this.ThrottleLimit,
                cmdletMode: !this.AsJob.IsSpecified);
        }

        /// <summary>
        /// Performs cleanup after cmdlet execution.
        /// </summary>
        public override void EndProcessing()
        {
            _parentJob.EndOfChildJobs();
            if (this.AsJob.IsSpecified)
            {
                this.Cmdlet.WriteObject(_parentJob);
                this.Cmdlet.JobRepository.Add(_parentJob);
                _parentJob = null; // this class doesn't own parentJob after it has been emitted to the outside world
            }
            else
            {
                _parentJob.ForwardAllResultsToCmdlet(this.Cmdlet);
                _parentJob.Finished.WaitOne();
            }
        }

        /// <summary>
        /// Stops the parent job when called.
        /// </summary>
        public override void StopProcessing()
        {
            Job jobToStop = _parentJob;
            jobToStop?.StopJob();

            base.StopProcessing();
        }

        #endregion
    }

    internal interface ISessionBoundQueryBuilder<out TSession>
    {
        TSession GetTargetSession();
    }
}
