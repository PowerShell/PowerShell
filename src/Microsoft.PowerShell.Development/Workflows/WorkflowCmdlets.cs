// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.Workflows
{
    /// <summary>
    /// Start-WorkflowRecording cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "WorkflowRecording")]
    [OutputType(typeof(WorkflowRecorder))]
    public sealed class StartWorkflowRecordingCommand : PSCmdlet
    {
        private static WorkflowRecorder _globalRecorder = new WorkflowRecorder();

        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public string[] Tags { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (_globalRecorder.IsRecording && !Force.IsPresent)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"Workflow recording '{_globalRecorder.WorkflowName}' is already in progress. Use -Force to stop it and start a new one."),
                    "RecordingInProgress",
                    ErrorCategory.ResourceExists,
                    _globalRecorder.WorkflowName));
                return;
            }

            if (_globalRecorder.IsRecording && Force.IsPresent)
            {
                WriteWarning($"Stopping current recording '{_globalRecorder.WorkflowName}'");
                _globalRecorder.Stop();
            }

            _globalRecorder.Start(Name, Description);

            if (Tags != null)
            {
                foreach (var tag in Tags)
                {
                    _globalRecorder.AddTag(tag);
                }
            }

            WriteVerbose($"Started recording workflow: {Name}");
            WriteObject(_globalRecorder);
        }

        internal static WorkflowRecorder GetRecorder()
        {
            return _globalRecorder;
        }
    }

    /// <summary>
    /// Stop-WorkflowRecording cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "WorkflowRecording")]
    [OutputType(typeof(Workflow))]
    public sealed class StopWorkflowRecordingCommand : PSCmdlet
    {
        [Parameter]
        public SwitchParameter Save { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = StartWorkflowRecordingCommand.GetRecorder();

            if (!recorder.IsRecording)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException("No workflow recording in progress"),
                    "NoRecordingInProgress",
                    ErrorCategory.InvalidOperation,
                    null));
                return;
            }

            var workflow = recorder.Stop();

            if (Save.IsPresent)
            {
                try
                {
                    WorkflowRepository.Save(workflow);
                    WriteVerbose($"Workflow '{workflow.Name}' saved with {workflow.Steps.Count} steps");
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(
                        ex,
                        "WorkflowSaveFailed",
                        ErrorCategory.WriteError,
                        workflow));
                    return;
                }
            }

            if (PassThru.IsPresent)
            {
                WriteObject(workflow);
            }
        }
    }

    /// <summary>
    /// Record-WorkflowStep cmdlet for manual step recording.
    /// </summary>
    [Cmdlet(VerbsData.Save, "WorkflowStep")]
    public sealed class SaveWorkflowStepCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Command { get; set; }

        [Parameter]
        public int ExitCode { get; set; } = 0;

        [Parameter]
        public SwitchParameter Failed { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = StartWorkflowRecordingCommand.GetRecorder();

            if (!recorder.IsRecording)
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException("No workflow recording in progress. Use Start-WorkflowRecording first."),
                    "NoRecordingInProgress",
                    ErrorCategory.InvalidOperation,
                    null));
                return;
            }

            var workingDir = SessionState.Path.CurrentFileSystemLocation.Path;
            recorder.RecordStep(Command, workingDir, ExitCode, !Failed.IsPresent);
            WriteVerbose($"Recorded step: {Command}");
        }
    }

    /// <summary>
    /// Get-Workflow cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Workflow")]
    [OutputType(typeof(Workflow))]
    public sealed class GetWorkflowCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [SupportsWildcards]
        public string Name { get; set; }

        [Parameter]
        public string[] Tag { get; set; }

        protected override void ProcessRecord()
        {
            List<Workflow> workflows;

            if (string.IsNullOrEmpty(Name))
            {
                workflows = WorkflowRepository.GetAll();
            }
            else if (WildcardPattern.ContainsWildcardCharacters(Name))
            {
                var pattern = new WildcardPattern(Name, WildcardOptions.IgnoreCase);
                workflows = WorkflowRepository.GetAll()
                    .Where(w => pattern.IsMatch(w.Name))
                    .ToList();
            }
            else
            {
                var workflow = WorkflowRepository.Load(Name);
                workflows = workflow != null ? new List<Workflow> { workflow } : new List<Workflow>();
            }

            // Filter by tags if specified
            if (Tag != null && Tag.Length > 0)
            {
                workflows = workflows.Where(w => Tag.Any(t => w.Tags.Contains(t, StringComparer.OrdinalIgnoreCase))).ToList();
            }

            foreach (var workflow in workflows.OrderBy(w => w.Name))
            {
                WriteObject(workflow);
            }
        }
    }

    /// <summary>
    /// Invoke-Workflow cmdlet.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Workflow", SupportsShouldProcess = true)]
    [OutputType(typeof(WorkflowExecutionResult))]
    public sealed class InvokeWorkflowCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        public Hashtable Variables { get; set; }

        [Parameter]
        public SwitchParameter WhatIf { get; set; }

        [Parameter]
        public SwitchParameter StopOnError { get; set; }

        protected override void ProcessRecord()
        {
            var workflow = WorkflowRepository.Load(Name);
            if (workflow == null)
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException($"Workflow '{Name}' not found"),
                    "WorkflowNotFound",
                    ErrorCategory.ObjectNotFound,
                    Name));
                return;
            }

            var result = new WorkflowExecutionResult
            {
                WorkflowName = workflow.Name,
                StartTime = DateTime.Now
            };

            // Merge variables
            var vars = new Dictionary<string, string>(workflow.DefaultVariables);
            if (Variables != null)
            {
                foreach (var key in Variables.Keys)
                {
                    vars[key.ToString()] = Variables[key]?.ToString() ?? string.Empty;
                }
            }

            WriteVerbose($"Executing workflow '{workflow.Name}' with {workflow.Steps.Count} steps");

            foreach (var step in workflow.Steps)
            {
                var stepResult = new StepExecutionResult
                {
                    StepNumber = step.StepNumber,
                    OriginalCommand = step.Command,
                    StartTime = DateTime.Now
                };

                // Substitute variables
                var command = SubstituteVariables(step.Command, vars);
                stepResult.ExecutedCommand = command;

                if (ShouldProcess(command, "Execute workflow step"))
                {
                    try
                    {
                        // Change to step's working directory if different
                        var currentDir = SessionState.Path.CurrentFileSystemLocation.Path;
                        if (!string.IsNullOrEmpty(step.WorkingDirectory) && step.WorkingDirectory != currentDir)
                        {
                            SessionState.Path.SetLocation(step.WorkingDirectory);
                        }

                        // Execute the command
                        var scriptBlock = ScriptBlock.Create(command);
                        var output = InvokeCommand.InvokeScript(scriptBlock.ToString());

                        stepResult.Success = true;
                        stepResult.Output = output.Select(o => o?.ToString() ?? string.Empty).ToList();

                        // Restore directory
                        if (!string.IsNullOrEmpty(step.WorkingDirectory) && step.WorkingDirectory != currentDir)
                        {
                            SessionState.Path.SetLocation(currentDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        stepResult.Success = false;
                        stepResult.Error = ex.Message;

                        WriteError(new ErrorRecord(
                            ex,
                            "WorkflowStepFailed",
                            ErrorCategory.NotSpecified,
                            step));

                        if (StopOnError.IsPresent)
                        {
                            result.Steps.Add(stepResult);
                            result.EndTime = DateTime.Now;
                            result.Success = false;
                            WriteObject(result);
                            return;
                        }
                    }
                }
                else
                {
                    stepResult.Success = true;
                    stepResult.Output = new List<string> { "[WhatIf] Would execute: " + command };
                }

                stepResult.EndTime = DateTime.Now;
                stepResult.Duration = stepResult.EndTime.Value - stepResult.StartTime;
                result.Steps.Add(stepResult);

                WriteVerbose($"Step {step.StepNumber}: {(stepResult.Success ? "Success" : "Failed")}");
            }

            result.EndTime = DateTime.Now;
            result.Success = result.Steps.All(s => s.Success);

            // Increment execution count
            workflow.ExecutionCount++;
            WorkflowRepository.Save(workflow);

            WriteObject(result);
        }

        private string SubstituteVariables(string command, Dictionary<string, string> variables)
        {
            var result = command;
            foreach (var kvp in variables)
            {
                result = result.Replace($"${{{kvp.Key}}}", kvp.Value);
                result = result.Replace($"${kvp.Key}", kvp.Value);
            }
            return result;
        }
    }

    /// <summary>
    /// Result of workflow execution.
    /// </summary>
    public class WorkflowExecutionResult
    {
        public string WorkflowName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public List<StepExecutionResult> Steps { get; set; }

        public WorkflowExecutionResult()
        {
            Steps = new List<StepExecutionResult>();
        }
    }

    /// <summary>
    /// Result of a single step execution.
    /// </summary>
    public class StepExecutionResult
    {
        public int StepNumber { get; set; }
        public string OriginalCommand { get; set; }
        public string ExecutedCommand { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public List<string> Output { get; set; }
        public string Error { get; set; }

        public StepExecutionResult()
        {
            Output = new List<string>();
        }
    }

    /// <summary>
    /// Remove-Workflow cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "Workflow", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public sealed class RemoveWorkflowCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (ShouldProcess(Name, "Remove workflow"))
            {
                if (WorkflowRepository.Delete(Name))
                {
                    WriteVerbose($"Workflow '{Name}' removed");
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ItemNotFoundException($"Workflow '{Name}' not found"),
                        "WorkflowNotFound",
                        ErrorCategory.ObjectNotFound,
                        Name));
                }
            }
        }
    }
}
