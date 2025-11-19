// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace Microsoft.PowerShell.Development.DevCommand
{
    /// <summary>
    /// Start-DevCommand cmdlet for starting async development commands.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "DevCommand")]
    [OutputType(typeof(DevCommandJob))]
    [Alias("devcmd")]
    public sealed class StartDevCommandCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Tool { get; set; }

        [Parameter(Position = 1)]
        public string Arguments { get; set; }

        [Parameter]
        public string WorkingDirectory { get; set; }

        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            string workingDir = WorkingDirectory;
            if (string.IsNullOrEmpty(workingDir))
            {
                workingDir = SessionState.Path.CurrentFileSystemLocation.Path;
            }

            string jobName = Name;
            if (string.IsNullOrEmpty(jobName))
            {
                jobName = $"{Tool} {Arguments}".Trim();
                if (jobName.Length > 50)
                {
                    jobName = jobName.Substring(0, 47) + "...";
                }
            }

            var job = new DevCommandJob(Tool, Arguments, workingDir, jobName);
            job.StartJob();

            // Register job with job manager
            JobRepository.Add(job);

            WriteObject(job);
        }
    }

    /// <summary>
    /// Get-DevCommandStatus cmdlet for checking status of DevCommand jobs.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DevCommandStatus")]
    [OutputType(typeof(DevCommandStatus))]
    public sealed class GetDevCommandStatusCommand : PSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("Id")]
        public Job Job { get; set; }

        [Parameter]
        public SwitchParameter All { get; set; }

        protected override void ProcessRecord()
        {
            if (Job != null)
            {
                if (Job is DevCommandJob devJob)
                {
                    WriteObject(devJob.GetStatus());
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("Job is not a DevCommandJob"),
                        "InvalidJobType",
                        ErrorCategory.InvalidArgument,
                        Job));
                }
            }
            else if (All.IsPresent)
            {
                var allJobs = JobRepository.Jobs.Where(j => j is DevCommandJob).Cast<DevCommandJob>();
                foreach (var devJob in allJobs)
                {
                    WriteObject(devJob.GetStatus());
                }
            }
            else
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("Either specify a Job or use -All switch"),
                    "MissingParameter",
                    ErrorCategory.InvalidArgument,
                    null));
            }
        }
    }

    /// <summary>
    /// Wait-DevCommand cmdlet for waiting on DevCommand completion.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Wait, "DevCommand")]
    [OutputType(typeof(DevCommandJob))]
    public sealed class WaitDevCommandCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public Job Job { get; set; }

        [Parameter]
        public int Timeout { get; set; } = -1;

        [Parameter]
        public SwitchParameter ShowProgress { get; set; }

        protected override void ProcessRecord()
        {
            if (!(Job is DevCommandJob devJob))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("Job is not a DevCommandJob"),
                    "InvalidJobType",
                    ErrorCategory.InvalidArgument,
                    Job));
                return;
            }

            DateTime startWait = DateTime.Now;
            int lastOutputCount = 0;

            while (devJob.JobStateInfo.State == JobState.Running)
            {
                if (Timeout > 0)
                {
                    var elapsed = (DateTime.Now - startWait).TotalSeconds;
                    if (elapsed > Timeout)
                    {
                        WriteWarning($"Timeout of {Timeout} seconds reached");
                        break;
                    }
                }

                if (ShowProgress.IsPresent)
                {
                    var status = devJob.GetStatus();
                    if (status.OutputLines > lastOutputCount)
                    {
                        var newLines = devJob.GetNewOutput();
                        foreach (var line in newLines)
                        {
                            WriteVerbose(line);
                        }
                        lastOutputCount = status.OutputLines;
                    }
                }

                Thread.Sleep(100);
            }

            WriteObject(devJob);
        }
    }

    /// <summary>
    /// Stop-DevCommand cmdlet for stopping running DevCommand jobs.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "DevCommand")]
    public sealed class StopDevCommandCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public Job Job { get; set; }

        protected override void ProcessRecord()
        {
            if (!(Job is DevCommandJob devJob))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("Job is not a DevCommandJob"),
                    "InvalidJobType",
                    ErrorCategory.InvalidArgument,
                    Job));
                return;
            }

            devJob.StopJob();
            WriteVerbose($"Stopped job: {devJob.Name}");
        }
    }

    /// <summary>
    /// Receive-DevCommandOutput cmdlet for getting output from DevCommand jobs.
    /// </summary>
    [Cmdlet(VerbsCommunications.Receive, "DevCommandOutput")]
    [OutputType(typeof(string))]
    public sealed class ReceiveDevCommandOutputCommand : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public Job Job { get; set; }

        [Parameter]
        public SwitchParameter IncludeErrors { get; set; }

        [Parameter]
        public SwitchParameter NewOnly { get; set; }

        [Parameter]
        public int Last { get; set; } = -1;

        protected override void ProcessRecord()
        {
            if (!(Job is DevCommandJob devJob))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("Job is not a DevCommandJob"),
                    "InvalidJobType",
                    ErrorCategory.InvalidArgument,
                    Job));
                return;
            }

            List<string> outputLines;

            if (NewOnly.IsPresent)
            {
                outputLines = devJob.GetNewOutput();
            }
            else
            {
                outputLines = devJob.GetAllOutput();
            }

            if (Last > 0 && outputLines.Count > Last)
            {
                outputLines = outputLines.GetRange(outputLines.Count - Last, Last);
            }

            foreach (var line in outputLines)
            {
                WriteObject(line);
            }

            if (IncludeErrors.IsPresent)
            {
                var errorLines = devJob.GetAllErrors();
                if (Last > 0 && errorLines.Count > Last)
                {
                    errorLines = errorLines.GetRange(errorLines.Count - Last, Last);
                }

                foreach (var line in errorLines)
                {
                    WriteError(new ErrorRecord(
                        new Exception(line),
                        "DevCommandError",
                        ErrorCategory.NotSpecified,
                        devJob));
                }
            }
        }
    }
}
