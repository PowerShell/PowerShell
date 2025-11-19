// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Development.DevCommand
{
    /// <summary>
    /// Represents the status of a DevCommand.
    /// </summary>
    public class DevCommandStatus
    {
        public string Id { get; set; }
        public string Tool { get; set; }
        public string Arguments { get; set; }
        public string State { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? ExitCode { get; set; }
        public int OutputLines { get; set; }
        public int ErrorLines { get; set; }
        public string CurrentOutput { get; set; }
        public double? ProgressPercentage { get; set; }
    }

    /// <summary>
    /// Job for executing development commands asynchronously.
    /// </summary>
    public class DevCommandJob : Job
    {
        private readonly string _tool;
        private readonly string _arguments;
        private readonly string _workingDirectory;
        private Process _process;
        private readonly StringBuilder _outputBuffer = new StringBuilder();
        private readonly StringBuilder _errorBuffer = new StringBuilder();
        private readonly List<string> _outputLines = new List<string>();
        private readonly List<string> _errorLines = new List<string>();
        private readonly object _lockObject = new object();
        private int _nextOutputIndex = 0;
        private readonly DateTime _startTime;

        public DevCommandJob(string command, string arguments, string workingDirectory, string name)
            : base(command, name)
        {
            _tool = command;
            _arguments = arguments ?? string.Empty;
            _workingDirectory = workingDirectory;
            _startTime = DateTime.Now;

            PSJobTypeName = "DevCommandJob";

            // Create a single child job to hold output
            var childJob = new PSChildJobBase(name);
            ChildJobs.Add(childJob);
        }

        public override string StatusMessage
        {
            get
            {
                if (JobStateInfo.State == JobState.Running)
                {
                    lock (_lockObject)
                    {
                        return $"Running - {_outputLines.Count} output lines, {_errorLines.Count} error lines";
                    }
                }
                return JobStateInfo.State.ToString();
            }
        }

        public override bool HasMoreData
        {
            get
            {
                lock (_lockObject)
                {
                    return _nextOutputIndex < _outputLines.Count;
                }
            }
        }

        public override string Location => _workingDirectory ?? Environment.CurrentDirectory;

        public DevCommandStatus GetStatus()
        {
            lock (_lockObject)
            {
                var status = new DevCommandStatus
                {
                    Id = Id.ToString(),
                    Tool = _tool,
                    Arguments = _arguments,
                    State = JobStateInfo.State.ToString(),
                    StartTime = _startTime,
                    OutputLines = _outputLines.Count,
                    ErrorLines = _errorLines.Count
                };

                if (JobStateInfo.State == JobState.Completed ||
                    JobStateInfo.State == JobState.Failed ||
                    JobStateInfo.State == JobState.Stopped)
                {
                    status.EndTime = DateTime.Now;
                    status.Duration = status.EndTime.Value - _startTime;

                    if (_process != null && _process.HasExited)
                    {
                        status.ExitCode = _process.ExitCode;
                    }
                }
                else
                {
                    status.Duration = DateTime.Now - _startTime;
                }

                // Get last few lines of output for status
                if (_outputLines.Count > 0)
                {
                    var lastLines = _outputLines.Count > 3
                        ? _outputLines.GetRange(_outputLines.Count - 3, 3)
                        : _outputLines;
                    status.CurrentOutput = string.Join(Environment.NewLine, lastLines);
                }

                return status;
            }
        }

        public List<string> GetNewOutput()
        {
            lock (_lockObject)
            {
                if (_nextOutputIndex >= _outputLines.Count)
                {
                    return new List<string>();
                }

                var newLines = _outputLines.GetRange(_nextOutputIndex, _outputLines.Count - _nextOutputIndex);
                _nextOutputIndex = _outputLines.Count;
                return newLines;
            }
        }

        public List<string> GetAllOutput()
        {
            lock (_lockObject)
            {
                return new List<string>(_outputLines);
            }
        }

        public List<string> GetAllErrors()
        {
            lock (_lockObject)
            {
                return new List<string>(_errorLines);
            }
        }

        public void StartJob()
        {
            SetJobState(JobState.Running);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    _process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = _tool,
                            Arguments = _arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            RedirectStandardInput = false,
                            CreateNoWindow = true,
                            WorkingDirectory = _workingDirectory ?? Environment.CurrentDirectory
                        }
                    };

                    _process.OutputDataReceived += OnOutputDataReceived;
                    _process.ErrorDataReceived += OnErrorDataReceived;

                    _process.Start();
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    _process.WaitForExit();

                    // Determine final state based on exit code
                    if (_process.ExitCode == 0)
                    {
                        SetJobState(JobState.Completed);
                    }
                    else
                    {
                        SetJobState(JobState.Failed);
                    }
                }
                catch (Exception ex)
                {
                    lock (_lockObject)
                    {
                        _errorLines.Add($"Error starting process: {ex.Message}");
                    }
                    SetJobState(JobState.Failed);
                }
            });
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (_lockObject)
                {
                    _outputLines.Add(e.Data);
                    _outputBuffer.AppendLine(e.Data);

                    // Write to output stream
                    if (ChildJobs.Count > 0)
                    {
                        var childJob = ChildJobs[0] as PSChildJobBase;
                        childJob?.Output.Add(PSObject.AsPSObject(e.Data));
                    }
                }
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (_lockObject)
                {
                    _errorLines.Add(e.Data);
                    _errorBuffer.AppendLine(e.Data);

                    // Write to error stream
                    if (ChildJobs.Count > 0)
                    {
                        var childJob = ChildJobs[0] as PSChildJobBase;
                        var errorRecord = new ErrorRecord(
                            new Exception(e.Data),
                            "DevCommandError",
                            ErrorCategory.NotSpecified,
                            _tool);
                        childJob?.Error.Add(errorRecord);
                    }
                }
            }
        }

        public override void StopJob()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    SetJobState(JobState.Stopped);
                }
            }
            catch
            {
                // Process may have already exited
            }
        }

        // Helper class for child jobs
        private class PSChildJobBase : Job
        {
            public PSChildJobBase(string name) : base(null, name)
            {
                SetJobState(JobState.Running);
            }

            public override string StatusMessage => string.Empty;
            public override bool HasMoreData => Output.Count > 0 || Error.Count > 0;
            public override string Location => string.Empty;

            public override void StopJob()
            {
                SetJobState(JobState.Stopped);
            }
        }
    }
}
