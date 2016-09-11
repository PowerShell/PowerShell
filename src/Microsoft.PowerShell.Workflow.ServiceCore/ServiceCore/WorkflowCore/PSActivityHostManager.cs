/*
 * Copyright (c) 2010 Microsoft Corporation. All rights reserved
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Activities;
using Microsoft.PowerShell.Workflow;
using System.Collections.Concurrent;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Class the describes an activity host arguments
    /// </summary>
    internal sealed class PSResumableActivityContext
    {
        /// <summary>
        /// PSActivityHostArguments
        /// </summary>
        /// <param name="streams"></param>
        internal PSResumableActivityContext(PowerShellStreams<PSObject, PSObject> streams)
        {
            Streams = streams;
            Error = null;
            Failed = false;
            SupportDisconnectedStreams = true;
        }

        /// <summary>
        /// Gets and sets Streams
        /// </summary>
        internal PowerShellStreams<PSObject, PSObject> Streams { get; set; }

        /// <summary>
        /// Gets Errors
        /// </summary>
        internal Exception Error { get; set; }

        /// <summary>
        /// Get failed
        /// </summary>
        internal bool Failed { get; set; }

        internal bool SupportDisconnectedStreams { get; set; }
    }

    /// <summary>
    /// Class the describes an activity host policy
    /// </summary>
    public sealed class PSActivityEnvironment
    {
        private readonly Collection<string> _modules = new Collection<string>();
        private readonly Dictionary<string, object> _variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Collection of modules that an activity is
        /// dependent upon
        /// </summary>
        public Collection<string> Modules
        {
            get { return _modules; }
        }

        /// <summary>
        /// Collection of variables that an activity is
        /// dependent upon
        /// </summary>
        public Dictionary<string, object> Variables
        {
            get { return _variables; }
        }        
    }

    #region PSActivityHostController

    /// <summary>
    /// Activity host manager interface. This interface can be
    /// used to implement various activity hosts
    /// </summary>
    public abstract class PSActivityHostController
    {
        private PSWorkflowRuntime _runtime;
        private readonly ConcurrentDictionary<string, bool> _inProcActivityLookup = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Runtime should be provided for accessing the runtime activity mode
        /// </summary>
        protected PSActivityHostController(PSWorkflowRuntime runtime)
        {
            _runtime = runtime;
        }

        /// <summary>
        /// Identifies whether the specified activity should run in the activity
        /// host or in-proc
        /// </summary>
        /// <param name="activity">activity that needs to be verified</param>
        /// <returns>true, if the activity should run in the activity
        /// host
        /// false otherwise</returns>
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public virtual bool RunInActivityController(Activity activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException("activity");
            }

            String name = activity.GetType().Name;

            if (_inProcActivityLookup.ContainsKey(name)) return _inProcActivityLookup[name];


            ActivityRunMode runMode = _runtime.Configuration.GetActivityRunMode(activity);
            bool runInProc = runMode == ActivityRunMode.InProcess;

            return _inProcActivityLookup.GetOrAdd(name, runInProc);
        }
    }

    /// <summary>
    /// This class will be used for disconnected execution where the 
    /// Job Id and bookmark will be used resume the execution of workflow
    /// after the completion of activity controller work.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public abstract class PSResumableActivityHostController : PSActivityHostController
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="runtime"></param>
        protected PSResumableActivityHostController(PSWorkflowRuntime runtime)
            : base(runtime)
        {

        }

        /// <summary>
        /// StartResumablePSCommand
        /// </summary>
        /// <param name="jobInstanceId"></param>
        /// <param name="bookmark"></param>
        /// <param name="command"></param>
        /// <param name="streams"></param>
        /// <param name="environment"></param>
        /// <param name="activityInstance"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public virtual void StartResumablePSCommand(Guid jobInstanceId,
                                            Bookmark bookmark,
                                            System.Management.Automation.PowerShell command,
                                            PowerShellStreams<PSObject,PSObject> streams,
                                            PSActivityEnvironment environment,
                                            PSActivity activityInstance)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// StopResumablePSCommand
        /// </summary>
        /// <param name="jobInstanceId"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public virtual void StopAllResumablePSCommands(Guid jobInstanceId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This property identifies if the Activity controller is running in disconnected mode 
        /// or not. If it is running in disconnected mode then all the output and data streams will be
        /// proxied as new objects.
        /// </summary>
        public virtual bool SupportDisconnectedPSStreams
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
}
