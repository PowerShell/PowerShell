//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Activities;
using System.Activities.Validation;
using System.Runtime.DurableInstancing;
using System.Collections.Generic;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerShell.Activities;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;
using System.Activities.Persistence;
using System.Activities.Hosting;

namespace Microsoft.PowerShell.Workflow
{
    /// <summary>
    /// WorkflowStoreComponent
    /// </summary>
    [Flags]
    public enum WorkflowStoreComponents
    {
        /// <summary>
        /// Streams
        /// </summary>
        Streams = 1,

        /// <summary>
        /// Metadata
        /// </summary>
        Metadata = 2,

        /// <summary>
        /// Definition
        /// </summary>
        Definition = 4,

        /// <summary>
        /// Timers 
        /// </summary>
        Timer = 8,

        /// <summary>
        /// JobState 
        /// </summary>
        JobState = 16,

        /// <summary>
        /// TerminatingError
        /// </summary>
        TerminatingError = 32,

        /// <summary>
        /// ActivityState, like remote activity execution state, etc.
        /// </summary>
        ActivityState = 64,
    }

    /// <summary>
    /// ActivityRunMode
    /// </summary>
    public enum ActivityRunMode
    {
        /// <summary>
        /// InProcess
        /// </summary>
        InProcess = 0,
        
        /// <summary>
        /// OutOfProcess
        /// </summary>
        OutOfProcess = 1,
    }

    /// <summary>
    /// WorkflowInstanceStore
    /// </summary>
    public abstract class PSWorkflowInstanceStore
    {
        /// <summary>
        /// PSWorkflowInstanceStore
        /// </summary>
        /// <param name="workflowInstance"></param>
        protected PSWorkflowInstanceStore(PSWorkflowInstance workflowInstance)
        {
            if (workflowInstance == null)
                throw new ArgumentNullException("workflowInstance");

            PSWorkflowInstance = workflowInstance;
        }

        /// <summary>
        /// PSWorkflowInstance
        /// </summary>
        public PSWorkflowInstance PSWorkflowInstance
        {
            get;
            private set;
        }

        /// <summary>
        /// CreatePersistenceIOParticipant
        /// </summary>
        /// <returns></returns>
        public abstract PersistenceIOParticipant CreatePersistenceIOParticipant();

        /// <summary>
        /// CreateInstanceStore
        /// </summary>
        /// <returns></returns>
        public abstract InstanceStore CreateInstanceStore();

        #region Save Methods

        /// <summary>
        /// Save
        /// </summary>
        /// <param name="components"></param>
        public void Save(WorkflowStoreComponents components)
        {
            this.Save(components, null);
        }

        internal void Save(WorkflowStoreComponents components, Dictionary<string, object> WorkflowContext)
        {
            Collection<object> componentsToSave = new Collection<object>();

            if ((components & WorkflowStoreComponents.JobState) == WorkflowStoreComponents.JobState)
            {
                componentsToSave.Add(PSWorkflowInstance.State);
            }

            if (WorkflowContext != null)
            {
                componentsToSave.Add(WorkflowContext);
            }

            if (((components & WorkflowStoreComponents.Definition) == WorkflowStoreComponents.Definition) &&
                (PSWorkflowInstance.PSWorkflowDefinition != null))
            {
                componentsToSave.Add(PSWorkflowInstance.PSWorkflowDefinition);
            }

            if (((components & WorkflowStoreComponents.TerminatingError) == WorkflowStoreComponents.TerminatingError) &&
                (PSWorkflowInstance.Error != null))
            {
                if (!WorkflowJobSourceAdapter.GetInstance().IsShutdownInProgress)
                {
                    if (PSWorkflowInstance.Error.GetType() != typeof(RemoteException))
                    {
                        componentsToSave.Add(PSWorkflowInstance.Error);
                    }
                }
            }

            if (((components & WorkflowStoreComponents.Metadata) == WorkflowStoreComponents.Metadata) &&
                (PSWorkflowInstance.PSWorkflowContext != null))
            {
                componentsToSave.Add(PSWorkflowInstance.PSWorkflowContext);
            }

            if (((components & WorkflowStoreComponents.Streams) == WorkflowStoreComponents.Streams) &&
                (PSWorkflowInstance.Streams != null))
            {
                componentsToSave.Add(PSWorkflowInstance.Streams);
            }

            if (((components & WorkflowStoreComponents.ActivityState) == WorkflowStoreComponents.ActivityState) &&
                (PSWorkflowInstance.RemoteActivityState != null))
            {
                componentsToSave.Add(PSWorkflowInstance.RemoteActivityState);
            }

            if (((components & WorkflowStoreComponents.Timer) == WorkflowStoreComponents.Timer) &&
                PSWorkflowInstance.Timer != null)
            {
                componentsToSave.Add(PSWorkflowInstance.Timer);
            }

            DoSave(componentsToSave);
        }

        /// <summary>
        /// DoSave
        /// </summary>
        /// <param name="components"></param>
        protected abstract void DoSave(IEnumerable<object> components);
        
        #endregion Save

        #region Load  Methods

        /// <summary>
        /// Load
        /// </summary>
        /// <param name="components"></param>
        public void Load(WorkflowStoreComponents components)
        {
            Collection<Type> componentsToLoad = new Collection<Type>();

            if ((components & WorkflowStoreComponents.JobState) == WorkflowStoreComponents.JobState)
            {
                componentsToLoad.Add(typeof(JobState));
                PSWorkflowInstance.JobStateRetrieved = false;
            }

            if ((components & WorkflowStoreComponents.Definition) == WorkflowStoreComponents.Definition)
            {
                componentsToLoad.Add(typeof(PSWorkflowDefinition));
                PSWorkflowInstance.PSWorkflowDefinition = null;
            }

            if ((components & WorkflowStoreComponents.TerminatingError) == WorkflowStoreComponents.TerminatingError)
            {
                componentsToLoad.Add(typeof(Exception));
                PSWorkflowInstance.Error = null;
            }

            if ((components & WorkflowStoreComponents.Metadata) == WorkflowStoreComponents.Metadata)
            {
                componentsToLoad.Add(typeof(PSWorkflowContext));
                PSWorkflowInstance.PSWorkflowContext = null;
            }

            if ((components & WorkflowStoreComponents.Streams) == WorkflowStoreComponents.Streams)
            {
                componentsToLoad.Add(typeof(PowerShellStreams<PSObject, PSObject>));
                PSWorkflowInstance.Streams = null;
            }

            if ((components & WorkflowStoreComponents.ActivityState) == WorkflowStoreComponents.ActivityState)
            {
                componentsToLoad.Add(typeof(PSWorkflowRemoteActivityState));
                PSWorkflowInstance.RemoteActivityState = null;
            }

            if ((components & WorkflowStoreComponents.Timer) == WorkflowStoreComponents.Timer)
            {
                componentsToLoad.Add(typeof(PSWorkflowTimer));
                PSWorkflowInstance.Timer = null;
            }

            IEnumerable<object> loadedComponents = DoLoad(componentsToLoad);
            
            foreach (object loadedComponent in loadedComponents)
            {
                Type componentType = loadedComponent.GetType();

                if (componentType == typeof(JobState))
                {
                    PSWorkflowInstance.State = (JobState)loadedComponent;
                    PSWorkflowInstance.JobStateRetrieved = true;
                }
                else if (componentType == typeof(PSWorkflowDefinition))
                {
                    PSWorkflowInstance.PSWorkflowDefinition = (PSWorkflowDefinition)loadedComponent;
                }
                else if (loadedComponent is Exception)
                {
                    PSWorkflowInstance.Error = (Exception)loadedComponent;
                }
                else if (componentType == typeof(PSWorkflowContext))
                {
                    PSWorkflowInstance.PSWorkflowContext = (PSWorkflowContext)loadedComponent;
                }
                else if (componentType == typeof(PowerShellStreams<PSObject, PSObject>))
                {
                    PSWorkflowInstance.Streams = (PowerShellStreams<PSObject, PSObject>)loadedComponent;
                }
                else if (componentType == typeof(PSWorkflowTimer))
                {
                    PSWorkflowInstance.Timer = (PSWorkflowTimer)loadedComponent;
                }
                else if (componentType == typeof(PSWorkflowRemoteActivityState))
                {
                    PSWorkflowInstance.RemoteActivityState = (PSWorkflowRemoteActivityState)loadedComponent;
                }
            }
        }

        internal Dictionary<string, object> LoadWorkflowContext()
        {
            Dictionary<string, object> workflowContext = null;

            Collection<Type> componentsToLoad = new Collection<Type>();
            componentsToLoad.Add(typeof(Dictionary<string, object>));

            IEnumerable<object> loadedComponents = DoLoad(componentsToLoad);

            foreach (object loadedComponent in loadedComponents)
            {
                Type componentType = loadedComponent.GetType();

                if (componentType == typeof(Dictionary<string, object>))
                {
                    workflowContext = (Dictionary<string, object>)loadedComponent;
                }
            }

            return workflowContext;
        }

        /// <summary>
        /// DoLoad
        /// </summary>
        /// <param name="componentTypes"></param>
        protected abstract IEnumerable<object> DoLoad(IEnumerable<Type> componentTypes);

        #endregion Load  Methods

        #region Delete  Methods

        /// <summary>
        /// Delete
        /// </summary>
        public void Delete()
        {
            DoDelete();
        }

        /// <summary>
        /// DoDelete
        /// </summary>
        protected abstract void DoDelete();

        #endregion Delete Methods
    }


    /// <summary>
    /// WorkflowInstance
    /// </summary>
    public abstract class PSWorkflowInstance : IDisposable
    {
        #region Private Members

        /// <summary>
        /// _syncLock 
        /// </summary>
        private object _syncLock = new object();

        #endregion Private Members

        #region Protected Members

        /// <summary>
        /// _disposed
        /// </summary>
        protected bool Disposed
        {
            set;
            get;
        }

        /// <summary>
        /// Synchronization object available to derived classes.
        /// </summary>
        protected object SyncLock
        {
            get
            {
                return _syncLock;
            }
        }


        /// <summary>
        /// DoStopInstance
        /// </summary>
        protected virtual void DoStopInstance()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoAbortInstance
        /// </summary>
        /// <param name="reason">Reason for aborting workflow.</param>
        protected virtual void DoAbortInstance(string reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoTerminateInstance
        /// </summary>
        /// <param name="reason">Reason message for termination</param>
        protected virtual void DoTerminateInstance(string reason)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoTerminateInstance
        /// </summary>
        /// <param name="reason">Reason message for termination</param>
        /// <param name="suppressError">Suppress error for termination</param>
        protected virtual void DoTerminateInstance(string reason, bool suppressError)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoResumeInstance
        /// </summary>
        protected virtual void DoResumeInstance(string label)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoSuspendInstance
        /// </summary>
        /// <param name="notStarted"></param>
        protected virtual void DoSuspendInstance(bool notStarted)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoExecuteInstance
        /// </summary>
        protected virtual void DoExecuteInstance()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoResumeBookmark
        /// </summary>
        /// <param name="bookmark"></param>
        /// <param name="state"></param>
        protected virtual void DoResumeBookmark(Bookmark bookmark, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads the xaml to create an executable activity.
        /// </summary>
        protected virtual void DoCreateInstance()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove
        /// </summary>
        protected virtual void DoRemoveInstance()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// DoPersistInstance
        /// </summary>
        protected virtual void DoPersistInstance()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// DoGetPersistableIdleAction
        /// </summary>
        /// <param name="bookmarks"></param>
        /// <param name="externalSuspendRequest"></param>
        /// <returns></returns>
        protected virtual PSPersistableIdleAction DoGetPersistableIdleAction(ReadOnlyCollection<BookmarkInfo> bookmarks, bool externalSuspendRequest)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dispose 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Disposed)
                return;

            lock (SyncLock)
            {
                if (Disposed)
                    return;

                Disposed = true;

                this.OnCompleted = null;
                this.OnFaulted = null;
                this.OnStopped = null;
                this.OnAborted = null;
                this.OnSuspended = null;
                this.OnIdle = null;
                this.OnPersistableIdleAction = null;
                this.OnUnloaded = null;
            }
        }
        #endregion Protected Members

        #region Internal Members
        internal PSWorkflowRuntime Runtime { get; set; }

        internal void CreateInstance() { this.DoCreateInstance(); }
        internal void ExecuteInstance() { this.DoExecuteInstance(); }
        internal void SuspendInstance(bool notStarted) { this.DoSuspendInstance(notStarted); }
        internal void RemoveInstance() { this.DoRemoveInstance(); }
        internal void ResumeInstance(string label) { this.DoResumeInstance(label); }
        internal void ResumeBookmark(Bookmark bookmark, object state) { this.DoResumeBookmark(bookmark, state); }
        internal void StopInstance() { this.DoStopInstance(); }
        internal void AbortInstance(string reason) { this.DoAbortInstance(reason); }
        internal void TerminateInstance(string reason, bool suppressError) { this.DoTerminateInstance(reason, suppressError); }
        internal void PersistInstance() { this.DoPersistInstance(); }
        internal PSPersistableIdleAction GetPersistableIdleAction(ReadOnlyCollection<BookmarkInfo> bookmarks, bool externalSuspendRequest) { return this.DoGetPersistableIdleAction(bookmarks, externalSuspendRequest); }

        /// <summary>
        /// Gets the Guid of workflow instance.
        /// </summary>
        internal virtual Guid Id
        {
            get { return this.InstanceId.Guid; }
        }

        /// <summary>
        /// JobStateRetrieved
        /// </summary>
        internal bool JobStateRetrieved { get; set; }

        internal bool ForceDisableStartOrEndPersistence { get; set; }

        /// <summary>
        /// CheckForTerminalAction
        /// </summary>
        internal virtual void CheckForTerminalAction()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load instance for resuming the workflow
        /// </summary>
        internal virtual void DoLoadInstanceForReactivation()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// PerformTaskAtTerminalState
        /// </summary>
        internal virtual void PerformTaskAtTerminalState()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// On completed handler.
        /// </summary>
        internal Action<object> OnCompletedDelegate
        {
            set { this.OnCompleted = value; }
        }

        /// <summary>
        /// On faulted handler.
        /// </summary>
        internal Action<Exception, object> OnFaultedDelegate
        {
            set { this.OnFaulted = value; }
        }

        /// <summary>
        /// On stopped handler.
        /// </summary>
        internal Action<object> OnStoppedDelegate
        {
            set { this.OnStopped = value; }
        }

        /// <summary>
        /// On aborted handler.
        /// </summary>
        internal Action<Exception, object> OnAbortedDelegate
        {
            set { this.OnAborted = value; }
        }

        /// <summary>
        /// On suspended handler.
        /// </summary>
        internal Action<object> OnSuspendedDelegate
        {
            set { this.OnSuspended = value; }
        }

        /// <summary>
        /// On idle handler.
        /// </summary>
        internal Action<ReadOnlyCollection<System.Activities.Hosting.BookmarkInfo>, object> OnIdleDelegate
        {
            set { this.OnIdle = value; }
        }


        /// <summary>
        /// On persistable idle action handler.
        /// </summary>
        internal Func<ReadOnlyCollection<System.Activities.Hosting.BookmarkInfo>, bool, object, PSPersistableIdleAction> OnPersistableIdleActionDelegate
        {
            set { this.OnPersistableIdleAction = value; }
        }

        /// <summary>
        /// On unloaded handler.
        /// </summary>
        internal Action<object> OnUnloadedDelegate
        {
            set { this.OnUnloaded = value; }
        }

        /// <summary>
        /// Validate label
        /// </summary>
        /// <param name="label"></param>
        internal virtual void ValidateIfLabelExists(string label)
        {
            throw new NotImplementedException();
        }

        internal virtual bool SaveStreamsIfNecessary()
        {
            return false;            
        }

        #endregion Internal Members


        #region protected delegate members

        /// <summary>
        /// On completed handler.
        /// </summary>
        protected Action<object> OnCompleted { get; private set; }

        /// <summary>
        /// On faulted handler.
        /// </summary>
        protected Action<Exception, object> OnFaulted { get; private set; }

        /// <summary>
        /// On stopped handler.
        /// </summary>
        protected Action<object> OnStopped { get; private set; }

        /// <summary>
        /// On aborted handler.
        /// </summary>
        protected Action<Exception, object> OnAborted { get; private set; }

        /// <summary>
        /// On suspended handler.
        /// </summary>
        protected Action<object> OnSuspended { get; private set; }

        /// <summary>
        /// On idle handler.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "????")]
        protected Action<ReadOnlyCollection<System.Activities.Hosting.BookmarkInfo>, object> OnIdle { get; private set; }

        /// <summary>
        /// On persistable idle action handler.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "????")]
        protected Func<ReadOnlyCollection<System.Activities.Hosting.BookmarkInfo>, bool, object, PSPersistableIdleAction> OnPersistableIdleAction { get; private set; }

        /// <summary>
        /// On unloaded handler.
        /// </summary>
        protected Action<object> OnUnloaded { get; private set; }

        #endregion protected delegate members


        #region Public Members

        /// <summary>
        /// PSWorkflowJob
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowJob PSWorkflowJob
        {
            get { throw new NotImplementedException();}
            protected internal set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the Guid of workflow instance.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowId InstanceId
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the workflow job creation context.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual Dictionary<string, object> CreationContext
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// State
        /// </summary>
        public virtual JobState State
        {
            get;
            set;
        }

        /// <summary>
        /// InstanceStore
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowInstanceStore InstanceStore
        {
            get{ throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the definition of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowDefinition PSWorkflowDefinition
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the streams of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PowerShellStreams<PSObject, PSObject> Streams
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets/Sets the RemoteActivityState of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowRemoteActivityState RemoteActivityState
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        /// <summary>
        /// Gets the streams of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        public virtual Exception Error
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the timers of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowTimer Timer
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the metadatas of workflow.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public virtual PSWorkflowContext PSWorkflowContext
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the workflow debugger.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        internal virtual PSWorkflowDebugger PSWorkflowDebugger
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the streams to save memory
        /// </summary>
        public virtual void DisposeStreams()
        {
            
        }

        #endregion Public Members

    }
}