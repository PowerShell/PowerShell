//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Activities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Tracing;
using Microsoft.PowerShell.Commands;
using System.Reflection;

namespace Microsoft.PowerShell.Workflow
{
    internal sealed class DefinitionCache
    {

        #region Privates

        internal struct WorkflowDetails
        {
            internal Activity ActivityTree;
            internal string CompiledAssemblyPath;
            internal string CompiledAssemblyName;
            internal bool IsWindowsActivity;
        };

        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        /// <summary>
        /// Cache of workflow details. This is unbounded
        /// </summary>
        private readonly Dictionary<WorkflowJobDefinition, WorkflowDetails> _workflowDetailsCache =
            new Dictionary<WorkflowJobDefinition, WorkflowDetails>(new CompareBasedOnInstanceId());

        /// <summary>
        /// this is the cache of compiled activities which will be bounded
        /// </summary>
        /// <remarks>this is separate since Opalis will not use our cache</remarks>
        private readonly ConcurrentDictionary<WorkflowJobDefinition, Activity> _cachedActivities =
            new ConcurrentDictionary<WorkflowJobDefinition, Activity>(new CompareBasedOnInstanceId());

        private System.Timers.Timer _cleanupTimer;
        private int activitiesCleanupIntervalMSec;

        private static readonly DefinitionCache _instance = new DefinitionCache();
        
        private DefinitionCache()
        {
            activitiesCleanupIntervalMSec = WorkflowJobSourceAdapter.GetInstance().GetPSWorkflowRuntime().Configuration.ActivitiesCacheCleanupIntervalMSec;

            _cleanupTimer = new System.Timers.Timer(activitiesCleanupIntervalMSec);
            _cleanupTimer.Elapsed += HandleCleanupTimerElapsed;
            _cleanupTimer.AutoReset = false;
            _cleanupTimer.Start();
        }

        private void HandleCleanupTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<WorkflowJobDefinition> activitiesToRemove = new List<WorkflowJobDefinition>();
            Activity activity;

            foreach (var workflowJobDefinition in _cachedActivities.Keys)
            {
                if ((GetRuntimeAssemblyPath(workflowJobDefinition) == null) &&
                    (DateTime.Now - workflowJobDefinition.LastUsedDateTime > TimeSpan.FromMilliseconds(activitiesCleanupIntervalMSec)))
                {
                    activitiesToRemove.Add(workflowJobDefinition);
                }
            }

            foreach (var wfJobDefinition in activitiesToRemove)
            {
                _cachedActivities.TryRemove(wfJobDefinition, out activity);
            }

            // Schedule the next cleanup
            _cleanupTimer.Start();
        }

        /// <summary>
        /// Cache size
        /// </summary>
        private const int _cacheSize = 1000;

        /// <summary>
        /// Path to Windows folder
        /// </summary>
        private const string WindowsPath = "%windir%\\system32";

        #endregion Privates

        #region Internal

        internal int CacheSize
        {
            get { return _cacheSize; }
        }

        /// <summary>
        /// Return the singleton instance.
        /// </summary>
        internal static DefinitionCache Instance
        {
            get
            {
                return _instance;
            }
        }

        internal JobDefinition GetDefinition(Guid instanceId)
        {
            return _workflowDetailsCache.Keys.FirstOrDefault(def => def.InstanceId == instanceId);
        }
 
        internal string GetWorkflowXaml(WorkflowJobDefinition definition)
        {
            return definition.Xaml;
        }

        internal string GetRuntimeAssemblyPath(WorkflowJobDefinition definition)
        {
            WorkflowDetails workflowDetail;

            return _workflowDetailsCache.TryGetValue(definition, out workflowDetail) ? workflowDetail.CompiledAssemblyPath : null;
        }

        internal string GetRuntimeAssemblyName(WorkflowJobDefinition definition)
        {
            WorkflowDetails workflowDetail;

            return _workflowDetailsCache.TryGetValue(definition, out workflowDetail) ? workflowDetail.CompiledAssemblyName : null;
        }

        internal bool AllowExternalActivity;

        /// <summary>
        /// Compiles the activity and adds it to the cache before returning it.
        /// </summary>
        /// <param name="definition">WorkflowJobDefinition defined to represent a compiled activity.</param>
        /// <param name="activityTree">Activity Tree used for external activities</param>
        /// <param name="requiredAssemblies"></param>
        /// <param name="windowsWorkflow">indicates if the specified xaml is a Windows workflow</param>
        /// <param name="rootWorkflowName">Optional, once assigned, only root Workflow will be compiled</param>
        /// <returns>Activity compiled from xaml given, or retrieved from cache.
        /// Null if not found.</returns>
        internal Activity CompileActivityAndSaveInCache(WorkflowJobDefinition definition, Activity activityTree, Dictionary<string, string> requiredAssemblies, out bool windowsWorkflow, string rootWorkflowName = null)
        {
            WorkflowDetails workflowDetail = new WorkflowDetails();
            Activity activity = null;

            // initialize windows workflow to false
            windowsWorkflow = false;

            string resultingCompiledAssemblyPath = definition.DependentAssemblyPath;
            string modulePath = definition.ModulePath;
            string[] dependentWorkflows = definition.DependentWorkflows.ToArray();
            Assembly resultingCompiledAssembly = null;
            string resultingCompiledAssemblyName = null;
            string xaml = definition.Xaml;

            if (activityTree != null)
            {
                _tracer.WriteMessage(string.Format(CultureInfo.InvariantCulture,
                                                   "DefinitionCache: Caching activity for definition with instance ID: {0}. The activity Tree is passed.",
                                                   definition.InstanceId));
                workflowDetail.ActivityTree = activityTree;
                activity = activityTree;
            }
            else if (!String.IsNullOrEmpty(xaml))
            {
                // we need to read the xaml from the specified path and compile the same
                _tracer.WriteMessage(string.Format(CultureInfo.InvariantCulture,
                                                   "DefinitionCache: Caching activity for definition with instance ID: {0}. Xaml is passed",
                                                   definition.InstanceId));

                workflowDetail.ActivityTree = null;
                // check if specified workflow is a windows workflow
                if (!string.IsNullOrEmpty(modulePath))
                {
                    string resolvedPath = Environment.ExpandEnvironmentVariables(modulePath);
                    string resolvedWindowsPath = Environment.ExpandEnvironmentVariables(WindowsPath);
                    if (resolvedPath.IndexOf(resolvedWindowsPath, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        windowsWorkflow = true;
                    }
                }

                if (definition.DependentAssemblyPath == null && dependentWorkflows.Length == 0)
                {
                    activity = ImportWorkflowCommand.ConvertXamlToActivity(xaml);
                }
                else
                {
                    if (rootWorkflowName == null || (definition.Command == rootWorkflowName))
                    {
                        activity = ImportWorkflowCommand.ConvertXamlToActivity(xaml, dependentWorkflows,
                                                                               requiredAssemblies,
                                                                               ref resultingCompiledAssemblyPath,
                                                                               ref resultingCompiledAssembly,
                                                                               ref resultingCompiledAssemblyName);
                    }
                    else
                    {
                        activity = ImportWorkflowCommand.ConvertXamlToActivity(xaml);
                    }
                }
            }

            if (activity != null)
            {
                workflowDetail.IsWindowsActivity = windowsWorkflow;
                workflowDetail.CompiledAssemblyPath = resultingCompiledAssemblyPath;
                workflowDetail.CompiledAssemblyName = resultingCompiledAssemblyName;

                lock (_syncObject)
                {                   
                    WorkflowJobDefinition definitionToRemove =
                        _workflowDetailsCache.Keys.FirstOrDefault(item => item.InstanceId == definition.InstanceId);
                    if (definitionToRemove != null)
                        _workflowDetailsCache.Remove(definition);

                    _workflowDetailsCache.Add(definition, workflowDetail);
                }

                // If cached activity count reaches _cacheSize, 
                // Removing the cached activity at index 0 and adding the new activity to the activity cache, 
                // Old logic was to clear 1000 cached activities and recompiling them again when needed.
                //
                if (_cachedActivities.Count == _cacheSize)
                {
                    Activity removedActivity;
                    _cachedActivities.TryRemove(_cachedActivities.Keys.ElementAt<WorkflowJobDefinition>(0), out removedActivity);
                }
                
                _cachedActivities.TryAdd(definition, activity);

                return activity;                
            }

            // we should never hit this point under normal course of operations
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="windowsWorkflow"></param>
        /// <returns></returns>
        internal Activity GetActivityFromCache(WorkflowJobDefinition definition, out bool windowsWorkflow)
        {
            Activity activity = null;
            WorkflowDetails workflowDetail;

            // initialize windowsWorkflow to false
            windowsWorkflow = false;
            if (_workflowDetailsCache.TryGetValue(definition, out workflowDetail))
            {
                // if there is a workflow activity we would have already computed whether it is a
                // Windows workflow, set the same
                windowsWorkflow = workflowDetail.IsWindowsActivity;

                if (workflowDetail.ActivityTree != null)
                {
                    if (!AllowExternalActivity)
                    {
                        Debug.Assert(false, "Product code should not contain passing an activity to definition cache");
                    }

                    activity = workflowDetail.ActivityTree;
                }

                // if a cached value is available return the same
                _cachedActivities.TryGetValue(definition, out activity);
                if (activity == null)
                {
                    // if activity is not available in cache recompile using info in
                    // definition cache               
                    activity = CompileActivityAndSaveInCache(definition, null, null, out windowsWorkflow);
                }

                definition.LastUsedDateTime = DateTime.Now;
            }

            return activity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xaml"></param>
        /// <param name="workflowJobDefinition"></param>
        /// <returns></returns>
        internal Activity GetActivityFromCache(string xaml, out WorkflowJobDefinition workflowJobDefinition)
        {
            workflowJobDefinition = null;

            foreach (var definition in
                _workflowDetailsCache.Keys.Where(definition => string.Equals(definition.Xaml, xaml, StringComparison.OrdinalIgnoreCase)))
            {
                bool windowsWorkflow;
                workflowJobDefinition = definition;
                return GetActivityFromCache(definition, out windowsWorkflow);
            }

            return null;
        }

        private readonly object _syncObject = new object();

        #endregion Internal

        #region Test Helpers

        internal Dictionary<WorkflowJobDefinition, WorkflowDetails> WorkflowDetailsCache
        {
            get { return _workflowDetailsCache; }
        }

        internal ConcurrentDictionary<WorkflowJobDefinition, Activity> ActivityCache
        {
            get { return _cachedActivities; }
        }

        internal void ClearAll()
        {
            _workflowDetailsCache.Clear();
            _cachedActivities.Clear();
        }

        // only called from test
        internal Activity GetActivity(Guid instanceId)
        {
            WorkflowJobDefinition workflowJobDefinition = new WorkflowJobDefinition(typeof(WorkflowJobSourceAdapter),
                                                                                    string.Empty, string.Empty,
                                                                                    string.Empty,
                                                                                    WorkflowJobDefinition.EmptyEnumerable,
                                                                                    string.Empty, null, string.Empty) { InstanceId = instanceId };

            bool windowsWorkflow;
            Activity activity = GetActivityFromCache(workflowJobDefinition, out windowsWorkflow) ??
                                CompileActivityAndSaveInCache(workflowJobDefinition, null, null, out windowsWorkflow);

            return activity;
        }

        //only called from test
        internal Activity GetActivity(JobDefinition definition, string xaml)
        {
            bool windowsWorkflow;
            WorkflowJobDefinition workflowJobDefinition = new WorkflowJobDefinition(definition, string.Empty,
                                                                                    WorkflowJobDefinition.EmptyEnumerable, 
                                                                                    string.Empty,
                                                                                    xaml);
            Activity activity = GetActivityFromCache(workflowJobDefinition, out windowsWorkflow) ??
                                            CompileActivityAndSaveInCache(workflowJobDefinition, null, null, out windowsWorkflow);

            return activity;
        }

        //only called from test
        internal Activity GetActivity(JobDefinition definition, string xaml, string[] dependentWorkflows)
        {
            bool windowsWorkflow;
            WorkflowJobDefinition workflowJobDefinition = new WorkflowJobDefinition(definition, string.Empty,
                                                                                    dependentWorkflows ?? WorkflowJobDefinition.EmptyEnumerable, 
                                                                                    string.Empty,
                                                                                    xaml);

            Activity activity = GetActivityFromCache(workflowJobDefinition, out windowsWorkflow) ??
                                CompileActivityAndSaveInCache(workflowJobDefinition, null, null, out windowsWorkflow);

            return activity;
        }

        internal Activity GetActivity(JobDefinition definition, out bool windowsWorkflow)
        {
            WorkflowJobDefinition workflowJobDefinition = new WorkflowJobDefinition(definition);

            Activity activity = GetActivityFromCache(workflowJobDefinition, out windowsWorkflow) ??
                                CompileActivityAndSaveInCache(workflowJobDefinition, null, null, out windowsWorkflow);

            return activity;            
        }

        /// <summary>
        /// Remove a cached definition. Needed when functions are removed to release resources.
        /// </summary>
        /// <param name="definition">Xaml definition to remove.</param>
        /// <returns>True if succeeded.</returns>
        internal bool RemoveCachedActivity(JobDefinition definition)
        {
            Debug.Assert(definition != null);

            WorkflowJobDefinition workflowJobDefinition = new WorkflowJobDefinition(definition);
            Activity activity;
            _cachedActivities.TryRemove(workflowJobDefinition, out activity);
            lock (_syncObject)
            {
                if (_workflowDetailsCache.ContainsKey(workflowJobDefinition))
                {
                    return _workflowDetailsCache.Remove(workflowJobDefinition);
                }
            }
            return false;
        }

        internal bool RemoveCachedActivity(Guid instanceId)
        {
            var definition = new JobDefinition(null, null, null);
            definition.InstanceId = instanceId;
            return RemoveCachedActivity(definition);
        }

        #endregion Test Helpers
    }

    internal class CompareBasedOnInstanceId : IEqualityComparer<WorkflowJobDefinition>
    {
        #region IEqualityComparer<JobDefinition> Members

        public bool Equals(WorkflowJobDefinition x, WorkflowJobDefinition y)
        {
            return x.InstanceId == y.InstanceId;
        }

        public int GetHashCode(WorkflowJobDefinition obj)
        {
            return obj.InstanceId.GetHashCode();
        }

        #endregion
    }

    internal class CompareBasedOnCommand : IEqualityComparer<WorkflowJobDefinition>
    {
        #region IEqualityComparer<JobDefinition> Members

        public bool Equals(WorkflowJobDefinition x, WorkflowJobDefinition y)
        {
            bool returnValue = false;

            if (String.Equals(y.ModulePath, x.ModulePath, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(y.Command, x.Command, StringComparison.OrdinalIgnoreCase))
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        public int GetHashCode(WorkflowJobDefinition obj)
        {
            int hashCode = 0;
            string moduleQualifiedCommand = string.Empty;

            if (!string.IsNullOrEmpty(obj.ModulePath))
                moduleQualifiedCommand += obj.ModulePath;

            if (!string.IsNullOrEmpty(obj.Command))
                moduleQualifiedCommand += obj.Command;

            hashCode = moduleQualifiedCommand.GetHashCode();

            return hashCode;
        }

        private static readonly CompareBasedOnCommand Comparer = new CompareBasedOnCommand();
        internal static bool Compare(WorkflowJobDefinition x, WorkflowJobDefinition y)
        {
            return Comparer.Equals(x, y);
        }

        #endregion
    }

    internal class WorkflowJobDefinition : JobDefinition
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobSourceAdapterType"></param>
        /// <param name="command"></param>
        /// <param name="name"></param>
        public WorkflowJobDefinition(Type jobSourceAdapterType, string command, string name) : base(jobSourceAdapterType, command, name)
        {
            IsScriptWorkflow = false;
        }

        internal WorkflowJobDefinition(Type jobSourceAdapterType, string command, string name, string modulePath, 
                    IEnumerable<string> dependentWorkflows, string dependentAssemblyPath, Dictionary<string, string> requiredAssemblies, string xaml)
                        : this(jobSourceAdapterType, command, name)
        {
            _modulePath = modulePath;
            _dependentAssemblyPath = dependentAssemblyPath;
            _dependentWorkflows.AddRange(dependentWorkflows);
            _requiredAssemblies = requiredAssemblies;
            _xaml = xaml;
            _lastUsedDateTime = DateTime.Now;
        }

        internal WorkflowJobDefinition(JobDefinition jobDefinition, string modulePath, 
                    IEnumerable<string> dependentWorkflows, string dependentAssemblyPath, string xaml)
            : this(jobDefinition.JobSourceAdapterType, jobDefinition.Command, jobDefinition.Name, modulePath, dependentWorkflows, dependentAssemblyPath, null, xaml)
        {
            InstanceId = jobDefinition.InstanceId;
        }

        internal WorkflowJobDefinition(JobDefinition jobDefinition)
            :this(jobDefinition, string.Empty, EmptyEnumerable, string.Empty, string.Empty)
        {
            
        }

        private readonly string _modulePath;
        internal string ModulePath 
        {
            get { return _modulePath; }
        }

        private readonly List<string> _dependentWorkflows = new List<string>();
        internal List<string> DependentWorkflows
        {
            get { return _dependentWorkflows; }
        }

        private readonly string _dependentAssemblyPath;
        internal string DependentAssemblyPath
        {
            get { return _dependentAssemblyPath; }
        }

        private readonly Dictionary<string, string> _requiredAssemblies;
        internal Dictionary<string, string> RequiredAssemblies
        {
            get { return _requiredAssemblies; }
        }

        private readonly string _xaml;
        internal string Xaml
        {
            get { return _xaml; }
        }

        internal bool IsScriptWorkflow { get; set; }

        internal static IEnumerable<string> EmptyEnumerable = new Collection<string>();

        private DateTime _lastUsedDateTime;
        internal DateTime LastUsedDateTime
        {
            get { return _lastUsedDateTime; }
            set { _lastUsedDateTime = value; }
        }

        /// <summary>
        /// Returns the same object is the specified job definition is a
        /// WorkflowJobDefinition. If not creates a new one and assigns
        /// the same
        /// </summary>
        /// <param name="definition">job definition to check</param>
        /// <returns>WorkflowJobDefinition equivalent</returns>
        internal static WorkflowJobDefinition AsWorkflowJobDefinition(JobDefinition definition)
        {
            return definition as WorkflowJobDefinition ??
                                                          (DefinitionCache.Instance.GetDefinition(definition.InstanceId) as WorkflowJobDefinition ??
                                                                                                  new WorkflowJobDefinition(definition));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Workflow script file name or null if no associated file.
        /// </summary>
        internal string WorkflowScriptFile
        {
            get;
            set;
        }

        /// <summary>
        /// Full workflow script source.
        /// </summary>
        internal string WorkflowFullScript
        {
            get;
            set;
        }
    }
}
