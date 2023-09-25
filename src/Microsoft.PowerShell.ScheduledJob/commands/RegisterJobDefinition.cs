// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using Microsoft.PowerShell.Commands;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet creates a new scheduled job definition object based on the provided
    /// parameter values and registers it with the Task Scheduler.
    /// </summary>
    [SuppressMessage("Microsoft.PowerShell", "PS1012:CallShouldProcessOnlyIfDeclaringSupport")]
    [Cmdlet(VerbsLifecycle.Register, "ScheduledJob", SupportsShouldProcess = true, DefaultParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223922")]
    [OutputType(typeof(ScheduledJobDefinition))]
    public sealed class RegisterScheduledJobCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string FilePathParameterSet = "FilePath";
        private const string ScriptBlockParameterSet = "ScriptBlock";

        /// <summary>
        /// File path for script to be run in job.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true,
                   ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Alias("Path")]
        [ValidateNotNullOrEmpty]
        public string FilePath
        {
            get { return _filePath; }

            set { _filePath = value; }
        }

        private string _filePath;

        /// <summary>
        /// ScriptBlock containing script to run in job.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true,
                   ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock
        {
            get { return _scriptBlock; }

            set { _scriptBlock = value; }
        }

        private ScriptBlock _scriptBlock;

        /// <summary>
        /// Name of scheduled job definition.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(Position = 0, Mandatory = true,
                   ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        private string _name;

        /// <summary>
        /// Triggers to define when job will run.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ScheduledJobTrigger[] Trigger
        {
            get { return _triggers; }

            set { _triggers = value; }
        }

        private ScheduledJobTrigger[] _triggers;

        /// <summary>
        /// Initialization script to run before the job starts.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNull]
        public ScriptBlock InitializationScript
        {
            get { return _initializationScript; }

            set { _initializationScript = value; }
        }

        private ScriptBlock _initializationScript;

        /// <summary>
        /// Runs the job in a 32-bit PowerShell process.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        public SwitchParameter RunAs32
        {
            get { return _runAs32; }

            set { _runAs32 = value; }
        }

        private SwitchParameter _runAs32;

        /// <summary>
        /// Credentials for job.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [Credential]
        public PSCredential Credential
        {
            get { return _credential; }

            set { _credential = value; }
        }

        private PSCredential _credential;

        /// <summary>
        /// Authentication mechanism to use for job.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        public AuthenticationMechanism Authentication
        {
            get { return _authenticationMechanism; }

            set { _authenticationMechanism = value; }
        }

        private AuthenticationMechanism _authenticationMechanism;

        /// <summary>
        /// Scheduling options for job.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNull]
        public ScheduledJobOptions ScheduledJobOption
        {
            get { return _options; }

            set { _options = value; }
        }

        private ScheduledJobOptions _options;

        /// <summary>
        /// Argument list for FilePath parameter.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ArgumentList
        {
            get { return _arguments; }

            set { _arguments = value; }
        }

        private object[] _arguments;

        /// <summary>
        /// Maximum number of job results allowed in job store.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        public int MaxResultCount
        {
            get { return _executionHistoryLength; }

            set { _executionHistoryLength = value; }
        }

        private int _executionHistoryLength;

        /// <summary>
        /// Runs scheduled job immediately after successful registration.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        public SwitchParameter RunNow
        {
            get { return _runNow; }

            set { _runNow = value; }
        }

        private SwitchParameter _runNow;

        /// <summary>
        /// Runs scheduled job at the repetition interval indicated by the
        /// TimeSpan value for an unending duration.
        /// </summary>
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = RegisterScheduledJobCommand.ScriptBlockParameterSet)]
        public TimeSpan RunEvery
        {
            get { return _runEvery; }

            set { _runEvery = value; }
        }

        private TimeSpan _runEvery;

        #endregion

        #region Cmdlet Overrides

        /// <summary>
        /// Process input.
        /// </summary>
        protected override void ProcessRecord()
        {
            string targetString = StringUtil.Format(ScheduledJobErrorStrings.DefinitionWhatIf, Name);
            if (!ShouldProcess(targetString, VerbsLifecycle.Register))
            {
                return;
            }

            ScheduledJobDefinition definition = null;

            switch (ParameterSetName)
            {
                case ScriptBlockParameterSet:
                    definition = CreateScriptBlockDefinition();
                    break;

                case FilePathParameterSet:
                    definition = CreateFilePathDefinition();
                    break;
            }

            if (definition != null)
            {
                // Set the MaxCount value if available.
                if (MyInvocation.BoundParameters.ContainsKey(nameof(MaxResultCount)))
                {
                    if (MaxResultCount < 1)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidMaxResultCount);
                        Exception reason = new RuntimeException(msg);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "InvalidMaxResultCountParameterForRegisterScheduledJobDefinition", ErrorCategory.InvalidArgument, null);
                        WriteError(errorRecord);

                        return;
                    }

                    definition.SetExecutionHistoryLength(MaxResultCount, false);
                }

                try
                {
                    // If RunEvery parameter is specified then create a job trigger for the definition that
                    // runs the job at the requested interval.
                    if (MyInvocation.BoundParameters.ContainsKey(nameof(RunEvery)))
                    {
                        AddRepetitionJobTriggerToDefinition(
                            definition,
                            RunEvery,
                            false);
                    }

                    definition.Register();
                    WriteObject(definition);

                    if (_runNow)
                    {
                        definition.RunAsTask();
                    }
                }
                catch (ScheduledJobException e)
                {
                    // Check for access denied error.
                    if (e.InnerException != null && e.InnerException is System.UnauthorizedAccessException)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.UnauthorizedAccessError, definition.Name);
                        Exception reason = new RuntimeException(msg, e);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "UnauthorizedAccessToRegisterScheduledJobDefinition", ErrorCategory.PermissionDenied, definition);
                        WriteError(errorRecord);
                    }
                    else if (e.InnerException != null && e.InnerException is System.IO.DirectoryNotFoundException)
                    {
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.DirectoryNotFoundError, definition.Name);
                        Exception reason = new RuntimeException(msg, e);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "DirectoryNotFoundWhenRegisteringScheduledJobDefinition", ErrorCategory.ObjectNotFound, definition);
                        WriteError(errorRecord);
                    }
                    else if (e.InnerException != null && e.InnerException is System.Runtime.Serialization.InvalidDataContractException)
                    {
                        string innerMsg = (!string.IsNullOrEmpty(e.InnerException.Message)) ? e.InnerException.Message : string.Empty;
                        string msg = StringUtil.Format(ScheduledJobErrorStrings.CannotSerializeData, definition.Name, innerMsg);
                        Exception reason = new RuntimeException(msg, e);
                        ErrorRecord errorRecord = new ErrorRecord(reason, "CannotSerializeDataWhenRegisteringScheduledJobDefinition", ErrorCategory.InvalidData, definition);
                        WriteError(errorRecord);
                    }
                    else
                    {
                        // Create record around known exception type.
                        ErrorRecord errorRecord = new ErrorRecord(e, "CantRegisterScheduledJobDefinition", ErrorCategory.InvalidOperation, definition);
                        WriteError(errorRecord);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private ScheduledJobDefinition CreateScriptBlockDefinition()
        {
            JobDefinition jobDefinition = new JobDefinition(typeof(ScheduledJobSourceAdapter), ScriptBlock.ToString(), _name);
            jobDefinition.ModuleName = ModuleName;
            Dictionary<string, object> parameterCollection = CreateCommonParameters();

            // ScriptBlock, mandatory
            parameterCollection.Add(ScheduledJobInvocationInfo.ScriptBlockParameter, ScriptBlock);

            JobInvocationInfo jobInvocationInfo = new ScheduledJobInvocationInfo(jobDefinition, parameterCollection);

            ScheduledJobDefinition definition = new ScheduledJobDefinition(jobInvocationInfo, Trigger,
                ScheduledJobOption, _credential);

            return definition;
        }

        private ScheduledJobDefinition CreateFilePathDefinition()
        {
            JobDefinition jobDefinition = new JobDefinition(typeof(ScheduledJobSourceAdapter), FilePath, _name);
            jobDefinition.ModuleName = ModuleName;
            Dictionary<string, object> parameterCollection = CreateCommonParameters();

            // FilePath, mandatory
            if (!FilePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidFilePathFile);
                Exception reason = new RuntimeException(msg);
                ErrorRecord errorRecord = new ErrorRecord(reason, "InvalidFilePathParameterForRegisterScheduledJobDefinition", ErrorCategory.InvalidArgument, this);
                WriteError(errorRecord);

                return null;
            }

            Collection<PathInfo> pathInfos = SessionState.Path.GetResolvedPSPathFromPSPath(FilePath);
            if (pathInfos.Count != 1)
            {
                string msg = StringUtil.Format(ScheduledJobErrorStrings.InvalidFilePath);
                Exception reason = new RuntimeException(msg);
                ErrorRecord errorRecord = new ErrorRecord(reason, "InvalidFilePathParameterForRegisterScheduledJobDefinition", ErrorCategory.InvalidArgument, this);
                WriteError(errorRecord);

                return null;
            }

            parameterCollection.Add(ScheduledJobInvocationInfo.FilePathParameter, pathInfos[0].Path);

            JobInvocationInfo jobInvocationInfo = new ScheduledJobInvocationInfo(jobDefinition, parameterCollection);

            ScheduledJobDefinition definition = new ScheduledJobDefinition(jobInvocationInfo, Trigger,
                ScheduledJobOption, _credential);

            return definition;
        }

        private Dictionary<string, object> CreateCommonParameters()
        {
            Dictionary<string, object> parameterCollection = new Dictionary<string, object>();

            parameterCollection.Add(ScheduledJobInvocationInfo.RunAs32Parameter, RunAs32.ToBool());
            parameterCollection.Add(ScheduledJobInvocationInfo.AuthenticationParameter, Authentication);

            if (InitializationScript != null)
            {
                parameterCollection.Add(ScheduledJobInvocationInfo.InitializationScriptParameter, InitializationScript);
            }

            if (ArgumentList != null)
            {
                parameterCollection.Add(ScheduledJobInvocationInfo.ArgumentListParameter, ArgumentList);
            }

            return parameterCollection;
        }

        #endregion
    }
}
