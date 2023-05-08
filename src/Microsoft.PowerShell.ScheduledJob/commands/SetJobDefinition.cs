// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This cmdlet updates a scheduled job definition object based on the provided
    /// parameter values and saves changes to job store and Task Scheduler.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "ScheduledJob", DefaultParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=223924")]
    [OutputType(typeof(ScheduledJobDefinition))]
    public sealed class SetScheduledJobCommand : ScheduleJobCmdletBase
    {
        #region Parameters

        private const string ExecutionParameterSet = "Execution";
        private const string ScriptBlockParameterSet = "ScriptBlock";
        private const string FilePathParameterSet = "FilePath";

        /// <summary>
        /// Name of scheduled job definition.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get { return _name; }

            set { _name = value; }
        }

        private string _name;

        /// <summary>
        /// File path for script to be run in job.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
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
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock
        {
            get { return _scriptBlock; }

            set { _scriptBlock = value; }
        }

        private ScriptBlock _scriptBlock;

        /// <summary>
        /// Triggers to define when job will run.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
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
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
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
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        public SwitchParameter RunAs32
        {
            get { return _runAs32; }

            set { _runAs32 = value; }
        }

        private SwitchParameter _runAs32;

        /// <summary>
        /// Credentials for job.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [Credential()]
        public PSCredential Credential
        {
            get { return _credential; }

            set { _credential = value; }
        }

        private PSCredential _credential;

        /// <summary>
        /// Authentication mechanism to use for job.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        public AuthenticationMechanism Authentication
        {
            get { return _authenticationMechanism; }

            set { _authenticationMechanism = value; }
        }

        private AuthenticationMechanism _authenticationMechanism;

        /// <summary>
        /// Scheduling options for job.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [ValidateNotNull]
        public ScheduledJobOptions ScheduledJobOption
        {
            get { return _options; }

            set { _options = value; }
        }

        private ScheduledJobOptions _options;

        /// <summary>
        /// Input for the job.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = SetScheduledJobCommand.ExecutionParameterSet)]
        [ValidateNotNull]
        public ScheduledJobDefinition InputObject
        {
            get { return _definition; }

            set { _definition = value; }
        }

        private ScheduledJobDefinition _definition;

        /// <summary>
        /// ClearExecutionHistory.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ExecutionParameterSet)]
        public SwitchParameter ClearExecutionHistory
        {
            get { return _clearExecutionHistory; }

            set { _clearExecutionHistory = value; }
        }

        private SwitchParameter _clearExecutionHistory;

        /// <summary>
        /// Maximum number of job results allowed in job store.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        public int MaxResultCount
        {
            get { return _executionHistoryLength; }

            set { _executionHistoryLength = value; }
        }

        private int _executionHistoryLength;

        /// <summary>
        /// Pass the ScheduledJobDefinition object through to output.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.ExecutionParameterSet)]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private SwitchParameter _passThru;

        /// <summary>
        /// Argument list.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public object[] ArgumentList
        {
            get { return _arguments; }

            set { _arguments = value; }
        }

        private object[] _arguments;

        /// <summary>
        /// Runs scheduled job immediately after successfully setting job definition.
        /// </summary>
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
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
        [Parameter(ParameterSetName = SetScheduledJobCommand.ScriptBlockParameterSet)]
        [Parameter(ParameterSetName = SetScheduledJobCommand.FilePathParameterSet)]
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
            switch (ParameterSetName)
            {
                case ExecutionParameterSet:
                    UpdateExecutionDefinition();
                    break;

                case ScriptBlockParameterSet:
                case FilePathParameterSet:
                    UpdateDefinition();
                    break;
            }

            try
            {
                // If RunEvery parameter is specified then create a job trigger for the definition that
                // runs the job at the requested interval.
                bool addedTrigger = false;
                if (MyInvocation.BoundParameters.ContainsKey(nameof(RunEvery)))
                {
                    AddRepetitionJobTriggerToDefinition(
                        _definition,
                        RunEvery,
                        false);

                    addedTrigger = true;
                }

                if (Trigger != null || ScheduledJobOption != null || Credential != null || addedTrigger)
                {
                    // Save definition to file and update WTS.
                    _definition.Save();
                }
                else
                {
                    // No WTS changes.  Save definition to store only.
                    _definition.SaveToStore();
                }

                if (_runNow)
                {
                    _definition.RunAsTask();
                }
            }
            catch (ScheduledJobException e)
            {
                ErrorRecord errorRecord;

                if (e.InnerException != null &&
                    e.InnerException is System.UnauthorizedAccessException)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.NoAccessOnSetJobDefinition, _definition.Name);
                    errorRecord = new ErrorRecord(new RuntimeException(msg, e),
                        "NoAccessFailureOnSetJobDefinition", ErrorCategory.InvalidOperation, _definition);
                }
                else if (e.InnerException != null &&
                         e.InnerException is System.IO.IOException)
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.IOFailureOnSetJobDefinition, _definition.Name);
                    errorRecord = new ErrorRecord(new RuntimeException(msg, e),
                        "IOFailureOnSetJobDefinition", ErrorCategory.InvalidOperation, _definition);
                }
                else
                {
                    string msg = StringUtil.Format(ScheduledJobErrorStrings.CantSetJobDefinition, _definition.Name);
                    errorRecord = new ErrorRecord(new RuntimeException(msg, e),
                        "CantSetPropertiesToScheduledJobDefinition", ErrorCategory.InvalidOperation, _definition);
                }

                WriteError(errorRecord);
            }

            if (_passThru)
            {
                WriteObject(_definition);
            }
        }

        #endregion

        #region Private Methods

        private void UpdateExecutionDefinition()
        {
            if (_clearExecutionHistory)
            {
                _definition.ClearExecutionHistory();
            }
        }

        private void UpdateDefinition()
        {
            if (_name != null &&
                string.Compare(_name, _definition.Name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                _definition.RenameAndSave(_name);
            }

            UpdateJobInvocationInfo();

            if (MyInvocation.BoundParameters.ContainsKey(nameof(MaxResultCount)))
            {
                _definition.SetExecutionHistoryLength(MaxResultCount, false);
            }

            if (Credential != null)
            {
                _definition.Credential = Credential;
            }

            if (Trigger != null)
            {
                _definition.SetTriggers(Trigger, false);
            }

            if (ScheduledJobOption != null)
            {
                _definition.UpdateOptions(ScheduledJobOption, false);
            }
        }

        /// <summary>
        /// Create new ScheduledJobInvocationInfo object with update information and
        /// update the job definition object.
        /// </summary>
        private void UpdateJobInvocationInfo()
        {
            Dictionary<string, object> parameters = UpdateParameters();
            string name = _definition.Name;
            string command;

            if (ScriptBlock != null)
            {
                command = ScriptBlock.ToString();
            }
            else if (FilePath != null)
            {
                command = FilePath;
            }
            else
            {
                command = _definition.InvocationInfo.Command;
            }

            JobDefinition jobDefinition = new JobDefinition(typeof(ScheduledJobSourceAdapter), command, name);
            jobDefinition.ModuleName = ModuleName;
            JobInvocationInfo jobInvocationInfo = new ScheduledJobInvocationInfo(jobDefinition, parameters);

            _definition.UpdateJobInvocationInfo(jobInvocationInfo, false);
        }

        /// <summary>
        /// Creates a new parameter dictionary with update parameters.
        /// </summary>
        /// <returns>Updated parameters.</returns>
        private Dictionary<string, object> UpdateParameters()
        {
            Debug.Assert(_definition.InvocationInfo.Parameters.Count != 0,
                "ScheduledJobDefinition must always have some job invocation parameters");
            Dictionary<string, object> newParameters = new Dictionary<string, object>();
            foreach (CommandParameter parameter in _definition.InvocationInfo.Parameters[0])
            {
                newParameters.Add(parameter.Name, parameter.Value);
            }

            // RunAs32
            if (MyInvocation.BoundParameters.ContainsKey(nameof(RunAs32)))
            {
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.RunAs32Parameter))
                {
                    newParameters[ScheduledJobInvocationInfo.RunAs32Parameter] = RunAs32.ToBool();
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.RunAs32Parameter, RunAs32.ToBool());
                }
            }

            // Authentication
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Authentication)))
            {
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.AuthenticationParameter))
                {
                    newParameters[ScheduledJobInvocationInfo.AuthenticationParameter] = Authentication;
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.AuthenticationParameter, Authentication);
                }
            }

            // InitializationScript
            if (InitializationScript == null)
            {
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.InitializationScriptParameter))
                {
                    newParameters.Remove(ScheduledJobInvocationInfo.InitializationScriptParameter);
                }
            }
            else
            {
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.InitializationScriptParameter))
                {
                    newParameters[ScheduledJobInvocationInfo.InitializationScriptParameter] = InitializationScript;
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.InitializationScriptParameter, InitializationScript);
                }
            }

            // ScriptBlock
            if (ScriptBlock != null)
            {
                // FilePath cannot also be specified.
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.FilePathParameter))
                {
                    newParameters.Remove(ScheduledJobInvocationInfo.FilePathParameter);
                }

                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.ScriptBlockParameter))
                {
                    newParameters[ScheduledJobInvocationInfo.ScriptBlockParameter] = ScriptBlock;
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.ScriptBlockParameter, ScriptBlock);
                }
            }

            // FilePath
            if (FilePath != null)
            {
                // ScriptBlock cannot also be specified.
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.ScriptBlockParameter))
                {
                    newParameters.Remove(ScheduledJobInvocationInfo.ScriptBlockParameter);
                }

                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.FilePathParameter))
                {
                    newParameters[ScheduledJobInvocationInfo.FilePathParameter] = FilePath;
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.FilePathParameter, FilePath);
                }
            }

            // ArgumentList
            if (ArgumentList == null)
            {
                // Clear existing argument list only if new scriptblock or script file path was specified
                // (in this case old argument list is invalid).
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.ArgumentListParameter) &&
                    (ScriptBlock != null || FilePath != null))
                {
                    newParameters.Remove(ScheduledJobInvocationInfo.ArgumentListParameter);
                }
            }
            else
            {
                if (newParameters.ContainsKey(ScheduledJobInvocationInfo.ArgumentListParameter))
                {
                    newParameters[ScheduledJobInvocationInfo.ArgumentListParameter] = ArgumentList;
                }
                else
                {
                    newParameters.Add(ScheduledJobInvocationInfo.ArgumentListParameter, ArgumentList);
                }
            }

            return newParameters;
        }

        #endregion
    }
}
