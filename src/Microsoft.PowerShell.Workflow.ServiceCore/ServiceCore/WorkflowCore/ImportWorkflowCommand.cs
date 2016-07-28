//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Remoting;
using System.Management.Automation.Tracing;
using System.Reflection;
using System.Text;
using System.Xaml;
using Microsoft.PowerShell.Workflow;
using System.Text.RegularExpressions;
using System.Management.Automation.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet to load WF Workflows, expressed as XAML and wrap them
    /// in functions.
    /// </summary>
    [Cmdlet("Import", "PSWorkflow", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=210606")]
    public class ImportWorkflowCommand : PSCmdlet
    {
        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static Tracer _structuredTracer = new System.Management.Automation.Tracing.Tracer();

        /// <summary>
        /// Paths to the XAML files to load. Wild cards are supported.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Alias("PSPath")]
        public String[] Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
            }
        }
        private string[] _path;

        /// <summary>
        /// Paths to the dependent XAML files to load. Wild cards are supported.
        /// </summary>
        [Parameter(Position = 1)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Alias("PSDependentWorkflow")]
        public String[] DependentWorkflow
        {
            get
            {
                return _dependentWorkflow;
            }
            set
            {
                _dependentWorkflow = value;
            }
        }
        private string[] _dependentWorkflow;


        /// <summary>
        /// Paths to the dependent XAML files to load. Wild cards are supported.
        /// </summary>
        [Parameter(Position = 2)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Alias("PSDependentAssemblies")]
        public String[] DependentAssemblies
        {
            get
            {
                return _dependentAssemblies;
            }
            set
            {
                _dependentAssemblies = value;
            }
        }
        private string[] _dependentAssemblies;

        /// <summary>
        /// Flags -force operations
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        private bool _force;

        /// <summary>
        /// Process all of the specified  XAML files to generate corresponding functions
        /// </summary>
        protected override void ProcessRecord()
        {
            // In ConstrainedLanguage, XAML workflows are not supported (even from a trusted FullLanguage state),
            // unless they are signed in-box OS binaries.
            bool checkSignatures = false;
            if ((SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) ||
                (this.SessionState.LanguageMode == PSLanguageMode.ConstrainedLanguage))
            {
                // However, this static internal property can be changed by tests that can already run a script
                // in full-language mode.
                PropertyInfo xamlProperty = typeof(SystemPolicy).GetProperty("XamlWorkflowSupported", BindingFlags.NonPublic | BindingFlags.Static);
                checkSignatures = !((bool)xamlProperty.GetValue(null, null));
            }

            string dependentWorkflowAssemblyPath = string.Empty;
            List<string> dependentWorkflowContent = new List<string>();

            if (_dependentWorkflow != null)
            {
                foreach (string dependentPath in _dependentWorkflow)
                {
                    Collection<string> dependentFilePaths;
                    try
                    {
                        // Try to resolve the pathname, including wildcard resolution
                        // if an error occurs, generate a non-terminating exception and continue to the next path.
                        ProviderInfo provider = null;
                        dependentFilePaths = this.SessionState.Path.GetResolvedProviderPathFromPSPath(dependentPath, /* Ignored */ out provider);
                    }
                    catch (ItemNotFoundException notFound)
                    {

                        ErrorRecord er = new ErrorRecord(notFound, "Workflow_XamlFileNotFound",
                            ErrorCategory.OpenError, dependentPath);
                        WriteError(er);
                        Tracer.TraceErrorRecord(er);
                        continue;
                    }


                    if (dependentFilePaths != null && dependentFilePaths.Count > 0)
                    {
                        if (dependentFilePaths.Count == 1 && string.Compare(System.IO.Path.GetExtension(dependentFilePaths[0]), ".dll", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            dependentWorkflowAssemblyPath = dependentFilePaths[0];
                        }
                        else
                        {
                            // The argument may have resolved to more than one path. Loop over the resolved paths
                            // loading each file in turn. If the resolved path does not have a '.xaml' exception,
                            // generate a non-terminating exception and continue.
                            foreach (string resolvedPath in dependentFilePaths)
                            {
                                string fileText;
                                //string name;
                                string extension = System.IO.Path.GetExtension(resolvedPath);
                                if (string.Compare(extension, ".xaml", StringComparison.OrdinalIgnoreCase) != 0)
                                {
                                    InvalidOperationException opException = new InvalidOperationException(
                                        string.Format(CultureInfo.CurrentUICulture, Resources.InvalidWorkflowExtension, extension));
                                    ErrorRecord er = new ErrorRecord(opException, "Workflows_InvalidWorkflowFileExtension",
                                        ErrorCategory.InvalidOperation, resolvedPath);
                                    WriteError(er);
                                    Tracer.TraceErrorRecord(er);
                                    continue;
                                }

                                CheckFileSignatureAsNeeded(checkSignatures, resolvedPath);

                                try
                                {
                                    // Finally load the file. If there is an access violation, write a
                                    // not terminating error and continue.
                                    fileText = System.IO.File.ReadAllText(resolvedPath);
                                    dependentWorkflowContent.Add(fileText);
                                }
                                catch (System.AccessViolationException noPerms)
                                {
                                    ErrorRecord er = new ErrorRecord(noPerms, "Workflow_XAMLfileNotAccessible",
                                        ErrorCategory.PermissionDenied, dependentPath);
                                    WriteError(er);
                                    Tracer.TraceErrorRecord(er);
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        // If the file spec didn't resolve to a path, write an error
                        string message = string.Format(CultureInfo.CurrentUICulture, Resources.NoMatchingWorkflowWasFound, dependentPath);
                        FileNotFoundException fnf = new FileNotFoundException(message);
                        ErrorRecord er = new ErrorRecord(fnf, "Workflow_NoMatchingWorkflowXamlFileFound",
                            ErrorCategory.ResourceUnavailable, dependentPath);
                        WriteError(er);
                        Tracer.TraceErrorRecord(er);
                    }
                }
            }

            Collection<string> filePaths;

            if (_path != null)
            {
                foreach (string path in _path)
                {
                    try
                    {
                        // Try to resolve the pathname, including wildcard resolution
                        // if an error occurs, generate a non-terminating exception and continue to the next path.
                        ProviderInfo provider = null;
                        filePaths = this.SessionState.Path.GetResolvedProviderPathFromPSPath(path, /* Ignored */ out provider);
                    }
                    catch (ItemNotFoundException notFound)
                    {

                        ErrorRecord er = new ErrorRecord(notFound, "Workflow_XamlFileNotFound",
                            ErrorCategory.OpenError, path);
                        WriteError(er);
                        Tracer.TraceErrorRecord(er);
                        continue;
                    }

                    if (filePaths != null && filePaths.Count > 0)
                    {
                        // The argument may have resolved to more than one path. Loop over the resolved paths
                        // loading each file in turn. If the resolved path does not have a '.xaml' exception,
                        // generate a non-terminating exception and continue.
                        foreach (string resolvedPath in filePaths)
                        {
                            string fileText = string.Empty;
                            string name;
                            string extension = System.IO.Path.GetExtension(resolvedPath);
                            if (string.Compare(extension, ".xaml", StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                InvalidOperationException opException = new InvalidOperationException(
                                    string.Format(CultureInfo.CurrentUICulture, Resources.InvalidWorkflowExtension, extension));
                                ErrorRecord er = new ErrorRecord(opException, "Workflows_InvalidWorkflowFileExtension",
                                    ErrorCategory.InvalidOperation, resolvedPath);
                                WriteError(er);
                                Tracer.TraceErrorRecord(er);
                                continue;
                            }

                            CheckFileSignatureAsNeeded(checkSignatures, resolvedPath);

                            FunctionDetails detailsToUseForUpdate = null;
                            try
                            {
                                // Write a message indicating the file name it is trying to load. 
                                string message = string.Format(CultureInfo.CurrentUICulture, Resources.ImportingWorkflowFrom, resolvedPath);
                                WriteVerbose(message);

                                // If -Verbose has been passed to this cmdlet, then the Set operation in GenerateFunctionFromXaml()
                                // will also be logged the verbose stream. This is desirable as it displays the generated function...

                                // Try to read the file. If there is an access violation, write a
                                // non-terminating error and continue with the next workflow file.

                                // check if the function cache already has the entry to this file
                                // if the xaml was already parsed use the same function definition
                                // again
                                name = System.IO.Path.GetFileNameWithoutExtension(resolvedPath);
                                _structuredTracer.ImportingWorkflowFromXaml(Guid.Empty, name);
                                FunctionCache.TryGetValue(resolvedPath, out detailsToUseForUpdate);

                                if (detailsToUseForUpdate != null && !_force)
                                {
                                    UpdateFunctionFromXaml(detailsToUseForUpdate);
                                    _structuredTracer.ImportedWorkflowFromXaml(Guid.Empty, name);
                                    continue;
                                }

                                fileText = System.IO.File.ReadAllText(resolvedPath);                                
                            }
                            catch (System.AccessViolationException noPerms)
                            {
                                ErrorRecord er = new ErrorRecord(noPerms, "Workflow_XAMLfileNotAccessible",
                                    ErrorCategory.PermissionDenied, path);
                                WriteError(er);
                                Tracer.TraceErrorRecord(er);
                                continue;
                            }

                            Dictionary<string, string> requiredAssemblies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            if (DependentAssemblies != null && DependentAssemblies.Length > 0)
                            {
                                foreach (string filePath in DependentAssemblies)
                                {
                                    requiredAssemblies.Add(System.IO.Path.GetFileNameWithoutExtension(filePath), filePath);
                                }
                            }

                            FunctionDetails details = GenerateFunctionFromXaml(name, fileText,
                                                                                requiredAssemblies,
                                                                                dependentWorkflowContent.ToArray(),
                                                                                dependentWorkflowAssemblyPath,
                                                                                resolvedPath,
                                                                                this.SessionState.LanguageMode);

                            // check if the function cache already has the entry to this file
                            // If detailsToUseForUpdate is not null it is a forced import module
                            if (detailsToUseForUpdate != null)
                            {
                                // Update the cached function details
                                //
                                FunctionCache.TryUpdate(resolvedPath, details, detailsToUseForUpdate);
                                detailsToUseForUpdate = details;
                            }
                            else
                            {
                                if (FunctionCache.Count == FunctionCacheSize)
                                    FunctionCache.Clear();

                                detailsToUseForUpdate = FunctionCache.GetOrAdd(resolvedPath, details);
                            }

                            UpdateFunctionFromXaml(detailsToUseForUpdate);

                            _structuredTracer.ImportedWorkflowFromXaml(Guid.Empty, name);
                        }
                    }
                    else
                    {
                        // If the file spec didn't resolve to a path, write an error
                        string message = string.Format(CultureInfo.CurrentUICulture, Resources.NoMatchingWorkflowWasFound, path);
                        FileNotFoundException fnf = new FileNotFoundException(message);
                        ErrorRecord er = new ErrorRecord(fnf, "Workflow_NoMatchingWorkflowXamlFileFound",
                            ErrorCategory.ResourceUnavailable, path);
                        WriteError(er);
                        Tracer.TraceErrorRecord(er);
                    }
                }
            } //if


        } //ProcessRecord  

        private static void CheckFileSignatureAsNeeded(bool checkSignatures, string filePath)
        {
            if (checkSignatures && !System.Management.Automation.Internal.SecuritySupport.IsProductBinary(filePath))
            {
                throw new NotSupportedException(Resources.XamlWorkflowsNotSupported);
            }
        }

        private static readonly ConcurrentDictionary<string, FunctionDetails> FunctionCache =
            new ConcurrentDictionary<string, FunctionDetails>(StringComparer.OrdinalIgnoreCase);

        private const int FunctionCacheSize = 1000;

        /// <summary>
        /// Load a workflow XAML file from the specified path and generate a PowerShell
        /// function from the file. The name of the generated function will be the basename
        /// of the XAML file.
        /// </summary>
        /// <param name="name">The name of workflow.</param>
        /// <param name="xaml">The xaml of workflow.</param>
        /// <param name="requiredAssemblies"></param>
        /// <param name="dependentWorkflows">Any workflows required by this workflow.</param>
        /// <param name="dependentAssemblyPath">Path to the dependent assembly.</param>
        /// <param name="resolvedPath">Resolved Path of the xaml</param>
        /// <param name="sourceLanguageMode">Language mode of source in which workflow should run</param>
        private static FunctionDetails GenerateFunctionFromXaml(
            string name, 
            string xaml, 
            Dictionary<string, string> requiredAssemblies, 
            string[] dependentWorkflows, 
            string dependentAssemblyPath, 
            string resolvedPath,
            PSLanguageMode sourceLanguageMode)
        {
            if (name == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException("name");
                Tracer.TraceException(argNullException);
                throw argNullException;
            }

            string modulePath = System.IO.Path.GetDirectoryName(resolvedPath);
            string functionDefinition = CreateFunctionFromXaml(
                name, 
                xaml, 
                requiredAssemblies, 
                dependentWorkflows, 
                dependentAssemblyPath, 
                null, 
                modulePath, 
                false, 
                "[CmdletBinding()]",
                null,   /* scriptContent */
                null,   /* fullScript */
                null,   /* rootWorkflowName */
                sourceLanguageMode,
                null);

            FunctionDetails details = new FunctionDetails
                                          {Name = name, FunctionDefinition = functionDefinition, Xaml = xaml};

            return details;
        }

        /// <summary>
        /// Generate a function in the current session using the specified
        /// function details
        /// </summary>
        /// <param name="details">details of the function</param>
        private void UpdateFunctionFromXaml(FunctionDetails details)
        {
            // Bind the command into the caller's command table. Note that if
            // ~/ has been passed to this cmdlet, then the Set operation will also be logged
            // the verbose stream. This is desirable as it shows the generated function...
            string functionName = "function:\\script:" + details.Name;

            // This script block is defined as FullLanguage mode, since it contains
            // no text that can be injected by the user.
            ScriptBlock xamlFunctionDefinition = null;
            PSLanguageMode oldLanguageMode = this.SessionState.LanguageMode;
            try
            {
                this.SessionState.LanguageMode = PSLanguageMode.FullLanguage;
                xamlFunctionDefinition = ScriptBlock.Create(details.FunctionDefinition);
            }
            finally
            {
                this.SessionState.LanguageMode = oldLanguageMode;
            }

            WorkflowInfo workflow = new WorkflowInfo(details.Name, "", xamlFunctionDefinition, details.Xaml, null);

            SessionState.InvokeProvider.Item.Set(functionName, workflow);
        }

        /// <summary>
        /// Executes an instance of the workflow object graph identified by the passed
        /// GUID, binding parameters from the Parameters hastable.
        /// </summary>
        /// <param name="command">The powershell command.</param>
        /// <param name="workflowGuid">The GUID used to identify the workflow to run.</param>
        /// <param name="parameters">The parameters to pass to the workflow instance.</param>
        /// <param name="jobName">The friendly name for the job</param>
        /// <param name="parameterCollectionProcessed">True if there was a PSParameters collection</param>
        /// <param name="startAsync"></param>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "workFlowDefinition")]
        public static ContainerParentJob StartWorkflowApplication(PSCmdlet command, string jobName, string workflowGuid, bool startAsync,
            bool parameterCollectionProcessed, Hashtable[] parameters)
        {
            return StartWorkflowApplication(command, jobName, workflowGuid, startAsync, parameterCollectionProcessed, parameters, false);
        }

        /// <summary>
        /// Executes an instance of the workflow object graph identified by the passed
        /// GUID, binding parameters from the Parameters hastable.
        /// </summary>
        /// <param name="command">The powershell command.</param>
        /// <param name="workflowGuid">The GUID used to identify the workflow to run.</param>
        /// <param name="parameters">The parameters to pass to the workflow instance.</param>
        /// <param name="jobName">The friendly name for the job</param>
        /// <param name="parameterCollectionProcessed">True if there was a PSParameters collection</param>
        /// <param name="startAsync"></param>
        /// <param name="debuggerActive">True if debugger is in active state.</param>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "workFlowDefinition")]
        public static ContainerParentJob StartWorkflowApplication(PSCmdlet command, string jobName, string workflowGuid, bool startAsync,
            bool parameterCollectionProcessed, Hashtable[] parameters, bool debuggerActive)
        {
            return StartWorkflowApplication(command, jobName, workflowGuid, startAsync, parameterCollectionProcessed, parameters, false, null);
        }

        /// <summary>
        /// Executes an instance of the workflow object graph identified by the passed
        /// GUID, binding parameters from the Parameters hastable.
        /// </summary>
        /// <param name="command">The powershell command.</param>
        /// <param name="workflowGuid">The GUID used to identify the workflow to run.</param>
        /// <param name="parameters">The parameters to pass to the workflow instance.</param>
        /// <param name="jobName">The friendly name for the job</param>
        /// <param name="parameterCollectionProcessed">True if there was a PSParameters collection</param>
        /// <param name="startAsync"></param>
        /// <param name="debuggerActive">True if debugger is in active state.</param>
        /// <param name="SourceLanguageMode">Language mode of source creating workflow.</param>
        public static ContainerParentJob StartWorkflowApplication(PSCmdlet command, string jobName, string workflowGuid, bool startAsync,
            bool parameterCollectionProcessed, Hashtable[] parameters, bool debuggerActive, string SourceLanguageMode)
        {
            Guid trackingGuid = Guid.NewGuid();
            _structuredTracer.BeginStartWorkflowApplication(trackingGuid);

            if (string.IsNullOrEmpty(workflowGuid))
            {
                var exception = new ArgumentNullException("workflowGuid");
                Tracer.TraceException(exception);
                throw exception;
            }

            if (command == null)
            {
                var exception = new ArgumentNullException("command");
                Tracer.TraceException(exception);
                throw exception;
            }

            if (parameterCollectionProcessed)
            {
                StringBuilder paramString = new StringBuilder();
                StringBuilder computers = new StringBuilder();

                // Add context for this trace record...
                paramString.Append("commandName ='" + command.MyInvocation.MyCommand.Name + "'\n");
                paramString.Append("jobName ='" + jobName + "'\n");
                paramString.Append("workflowGUID = " + workflowGuid + "\n");
                paramString.Append("startAsync " + startAsync.ToString() + "\n");

                // Stringize the parameter table to look like @{ k1 = v1; k2 = v2; ... }
                if (parameters != null)
                {
                    foreach (Hashtable h in parameters)
                    {
                        paramString.Append("@{");
                        bool first = true;
                        foreach (DictionaryEntry e in h)
                        {
                            if (e.Key != null)
                            {
                                if (!first)
                                {
                                    paramString.Append("'; ");
                                }
                                else
                                {
                                    first = false;
                                }
                                paramString.Append(e.Key.ToString());
                                paramString.Append("='");
                                if (e.Value != null)
                                {
                                    if (string.Equals(e.Key.ToString(), Constants.ComputerName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        computers.Append(LanguagePrimitives.ConvertTo<string>(e.Value));
                                    }

                                    paramString.Append(e.Value.ToString());
                                }
                            }
                        }
                        paramString.Append("}\n ");
                    }
                }

                _structuredTracer.ParameterSplattingWasPerformed(paramString.ToString(), computers.ToString());
            }

            JobDefinition workFlowDefinition = DefinitionCache.Instance.GetDefinition(new Guid(workflowGuid));

            if (workFlowDefinition == null)
            {
                var invalidOpException = new InvalidOperationException(
                                                                string.Format(
                                                                    CultureInfo.CurrentUICulture, 
                                                                    Resources.InvalidWorkflowDefinitionState, 
                                                                    DefinitionCache.Instance.CacheSize));
                Tracer.TraceException(invalidOpException);
                throw invalidOpException;
            }

            ContainerParentJob myJob = null;

            /*
             * Iterate through the list of parameters hashtables. Each table may, in turn
             * contain a list of computers to target.
             */
            var parameterDictionaryList = new List<Dictionary<string, object>>();
            if (parameters == null || parameters.Length == 0)
            {
                // There were no parameters so add a dummy parameter collection
                // to ensure that at least one child job is created...
                parameterDictionaryList.Add(new Dictionary<string, object>());
            }
            else if (parameters.Length == 1 && !parameterCollectionProcessed)
            {
                // If the paramter collection is just the $PSBoundParameters
                // i.e. did not come through the PSParameterCollection list
                // then just we can use it as is since it's already been processed through the parameter
                // binder...
                Hashtable h = parameters[0];
                Dictionary<string, object> paramDictionary = new Dictionary<string, object>();
                foreach (var key in parameters[0].Keys)
                {
                    paramDictionary.Add((string)key, h[key]);
                }
                parameterDictionaryList.Add(paramDictionary);
            }
            else
            {
                // Otherwise, iterate through the list of parameter collections using
                // a function to apply the parameter binder transformations on each item in the list.
                // on the hashtable.

                // Convert the parameters into dictionaries.
                foreach (Hashtable h in parameters)
                {
                    if (h != null)
                    {
                        Dictionary<string, object> canonicalEntry = ConvertToParameterDictionary(h);

                        // Add parameter dictionary to the list...
                        parameterDictionaryList.Add(canonicalEntry);
                    }
                }
            }

            JobInvocationInfo specification = new JobInvocationInfo(workFlowDefinition, parameterDictionaryList);
            specification.Name = jobName;

            specification.Command = command.MyInvocation.InvocationName;
            _structuredTracer.BeginCreateNewJob(trackingGuid);
            myJob = command.JobManager.NewJob(specification) as ContainerParentJob;
            _structuredTracer.EndCreateNewJob(trackingGuid);

            // Pass the source language mode to the workflow job so that it can be
            // applied during activity execution.
            PSLanguageMode sourceLanguageModeValue;
            PSLanguageMode? sourceLanguageMode = null;
            if (!string.IsNullOrEmpty(SourceLanguageMode) && Enum.TryParse<PSLanguageMode>(SourceLanguageMode, out sourceLanguageModeValue))
            {
                sourceLanguageMode = sourceLanguageModeValue;
            }

            // Raise engine event of new WF job start for debugger, if 
            // debugger is active (i.e., has breakpoints set).
            if (debuggerActive)
            {
                RaiseWFJobEvent(command, myJob, startAsync);
            }

            if (startAsync)
            {
                foreach(PSWorkflowJob childjob in myJob.ChildJobs)
                {
                    if (!PSSessionConfigurationData.IsServerManager)
                    {
                        childjob.EnableStreamUnloadOnPersistentState();
                    }
                    childjob.SourceLanguageMode = sourceLanguageMode;
                }
                myJob.StartJobAsync();
            }
            else
            {
                // If the job is running synchronously, the PSWorkflowJob(s) need to know
                // so that errors are decorated correctly.
                foreach (PSWorkflowJob childJob in myJob.ChildJobs)
                {
                    childJob.SynchronousExecution = true;
                    childJob.SourceLanguageMode = sourceLanguageMode;
                }
                myJob.StartJob();
            }

            // write an event specifying that job creation is done
            _structuredTracer.EndStartWorkflowApplication(trackingGuid);
            _structuredTracer.TrackingGuidContainerParentJobCorrelation(trackingGuid, myJob.InstanceId);

            return myJob;
        }

        private static void RaiseWFJobEvent(PSCmdlet command, ContainerParentJob job, bool startAsync)
        {
            var eventManager = command.Events;
            Debug.Assert(eventManager != null, "Event Manager cannot be null.");

            foreach (PSWorkflowJob cJob in job.ChildJobs)
            {
                if (cJob == null) continue;

                // Raise event for each child job.
                eventManager.GenerateEvent(
                        sourceIdentifier: PSEngineEvent.WorkflowJobStartEvent,
                        sender: null,
                        args: new object[] { new PSJobStartEventArgs(cJob, cJob.PSWorkflowDebugger, startAsync) },
                        extraData: null,
                        processInCurrentThread: true,
                        waitForCompletionInCurrentThread: true);
            }
        }

        private static Dictionary<string, object> ConvertToParameterDictionary(Hashtable h)
        {
            if (h == null)
                return null;

            Dictionary<string, object> paramDictionary = new Dictionary<string, object>();

            foreach (var key in h.Keys)
            {
                paramDictionary.Add((string)key, h[key]);
            }
            return paramDictionary;
        }

        /// <summary>
        /// Retrieve a localized error message saying that only a single default parameter collection can be specified 
        /// </summary>
        public static string ParameterErrorMessage
        {
            get
            {
                return Resources.OnlyOneDefaultParameterCollectionAllowed;
            }
        }

        /// <summary>
        /// Retrieve a localized error message saying that AsJob, JobName and PSParameterCollection cannot be specified as entries to PSParameterCollection 
        /// </summary>
        public static string InvalidPSParameterCollectionEntryErrorMessage
        {
            get
            {
                return Resources.AsJobandJobNameNotAllowed;
            }
        }

        /// <summary>
        /// Retrieve a localized error message saying that the only AsJob, JobName and InputObject can be used outside of PSParameterCollection.
        /// </summary>
        public static string InvalidPSParameterCollectionAdditionalErrorMessage
        {
            get
            {
                return Resources.ParameterCollectionOnlyUsedWithAsJobAndJobName;
            }
        }

        /// <summary>
        /// Retrieve a localized error message saying that starting the workflow failed...
        /// </summary>
        public static string UnableToStartWorkflowMessageMessage
        {
            get
            {
                return Resources.UnableToStartWorkflow;
            }
        }

        /// <summary>
        /// Convert the Xaml based workflow into object-graph activity.
        /// </summary>
        /// <param name="xaml">Xaml representing workflow.</param>
        /// <returns>Activity representing the workflow.</returns>
        internal static Activity ConvertXamlToActivity(string xaml)
        {
            Tracer.WriteMessage("Trying to convert Xaml into Activity.");

            if (string.IsNullOrEmpty(xaml))
            {
                // Tracer* _M3PErrorImportingWorkflowFromXaml
                ArgumentNullException exception = new ArgumentNullException("xaml", Resources.XamlNotNull);
                Tracer.TraceException(exception);
                throw exception;
            }

            StringReader sReader = new StringReader(xaml);
            Activity workflow;

            try
            {
                workflow = ActivityXamlServices.Load(sReader);
            }
            finally
            {
                sReader.Dispose();
            }
            //_M3PImportedWorkflowFromXaml

            return workflow;
        }

        internal static ConcurrentDictionary<string, WorkflowRuntimeCompilation> compiledAssemblyCache = new ConcurrentDictionary<string, WorkflowRuntimeCompilation>();

        internal static string CompileDependentWorkflowsToAssembly(string[] dependentWorkflows, Dictionary<string, string> requiredAssemblies)
        {
            Debug.Assert(dependentWorkflows.Length > 0, "There should be atleast one dependent workflow.");
            Debug.Assert((PSWorkflowRuntime.Instance.Configuration).GetType() == typeof(PSWorkflowConfigurationProvider), "type mismatch error.");

            PSWorkflowConfigurationProvider config = (PSWorkflowConfigurationProvider)PSWorkflowRuntime.Instance.Configuration;
            
            // Calculating Hashcode
            string hashcode = string.Empty;

            List<int> codes = new List<int>();
            foreach (string dependentWorkflow in dependentWorkflows)
            {
                codes.Add(dependentWorkflow.GetHashCode());
            }
            codes.Sort();
            
            foreach (int code in codes)
            {
                hashcode += code.ToString(CultureInfo.InvariantCulture);
            }

            Debug.Assert(string.IsNullOrEmpty(hashcode) == false, "Hash code should not be null or empty");

            WorkflowRuntimeCompilation compiler = null;

            // check if the hashcode already exist in cache
            if (compiledAssemblyCache.ContainsKey(hashcode))
            {
                compiledAssemblyCache.TryGetValue(hashcode, out compiler);
            }

            if (compiler == null)
            {
                try
                {
                    compiler = new WorkflowRuntimeCompilation();

                    compiler.Compile(new List<string>(dependentWorkflows), requiredAssemblies);
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                    throw;
                }

                // sanity check to block the unbounded increase in the cache size
                if (compiledAssemblyCache.Keys.Count >= config.CompiledAssemblyCacheLimit)
                {
                    compiledAssemblyCache.Clear();
                }

                compiledAssemblyCache.TryAdd(hashcode, compiler);
            }
            
            // Throw an error if there was a compilation problem
            if (!compiler.BuildReturnedCode || !File.Exists(compiler.AssemblyPath))
            {
                string message = string.Format(CultureInfo.CurrentUICulture,
                                               Resources.CompilationErrorWhileBuildingWorkflows,
                                               compiler.BuildLogPath);
                throw new InvalidDataException(message);
            }

            return compiler.AssemblyPath;
        }

        /// <summary>
        /// Convert the Xaml based workflow into object-graph activity with additional xamls assembly provided.
        /// </summary>
        /// <param name="xaml">Xaml representing workflow.</param>
        /// <param name="dependentWorkflows">Any workflows required by this workflow.</param>
        /// <param name="requiredAssemblies"></param>
        /// <param name="compiledAssemblyPath">The path to the compiled assembly for any dependent workflows.</param>
        /// <param name="compiledAssembly">The compiled assembly.</param>
        /// <param name="compiledAssemblyName">The compiled assembly name.</param>
        /// <returns>Activity representing the workflow.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.Reflection.Assembly.LoadFrom")]
        internal static Activity ConvertXamlToActivity(string xaml, string[] dependentWorkflows,
                                                       Dictionary<string, string> requiredAssemblies,
                                                       ref string compiledAssemblyPath, ref Assembly compiledAssembly,
                                                       ref string compiledAssemblyName)
        {
            _structuredTracer.ImportedWorkflowFromXaml(Guid.Empty, string.Empty);
            Tracer.WriteMessage("Trying to convert Xaml into Activity and using additional xamls assembly.");

            if (string.IsNullOrEmpty(xaml))
            {
                ArgumentNullException exception = new ArgumentNullException("xaml", Resources.XamlNotNull);
                Tracer.TraceException(exception);
                _structuredTracer.ErrorImportingWorkflowFromXaml(Guid.Empty, exception.Message);
                throw exception;
            }

            Activity workflow;
            XamlXmlReaderSettings readerSettings = new XamlXmlReaderSettings();

            // If they passed in an assembly path, use it
            if (!String.IsNullOrEmpty(compiledAssemblyPath))
            {
                if (compiledAssembly == null && String.IsNullOrEmpty(compiledAssemblyName))
                {
                    compiledAssembly = Assembly.LoadFrom(compiledAssemblyPath);
                    compiledAssemblyName = compiledAssembly.GetName().Name;
                }

                readerSettings.LocalAssembly = compiledAssembly;
            }
            else if ((dependentWorkflows != null) && (dependentWorkflows.Length > 0))
            {
                // Otherwise, it's a first-time compilation
                string assemblyPath = CompileDependentWorkflowsToAssembly(dependentWorkflows, requiredAssemblies);
                readerSettings.LocalAssembly = Assembly.LoadFrom(assemblyPath);
                compiledAssemblyPath = assemblyPath;
                compiledAssembly = Assembly.LoadFrom(compiledAssemblyPath);
                compiledAssemblyName = compiledAssembly.GetName().Name;

                readerSettings.LocalAssembly = compiledAssembly;
            }

            using (StringReader sReader = new StringReader(xaml))
            {
                XamlXmlReader reader = new XamlXmlReader(sReader, readerSettings);
                workflow = ActivityXamlServices.Load(reader);
            }

            _structuredTracer.ImportedWorkflowFromXaml(Guid.Empty, string.Empty);

            return workflow;
        }

        private class FunctionDetails
        {
            internal string Name { get; set; }
            internal string FunctionDefinition { get; set; }
            internal string Xaml { get; set; }
        }

        #region Moved from AstToWorkflowConverter

        private const string functionNamePattern = "^[a-zA-Z0-9-_]*$";
        /// <summary>
        /// Creates a workflow activity based on the provided Xaml and returns PowerShell script that will
        /// run the workflow.
        /// </summary>
        /// <param name="name">Workflow name</param>
        /// <param name="xaml">Workflow Xaml definition</param>
        /// <param name="requiredAssemblies">Required assemblies</param>
        /// <param name="dependentWorkflows">Dependent workflows</param>
        /// <param name="dependentAssemblyPath">Path for dependent assemblies</param>
        /// <param name="parameterValidation">Workflow parameters</param>
        /// <param name="modulePath">Module path</param>
        /// <param name="scriptWorkflow">True if this is script workflow</param>
        /// <param name="workflowAttributes">the attribute string to use for the workflow, should be '[CmdletBinding()]'</param>
        /// <returns></returns>
        public static string CreateFunctionFromXaml(
            string name,
            string xaml,
            Dictionary<string, string> requiredAssemblies,
            string[] dependentWorkflows,
            string dependentAssemblyPath,
            Dictionary<string, ParameterAst> parameterValidation,
            string modulePath,
            bool scriptWorkflow,
            string workflowAttributes
            )
        {
            return CreateFunctionFromXaml(
                name,
                xaml,
                requiredAssemblies,
                dependentWorkflows,
                dependentAssemblyPath,
                parameterValidation,
                modulePath,
                scriptWorkflow,
                workflowAttributes,
                null);
        }

        /// <summary>
        /// Creates a workflow activity based on the provided Xaml and returns PowerShell script that will
        /// run the workflow.
        /// </summary>
        /// <param name="name">Workflow name</param>
        /// <param name="xaml">Workflow Xaml definition</param>
        /// <param name="requiredAssemblies">Required assemblies</param>
        /// <param name="dependentWorkflows">Dependent workflows</param>
        /// <param name="dependentAssemblyPath">Path for dependent assemblies</param>
        /// <param name="parameterValidation">Workflow parameters</param>
        /// <param name="modulePath">Module path</param>
        /// <param name="scriptWorkflow">True if this is script workflow</param>
        /// <param name="workflowAttributes">the attribute string to use for the workflow, should be '[CmdletBinding()]'</param>
        /// <param name="scriptContent">File path containing script content.</param>
        /// <returns></returns>
        public static string CreateFunctionFromXaml(
            string name,
            string xaml,
            Dictionary<string, string> requiredAssemblies,
            string[] dependentWorkflows,
            string dependentAssemblyPath,
            Dictionary<string, ParameterAst> parameterValidation,
            string modulePath,
            bool scriptWorkflow,
            string workflowAttributes,
            string scriptContent
            )
        {
            return CreateFunctionFromXaml(
                name,
                xaml,
                requiredAssemblies,
                dependentWorkflows,
                dependentAssemblyPath,
                parameterValidation,
                modulePath,
                scriptWorkflow,
                workflowAttributes,
                scriptContent,
                null);
        }

        /// <summary>
        /// Creates a workflow activity based on the provided Xaml and returns PowerShell script that will
        /// run the workflow.
        /// </summary>
        /// <param name="name">Workflow name</param>
        /// <param name="xaml">Workflow Xaml definition</param>
        /// <param name="requiredAssemblies">Required assemblies</param>
        /// <param name="dependentWorkflows">Dependent workflows</param>
        /// <param name="dependentAssemblyPath">Path for dependent assemblies</param>
        /// <param name="parameterValidation">Workflow parameters</param>
        /// <param name="modulePath">Module path</param>
        /// <param name="scriptWorkflow">True if this is script workflow</param>
        /// <param name="workflowAttributes">the attribute string to use for the workflow, should be '[CmdletBinding()]'</param>
        /// <param name="scriptContent">File path containing script content.</param>
        /// <param name="fullScript">Full source script.</param>
        /// <returns></returns>
        public static string CreateFunctionFromXaml(
            string name,
            string xaml,
            Dictionary<string, string> requiredAssemblies,
            string[] dependentWorkflows,
            string dependentAssemblyPath,
            Dictionary<string, ParameterAst> parameterValidation,
            string modulePath,
            bool scriptWorkflow,
            string workflowAttributes,
            string scriptContent,
            string fullScript
            )
        {
            return CreateFunctionFromXaml(name, xaml, requiredAssemblies, dependentWorkflows, dependentAssemblyPath,
                parameterValidation, modulePath, scriptWorkflow, workflowAttributes, scriptContent, fullScript, null);
        }

        /// <summary>
        /// Creates a workflow activity based on the provided Xaml and returns PowerShell script that will
        /// run the workflow.
        /// </summary>
        /// <param name="name">Workflow name</param>
        /// <param name="xaml">Workflow Xaml definition</param>
        /// <param name="requiredAssemblies">Required assemblies</param>
        /// <param name="dependentWorkflows">Dependent workflows</param>
        /// <param name="dependentAssemblyPath">Path for dependent assemblies</param>
        /// <param name="parameterValidation">Workflow parameters</param>
        /// <param name="modulePath">Module path</param>
        /// <param name="scriptWorkflow">True if this is script workflow</param>
        /// <param name="workflowAttributes">the attribute string to use for the workflow, should be '[CmdletBinding()]'</param>
        /// <param name="scriptContent">File path containing script content.</param>
        /// <param name="fullScript">Full source script.</param>
        /// <param name="rootWorkflowName">Only root Workflow will be compiled</param>
        /// <returns></returns>
        public static string CreateFunctionFromXaml(
            string name,
            string xaml,
            Dictionary<string, string> requiredAssemblies,
            string[] dependentWorkflows,
            string dependentAssemblyPath,
            Dictionary<string, ParameterAst> parameterValidation,
            string modulePath,
            bool scriptWorkflow,
            string workflowAttributes,
            string scriptContent,
            string fullScript,
            string rootWorkflowName
            )
        {
            return CreateFunctionFromXaml(name, xaml, requiredAssemblies, dependentWorkflows, dependentAssemblyPath, parameterValidation, modulePath,
                scriptWorkflow, workflowAttributes, scriptContent, fullScript, rootWorkflowName, null, null);
        }

        /// <summary>
        /// Creates a workflow activity based on the provided Xaml and returns PowerShell script that will
        /// run the workflow.
        /// </summary>
        /// <param name="name">Workflow name</param>
        /// <param name="xaml">Workflow Xaml definition</param>
        /// <param name="requiredAssemblies">Required assemblies</param>
        /// <param name="dependentWorkflows">Dependent workflows</param>
        /// <param name="dependentAssemblyPath">Path for dependent assemblies</param>
        /// <param name="parameterValidation">Workflow parameters</param>
        /// <param name="modulePath">Module path</param>
        /// <param name="scriptWorkflow">True if this is script workflow</param>
        /// <param name="workflowAttributes">the attribute string to use for the workflow, should be '[CmdletBinding()]'</param>
        /// <param name="scriptContent">File path containing script content.</param>
        /// <param name="fullScript">Full source script.</param>
        /// <param name="rootWorkflowName">Only root Workflow will be compiled</param>
        /// <param name="sourceLanguageMode">Language mode of source that is creating the workflow</param>
        /// <param name="attributeAstCollection">Optional collection of parameter attribute Asts</param>
        /// <returns></returns>
        public static string CreateFunctionFromXaml(
            string name,
            string xaml,
            Dictionary<string, string> requiredAssemblies,
            string[] dependentWorkflows,
            string dependentAssemblyPath,
            Dictionary<string, ParameterAst> parameterValidation,
            string modulePath,
            bool scriptWorkflow,
            string workflowAttributes,
            string scriptContent,
            string fullScript,
            string rootWorkflowName,
            PSLanguageMode? sourceLanguageMode,
            ReadOnlyCollection<AttributeAst> attributeAstCollection
            )
        {
            // check to see if the specified name is allowed
            if (!Regex.IsMatch(name, functionNamePattern))
            {
                throw new PSArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.WorkflowNameInvalid, name));
            }

            WorkflowJobDefinition definition = null;
            Activity workflow = null;

            if (scriptWorkflow)
            {
                workflow = DefinitionCache.Instance.GetActivityFromCache(xaml, out definition);
            }

            if (workflow == null)
            {
                definition = new WorkflowJobDefinition(typeof(WorkflowJobSourceAdapter), name, null, modulePath, dependentWorkflows, dependentAssemblyPath, requiredAssemblies, xaml);
                definition.IsScriptWorkflow = scriptWorkflow;
                bool windowsWorkflow;
                workflow = DefinitionCache.Instance.CompileActivityAndSaveInCache(definition, null, requiredAssemblies,
                                                                                 out windowsWorkflow, rootWorkflowName);
            }

            definition.WorkflowScriptFile = scriptContent;
            definition.WorkflowFullScript = fullScript;

            // this can throw exceptions if the xaml is malformed.
            DynamicActivity daBody = workflow as DynamicActivity;

            StringBuilder innerParamDefinitions = new StringBuilder();
            StringBuilder outerParamDefinitions = new StringBuilder();
            string workflowGuid = definition.InstanceId.ToString();
            Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "Generating function for name: {0}, WFGuid: {1}", name, workflowGuid));

            // String to hold any updates we do for parameter defaults
            List<string> parametersWithDefaults = new List<string>();
            string defaultUpdates = "";

            // If the workflow is a DynamicActivity, we can use the Properties
            // property to retrieve parameters to the workflow and synthesize
            // PowerShell parameter declarations.
            if (daBody != null)
            {
                foreach (var p in daBody.Properties)
                {
                    // Skip out arguments
                    if (typeof(System.Activities.OutArgument).IsAssignableFrom(p.Type))
                    {
                        continue;
                    }

                    // If the parameter name is one of the expected collisons, don't add it to the list. 
                    if (p.Name.Equals(Constants.ComputerName, StringComparison.OrdinalIgnoreCase) || p.Name.Equals(Constants.PrivateMetadata, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (p.Name.Equals("InputObject", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("AsJob", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string paramTypeName = (string)LanguagePrimitives.ConvertTo(p.Type.GetGenericArguments()[0],
                        typeof(string), CultureInfo.InvariantCulture);

                    string mandatory = "";
                    string parameterDefault = "";

                    // If we got specific validations to add, add those
                    // We only get these in script-based workflow case, but not in XAML case.
                    if ((parameterValidation != null) && parameterValidation.ContainsKey(p.Name))
                    {
                        ParameterAst parameter = parameterValidation[p.Name];

                        foreach (AttributeBaseAst attribute in parameter.Attributes)
                        {
                            innerParamDefinitions.Append(attribute.ToString());
                            innerParamDefinitions.Append("\n                    ");

                            var attributeAst = attribute as AttributeAst;
                            if (attributeAst == null || !string.Equals(attribute.TypeName.Name, "Parameter", StringComparison.OrdinalIgnoreCase))
                            {
                                // If we have a Credential Attribute, it has been added to the inner function, it does not need to be added to the outer definiton.
                                // This will prevent prompting for the cred twice.
                                if (!string.Equals(attribute.TypeName.FullName, "System.Management.Automation.CredentialAttribute", StringComparison.OrdinalIgnoreCase))
                                {
                                    outerParamDefinitions.Append(attribute.ToString());
                                    outerParamDefinitions.Append("\n                    ");
                                }

                                continue;
                            }

                            string updatedAttribute = "[Parameter(";
                            bool first = true;
                            foreach (var namedAttribute in attributeAst.NamedArguments)
                            {
                                if (string.Equals(namedAttribute.ArgumentName, "Mandatory",
                                                  StringComparison.OrdinalIgnoreCase))
                                    continue;
                                if (string.Equals(namedAttribute.ArgumentName, "ValueFromPipeline", StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(namedAttribute.Argument.Extent.Text, "$true", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new PSInvalidOperationException(Resources.ValueFromPipelineNotSupported);
                                }

                                if (!first) updatedAttribute += ",";
                                first = false;
                                updatedAttribute += namedAttribute.ToString();
                            }
                            updatedAttribute += ")]";
                            outerParamDefinitions.Append(updatedAttribute);
                            outerParamDefinitions.Append("\n                    ");
                        }

                        if (parameter.DefaultValue != null)
                        {
                            parameterDefault = " = " + parameter.DefaultValue.ToString();
                            parametersWithDefaults.Add("'" + p.Name + "'");
                        }
                    }
                    // Otherwise, add our default treatment
                    // XAML workflows only go through this code path.
                    // Scriptworkflow ALSO comes through this path.
                    else
                    {
                        // If the parameter is an In parameter with the RequiredArguement attribute then make it mandatory
                        if (typeof(System.Activities.InArgument).IsAssignableFrom(p.Type))
                        {
                            if (p.Attributes != null)
                            {
                                foreach (var attribute in p.Attributes)
                                {
                                    // Check the type of the attribute 
                                    if (attribute.TypeId.GetType() == typeof(RequiredArgumentAttribute))
                                    {
                                        mandatory = "[Parameter(Mandatory=$true)] ";
                                    }
                                }
                            }
                        }
                    }

                    innerParamDefinitions.Append(String.Format(CultureInfo.InvariantCulture,
                        "{0}[{1}] ${2}{3},\n                ",
                            mandatory, paramTypeName, p.Name, parameterDefault));
                    outerParamDefinitions.Append(String.Format(CultureInfo.InvariantCulture,
                        "[{0}] ${1}{2},\n                ",
                            paramTypeName, p.Name, parameterDefault));
                }

                if (parametersWithDefaults.Count > 0)
                {
                    defaultUpdates = @"
                        # Update any parameters that had default values in the workflow
                        $parametersWithDefaults = @(" + String.Join(", ", parametersWithDefaults.ToArray()) + ")\n";
                }
                else
                {
                    defaultUpdates = @"
                    # None of the workflow parameters had default values
                    $parametersWithDefaults = @()";
                }

                // Add code into the function to handle PSParameterCollection parameter if present.
                defaultUpdates += @"
                    trap { break }
                    $parameterCollectionProcessed = $false
                    $PSParameterCollectionDefaultsMember = $null
                    $suspendOnError = $false

                    if ($PSBoundParameters.ContainsKey('PSParameterCollection'))
                    {
                        # validate parameters used with PSParameterCollection
                        foreach ($pa in $PSBoundParameters.Keys)
                        {
                            if ($pa -eq 'JobName' -or $pa -eq 'AsJob' -or $pa -eq 'InputObject' -or $pa -eq 'PSParameterCollection')
                            {
                                continue
                            }
                            $msg = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::InvalidPSParameterCollectionAdditionalErrorMessage;
                            throw (New-Object System.Management.Automation.ErrorRecord $msg, StartWorkflow.InvalidArgument, InvalidArgument, $PSParameterCollection)
                        }
                        $parameterCollectionProcessed = $true

                        # See if there is a defaults collection, indicated by '*'
                        foreach ($collection in $PSParameterCollection)
                        {
                            if ($collection['" + Constants.ComputerName + @"'] -eq '*' )
                            {
                                if ($PSParameterCollectionDefaultsMember -ne $null)
                                {
                                    $msg = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::ParameterErrorMessage;
                                    throw ( New-Object System.Management.Automation.ErrorRecord $msg, StartWorkflow.InvalidArgument, InvalidArgument, $PSParameterCollection)
                                }
                                $PSParameterCollectionDefaultsMember = $collection;
                                foreach($parameter in $parametersWithDefaults)
                                {
                                    if(! $collection.ContainsKey($parameter))
                                    {
                                        $collection[$parameter] = (Get-Variable $parameter).Value
                                    }
                                }
                            }
                        }

                        $PSParameterCollection = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::MergeParameterCollection(
                                        $PSParameterCollection, $PSParameterCollectionDefaultsMember)

                        # canonicalize each collection...
                        $PSParameterCollection = foreach ( $c in $PSParameterCollection) {
                            if($c.containskey('AsJob') -or $c.containsKey('JobName') -or $c.containsKey('PSParameterCollection') -or $c.containsKey('InputObject'))
                            {
                                    $msg = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::InvalidPSParameterCollectionEntryErrorMessage;
                                    throw ( New-Object System.Management.Automation.ErrorRecord $msg, StartWorkflow.InvalidArgument, InvalidArgument, $PSParameterCollection)
                            }

                            if ($c['" + Constants.ErrorAction + @"'] -eq ""Suspend"")
                            {
                                $suspendOnError = $true
                                $c['" + Constants.ErrorAction + @"'] = ""Continue""
                            }

                            $validated = & """ + name + @""" @c
                            $validated['" + Constants.PSSuspendOnError + @"'] = $suspendOnError
                            $validated
                        }

                        # If there was no '*' collection, added the paramter defaults
                        # to each individual collection if the parameter isn't already there... 
                        if (-not $PSParameterCollectionDefaultsMember)
                        {
                            foreach ($collection in $PSParameterCollection)
                            {
                                foreach($parameter in $parametersWithDefaults)
                                {
                                    if(! $collection.ContainsKey($parameter))
                                    {
                                        $collection[$parameter] = (Get-Variable $parameter).Value
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if ($PSBoundParameters['" + Constants.ErrorAction + @"'] -eq ""Suspend"")
                        {
                            $errorActionPreference = ""Continue""
                            $suspendOnError = $true
                            $PSBoundParameters['" + Constants.ErrorAction + @"'] = ""Continue""
                        }

                        $PSBoundParameters = & """ + name + @""" @PSBoundParameters

                        # no PSParameterCollection so add the default values to PSBoundParameters
                        foreach($parameter in $parametersWithDefaults)
                        {
                            if(! $PSBoundParameters.ContainsKey($parameter))
                            {
                                $PSBoundParameters[$parameter] = (Get-Variable $parameter).Value
                            }
                        }
                    }
                    ";
            }

            // Escaping the single quotes from the module path
            modulePath = CodeGeneration.EscapeSingleQuotedStringContent(modulePath);

            //Generate the PowerShell function template
            string functionPrefixTemplate = AddCommonWfParameters(false, workflowAttributes);
            string validationFunctionPrefixTemplate = AddCommonWfParameters(true, workflowAttributes);

            string outerParamDefinitionsString = outerParamDefinitions.ToString();
            outerParamDefinitionsString = CodeGeneration.EscapeFormatStringContent(outerParamDefinitionsString);
            string functionPrefix = String.Format(CultureInfo.InvariantCulture, functionPrefixTemplate, outerParamDefinitionsString);

            // Generate the param block for the synthesized function
            string innerParamDefinitionsString = innerParamDefinitions.ToString();
            innerParamDefinitionsString = CodeGeneration.EscapeFormatStringContent(innerParamDefinitionsString);
            // For the parameter validation function, add an extra param definition for $PSInputCollection
            string validationFunctionPrefix = String.Format(CultureInfo.InvariantCulture, validationFunctionPrefixTemplate, innerParamDefinitionsString + "$PSInputCollection");

            StringBuilder completeFunctionDefinitionTemplate = new StringBuilder();
            completeFunctionDefinitionTemplate.AppendLine(functionPrefix);
            completeFunctionDefinitionTemplate.AppendLine("        begin {{");
            completeFunctionDefinitionTemplate.AppendLine("                function " + name + "  {{");
            completeFunctionDefinitionTemplate.AppendLine(validationFunctionPrefix);
            completeFunctionDefinitionTemplate.AppendLine("                     $PSBoundParameters");
            completeFunctionDefinitionTemplate.AppendLine("              }}");
            completeFunctionDefinitionTemplate.AppendLine(FunctionBodyTemplate);

            // Mark the function definition with sourceLanguageMode (language mode that function can run under, i.e.,
            // as trusted or not trusted), unless the workflow script is marked with the "SecurityCritical" attribute in 
            // which case the function will always be run under the current system lock down setting.
            bool isSecurityCritical = ContainsSecurityCriticalAttribute(attributeAstCollection);
            string sourceLanguageModeStr = (!isSecurityCritical && (sourceLanguageMode != null)) ? sourceLanguageMode.ToString() : string.Empty;

            // Combine the pieces to create the complete function
            string functionDefinition = String.Format(CultureInfo.InvariantCulture, completeFunctionDefinitionTemplate.ToString(),
                     defaultUpdates, workflowGuid, modulePath, sourceLanguageModeStr);

#if DEBUG
            // Verify that the generated function is valid powershell. This is only an issue when changing the
            // generation code so it's debug only...
            Collection<PSParseError> templateErrors = null;
            System.Management.Automation.PSParser.Tokenize(functionDefinition, out templateErrors);
            if (templateErrors != null && templateErrors.Count > 0)
            {
                StringBuilder message = new StringBuilder();
                foreach (PSParseError parseErr in templateErrors)
                {
                    message.Append(parseErr.Token.Content).Append(':').Append(parseErr.Message).Append("\n");
                }
                message.Append("`nFunction code:`n").Append(functionDefinition);
                throw new InvalidOperationException(message.ToString());
            }
#endif
            workflow = null;
            daBody = null;

            // strip the comments in fre build
#if !DEBUG
            functionDefinition = System.Text.RegularExpressions.Regex.Replace(functionDefinition, "^ *\\#.*$", "", RegexOptions.Multiline);
#endif
            return functionDefinition;
        }

        private static Type securityCriticalAttributeType = typeof(System.Security.SecurityCriticalAttribute);
        private static bool ContainsSecurityCriticalAttribute(ReadOnlyCollection<AttributeAst> attributeAsts)
        {
            if (attributeAsts != null)
            {
                foreach (var attributeAst in attributeAsts)
                {
                    if (attributeAst.TypeName.GetReflectionAttributeType() == securityCriticalAttributeType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterCollection"></param>
        /// <param name="defaultsParameterCollection"></param>
        /// <returns></returns>
        public static Hashtable[] MergeParameterCollection(Hashtable[] parameterCollection, Hashtable defaultsParameterCollection)
        {
            if (defaultsParameterCollection == null) return parameterCollection;

            List<Hashtable> updatedParameterCollection = new List<Hashtable>();
            foreach (var collection in parameterCollection)
            {
                if (collection.ContainsKey(Constants.ComputerName))
                {
                    var cnobject = collection[Constants.ComputerName];
                    var pso = cnobject as PSObject;
                    string name;
                    if (pso != null)
                        name = pso.BaseObject as string;
                    else
                        name = cnobject as string;
                    if (name != null && name.Equals("*")) continue;
                }
                foreach (var parameterName in defaultsParameterCollection.Keys)
                {
                    if (parameterName.Equals(Constants.ComputerName)) continue;
                    if (collection.ContainsKey(parameterName)) continue;
                    collection.Add(parameterName, defaultsParameterCollection[parameterName]);
                }

                updatedParameterCollection.Add(collection);
            }
            return updatedParameterCollection.ToArray();
        }

        /// <summary>
        /// Adds common workflow parameters
        /// </summary>
        /// <param name="innerfunction"></param>
        /// <param name="workflowAttributes"></param>
        /// <returns></returns>
        internal static string AddCommonWfParameters(bool innerfunction, string workflowAttributes)
        {
            // If changes are made to the common workflow parameters, then Intellisense will need to be updated as well
            // as the strings are hard coded, see admin\monad\src\engine\CommandCompletion\PseudoParameterBinder.cs.
            string[] commonParameters = new string[]
                                            {
                                                "[string[]] $" + Constants.ComputerName,
                                                "[ValidateNotNullOrEmpty()] $" + Constants.Credential,
                                                "[uint32] $" + Constants.ConnectionRetryCount,
                                                "[uint32] $" + Constants.ConnectionRetryIntervalSec,
                                                "[ValidateRange(1, " + Constants.Int32MaxValueDivideByThousand + ")][uint32] $" + Constants.PSRunningTime,
                                                "[ValidateRange(1, " + Constants.Int32MaxValueDivideByThousand + ")][uint32] $" + Constants.PSElapsedTime,
                                                "[bool] $" + Constants.Persist,
                                                "[ValidateNotNullOrEmpty()] [System.Management.Automation.Runspaces.AuthenticationMechanism] $" + Constants.Authentication,
                                                "[ValidateNotNullOrEmpty()][System.Management.AuthenticationLevel] $" + Constants.AuthenticationLevel,
                                                "[ValidateNotNullOrEmpty()] [string] $" + Constants.ApplicationName,
                                                "[uint32] $" + Constants.Port,
                                                "[switch] $" + Constants.UseSSL,
                                                "[ValidateNotNullOrEmpty()] [string] $" + Constants.ConfigurationName,
                                                "[ValidateNotNullOrEmpty()][string[]] $" + Constants.ConnectionURI,
                                                "[switch] $" + Constants.AllowRedirection,
                                                "[ValidateNotNullOrEmpty()][System.Management.Automation.Remoting.PSSessionOption] $" + Constants.SessionOption,
                                                "[ValidateNotNullOrEmpty()] [string] $" + Constants.CertificateThumbprint,
                                                "[hashtable] $" + Constants.PrivateMetadata,
                                                "[switch] $" + Constants.AsJob,
                                                "[string] $" + Constants.JobName,
                                                "$InputObject"
                                            };

            string functionPrefix = "";
            foreach (string workflowAttribute in workflowAttributes.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                functionPrefix +=
                    @"
                " + CodeGeneration.EscapeFormatStringContent(workflowAttribute);
            }
            
            functionPrefix += 
                @"
                param (
                    {0}";

            // For inner function, don't add -PSParameterCollection param. AsJob and JobName are needed in the inner function for
            // parameter validation in non-PSParameterCollection cases. For PSParameterCollection, they will be checked for
            // with a seperate check.
            // We want it at workflow function, but not in the subfunction that validates the parameter for PSParameterCollection
            if (!innerfunction)
            {
                functionPrefix +=
                    @"
                    [hashtable[]] $" + Constants.PSParameterCollection;
            }

            //Add all the parametersets to each of the common WF parameter
            foreach (string commonParameter in commonParameters)
            {
                if (commonParameter.Equals("$InputObject"))
                {
                    functionPrefix += @",
                    " + "[Parameter(ValueFromPipeline=$true)]" + commonParameter;
                }
                else
                {
                    functionPrefix += @",
                    " + commonParameter;
                }
            }

            //Close the param block
            functionPrefix += @"
                    )";

            return functionPrefix;
        }

        const string FunctionBodyTemplate = @"

                $PSInputCollection = New-Object 'System.Collections.Generic.List[PSObject]'
            }}

            process {{
                 if ($PSBoundParameters.ContainsKey('InputObject'))
                 {{
                     $PSInputCollection.Add($InputObject)
                 }}
            }}
            
            end {{

                {0}
                if ($PSBoundParameters['" + Constants.Credential + @"'])
                {{
                    $CredentialTransform = New-Object System.Management.Automation.CredentialAttribute
                    $LocalCredential = $CredentialTransform.Transform($ExecutionContext, $PSCredential)
                    $PSBoundParameters['PSCredential'] = [system.management.automation.pscredential]$LocalCredential

                    if (!$PSBoundParameters['" + Constants.ComputerName + @"'] -and !$PSBoundParameters['" + Constants.ConnectionURI + @"'])
                    {{
                        $PSBoundParameters['" + Constants.ComputerName + @"'] =  New-Object string @(,'localhost')
                    }}
                }}

                # Extract the job name if specified
                $jobName = ''
                if ($PSBoundParameters['" + Constants.JobName + @"'])
                {{
                    $jobName = $PSBoundParameters['" + Constants.JobName + @"']
                    [void] $PSBoundParameters.Remove('" + Constants.JobName + @"');
                }}

                # Extract the PSParameterCollection if specified
                [hashtable[]] $jobSpecifications = @()
                $parametersCollection = $null;
                if ($PSBoundParameters['" + Constants.PSParameterCollection + @"'])
                {{
                    $parameterSCollection = $PSBoundParameters['" + Constants.PSParameterCollection + @"']
                    [void] $PSBoundParameters.Remove('" + Constants.PSParameterCollection + @"');
                }}

                # Remove the InputObject parameter from the bound parameters collection
                if ($PSBoundParameters['" + Constants.InputObject + @"'])
                {{
                    [void] $PSBoundParameters.Remove('" + Constants.InputObject + @"');
                }}

                # Remove parameters consumed by this function or PowerShell itself
                $null = $PSBoundParameters.Remove('" + Constants.AsJob + @"')
                $null = $psBoundParameters.Remove('" + Constants.WarningVariable + @"')
                $null = $psBoundParameters.Remove('" + Constants.ErrorVariable + @"')
                $null = $psBoundParameters.Remove('" + Constants.OutVariable + @"')
                $null = $psBoundParameters.Remove('" + Constants.OutBuffer + @"')
                $null = $psBoundParameters.Remove('" + Constants.PipelineVariable + @"')
                $null = $psBoundParameters.Remove('" + Constants.InformationVariable + @"')
                
                # Add parameter to add the path of the workflow module, needed by Import-LocalizedData
                # which uses this as a base path to find localized content files.
                $psBoundParameters['" + Constants.ModulePath + @"'] = '{2}'

                # Variable that contains the source language mode.
                [string] $SourceLanguageMode = '{3}'

                if (Test-Path variable:\PSSenderInfo)
                {{
                    $psBoundParameters['" + Constants.PSSenderInfo + @"'] = $PSSenderInfo
                }}

                $psBoundParameters['" + Constants.PSCurrentDirectory + @"'] = $pwd.Path
                $psBoundParameters['" + Constants.PSSuspendOnError + @"'] = $suspendOnError

                # Process author-specified metadata which is set using
                # the Private member in the module manifest
                $myCommand = $MyInvocation.MyCommand
                $myModule = $myCommand.Module
                if ($myModule)
                {{
                    # The function was defined in a module so look for 
                    # the PrivateData member
                    [Hashtable] $privateData = $myModule.PrivateData -as [Hashtable]
                        
                    if ($privateData)
                    {{
                        # Extract the nested hashtable corresponding to this
                        # command
                        [hashtable] $authorMetadata = $privateData[$myCommand.Name]
                        if ($authorMetadata)
                        {{
                            # Copy the author-supplied hashtable so we can safely
                            # modify it.
                            $authorMetadata = @{{}} + $authorMetadata 
                            if ($psBoundParameters['PSPrivateMetadata'])
                            {{
                                # merge in the user-supplied metadata
                                foreach ($pair in $psPrivateMetadata.GetEnumerator())
                                {{
                                    $authorMetadata[$pair.Key] = $pair.Value
                                }}
                            }}
                            # and update the bound parameter to include the merged data
                            $psBoundParameters['" + Constants.PrivateMetadata + @"'] = $authorMetadata
                        }}
                    }}
                }}

                # Add in the input collection if there wasn't one explicitly passed
                # which can only happen through PSParameterCollection               
                if (! $PSBoundParameters['" + Constants.PSInputCollection + @"'])
                {{
                    $PSBoundParameters['" + Constants.PSInputCollection + @"'] = $PSInputCollection
                }}

                # Populate Verbose / Debug / Error from preference variables
                if (-not $PSBoundParameters.ContainsKey('" + Constants.Verbose + @"'))
                {{
                    if($verbosePreference -in ""Continue"",""Inquire"")
                    {{
                        $PSBoundParameters['" + Constants.Verbose + @"'] = [System.Management.Automation.SwitchParameter]::Present
                    }}
                }}

                if (-not $PSBoundParameters.ContainsKey('" + Constants.Debug + @"'))
                {{
                    if($debugPreference -in ""Continue"",""Inquire"")
                    {{
                        $PSBoundParameters['" + Constants.Debug + @"'] = [System.Management.Automation.SwitchParameter]::Present
                    }}
                }}

                if (-not $PSBoundParameters.ContainsKey('" + Constants.ErrorAction + @"'))
                {{
                    $PSBoundParameters['" + Constants.ErrorAction + @"'] = $errorActionPreference
                }}

                if(Test-Path variable:\errorActionPreference)
                {{
                    $errorAction = $errorActionPreference
                }}
                else
                {{
                    $errorAction = ""Continue""
                }}

                if ($PSBoundParameters['" + Constants.ErrorAction + @"'] -eq ""SilentlyContinue"")
                {{
                    $errorAction = ""SilentlyContinue""
                }}

                if($PSBoundParameters['" + Constants.ErrorAction + @"'] -eq ""Ignore"")
                {{
                    $PSBoundParameters['" + Constants.ErrorAction + @"'] = ""SilentlyContinue""
                    $errorAction = ""SilentlyContinue""
                }}

                if ($PSBoundParameters['" + Constants.ErrorAction + @"'] -eq ""Inquire"")
                {{
                    $PSBoundParameters['" + Constants.ErrorAction + @"'] = ""Continue""
                    $errorAction = ""Continue""
                }}

                if (-not $PSBoundParameters.ContainsKey('" + Constants.WarningAction + @"'))
                {{
                    $PSBoundParameters['" + Constants.WarningAction + @"'] = $warningPreference
                }}

                if(Test-Path variable:\warningPreference)
                {{
                    $warningAction = $warningPreference
                }}
                else
                {{
                    $warningAction = ""Continue""
                }}
                
                if ($PSBoundParameters['" + Constants.WarningAction + @"'] -in ""SilentlyContinue"",""Ignore"")
                {{
                    $warningAction = ""SilentlyContinue""
                }}

                if ($PSBoundParameters['" + Constants.WarningAction + @"'] -eq ""Inquire"")
                {{
                    $PSBoundParameters['" + Constants.WarningAction + @"'] = ""Continue""
                    $warningAction = ""Continue""
                }}


                if (-not $PSBoundParameters.ContainsKey('" + Constants.InformationAction + @"'))
                {{
                    $PSBoundParameters['" + Constants.InformationAction + @"'] = $informationPreference
                }}

                if(Test-Path variable:\informationPreference)
                {{
                    $informationAction = $informationPreference
                }}
                else
                {{
                    $informationAction = ""Continue""
                }}
                
                if ($PSBoundParameters['" + Constants.InformationAction + @"'] -in ""SilentlyContinue"",""Ignore"")
                {{
                    $informationAction = ""SilentlyContinue""
                }}

                if ($PSBoundParameters['" + Constants.InformationAction + @"'] -eq ""Inquire"")
                {{
                    $PSBoundParameters['" + Constants.InformationAction + @"'] = ""Continue""
                    $informationAction = ""Continue""
                }}


                #  Create the final parameter collection...
                $finalParameterCollection = $null
                if ($PSParameterCollection -ne $null)
                {{
                    $finalParameterCollection = $PSParameterCollection 
                }}
                else
                {{
                    $finalParameterCollection = $PSBoundParameters
                }}

                try
                {{
                    # Start the workflow and return the job object...
                    $debuggerActive = (@(Get-PSBreakpoint).Count -gt 0)
                    if (($debuggerActive -eq $false) -and
                        ($host -ne $null) -and
                        ($host.Runspace -ne $null) -and
                        ($host.Runspace.Debugger -ne $null))
                    {{
                        $debuggerActive = $host.Runspace.Debugger.IsActive
                    }}
                    $job = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::StartWorkflowApplication(
                                        $PSCmdlet,
                                        $jobName,
                                        '{1}',
                                        $AsJob,
                                        $parameterCollectionProcessed,
                                        $finalParameterCollection,
                                        $debuggerActive,
                                        $SourceLanguageMode)
                }}
                catch
                {{
                    # extract exception from the error record
                    $e = $_.Exception
                    # this is probably a method invocation exception so we want the inner exception
                    # if it exists
                    if ($e -is [System.Management.Automation.MethodException] -and $e.InnerException)
                    {{
                        $e = $e.InnerException
                    }}

                    $msg = [Microsoft.PowerShell.Commands.ImportWorkflowCommand]::UnableToStartWorkflowMessageMessage -f `
                        $MyInvocation.MyCommand.Name, $e.Message

                    $newException = New-Object System.Management.Automation.RuntimeException $msg, $e

                    throw (New-Object System.Management.Automation.ErrorRecord $newException, StartWorkflow.InvalidArgument, InvalidArgument, $finalParameterCollection)
                }}

                if (-not $AsJob -and $job -ne $null)
                {{
                    try
                    {{
                        Receive-Job -Job $job -Wait -Verbose -Debug -ErrorAction $errorAction -WarningAction $warningAction -InformationAction $informationAction

                        $PSCmdlet.InvokeCommand.HasErrors = $job.State -eq 'failed'
                    }}
                    finally
                    {{
                        if($job.State -ne ""Suspended"" -and $job.State -ne ""Stopped"")
                        {{
                            Remove-Job -Job $job -Force
                        }}
                        else
                        {{
                            $job
                        }}
                    }}
                }}
                else
                {{
                    $job
                }}
            }}
";
        #endregion Moved from AstToWorkflowConverter
    }
}
