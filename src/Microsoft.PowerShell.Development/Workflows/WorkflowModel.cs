// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerShell.Development.Workflows
{
    /// <summary>
    /// Represents a single step in a workflow.
    /// </summary>
    public class WorkflowStep
    {
        public int StepNumber { get; set; }
        public string Command { get; set; }
        public string WorkingDirectory { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan? Duration { get; set; }
        public int? ExitCode { get; set; }
        public bool Success { get; set; }
        public List<string> Output { get; set; }
        public List<string> Errors { get; set; }
        public Dictionary<string, string> Variables { get; set; }

        public WorkflowStep()
        {
            Output = new List<string>();
            Errors = new List<string>();
            Variables = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Represents a recorded workflow.
    /// </summary>
    public class Workflow
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModified { get; set; }
        public List<WorkflowStep> Steps { get; set; }
        public Dictionary<string, string> DefaultVariables { get; set; }
        public List<string> Tags { get; set; }
        public int ExecutionCount { get; set; }

        public Workflow()
        {
            Steps = new List<WorkflowStep>();
            DefaultVariables = new Dictionary<string, string>();
            Tags = new List<string>();
            CreatedDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Manages workflow persistence and retrieval.
    /// </summary>
    public static class WorkflowRepository
    {
        private static readonly object _lock = new object();
        private static string _workflowDirectory;

        static WorkflowRepository()
        {
            // Default to user's PowerShell directory
            var psHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _workflowDirectory = Path.Combine(psHome, ".pwsh", "workflows");

            // Create directory if it doesn't exist
            if (!Directory.Exists(_workflowDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_workflowDirectory);
                }
                catch
                {
                    // Fallback to temp directory
                    _workflowDirectory = Path.Combine(Path.GetTempPath(), "pwsh-workflows");
                    Directory.CreateDirectory(_workflowDirectory);
                }
            }
        }

        public static void SetWorkflowDirectory(string path)
        {
            lock (_lock)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                _workflowDirectory = path;
            }
        }

        public static string GetWorkflowDirectory()
        {
            lock (_lock)
            {
                return _workflowDirectory;
            }
        }

        public static void Save(Workflow workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            if (string.IsNullOrWhiteSpace(workflow.Name))
                throw new ArgumentException("Workflow name cannot be empty", nameof(workflow));

            lock (_lock)
            {
                workflow.LastModified = DateTime.Now;
                var filePath = GetWorkflowPath(workflow.Name);
                var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                File.WriteAllText(filePath, json);
            }
        }

        public static Workflow Load(string name)
        {
            lock (_lock)
            {
                var filePath = GetWorkflowPath(name);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Workflow>(json);
            }
        }

        public static List<Workflow> GetAll()
        {
            lock (_lock)
            {
                var workflows = new List<Workflow>();
                var files = Directory.GetFiles(_workflowDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var workflow = JsonSerializer.Deserialize<Workflow>(json);
                        if (workflow != null)
                        {
                            workflows.Add(workflow);
                        }
                    }
                    catch
                    {
                        // Skip invalid workflow files
                    }
                }

                return workflows;
            }
        }

        public static bool Delete(string name)
        {
            lock (_lock)
            {
                var filePath = GetWorkflowPath(name);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
        }

        public static bool Exists(string name)
        {
            lock (_lock)
            {
                return File.Exists(GetWorkflowPath(name));
            }
        }

        private static string GetWorkflowPath(string name)
        {
            // Sanitize filename
            var sanitized = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_workflowDirectory, $"{sanitized}.json");
        }
    }

    /// <summary>
    /// Records workflow execution.
    /// </summary>
    public class WorkflowRecorder
    {
        private Workflow _workflow;
        private bool _isRecording;
        private readonly object _lock = new object();

        public bool IsRecording
        {
            get
            {
                lock (_lock)
                {
                    return _isRecording;
                }
            }
        }

        public string WorkflowName
        {
            get
            {
                lock (_lock)
                {
                    return _workflow?.Name;
                }
            }
        }

        public void Start(string name, string description = null)
        {
            lock (_lock)
            {
                if (_isRecording)
                {
                    throw new InvalidOperationException("Recording is already in progress");
                }

                _workflow = new Workflow
                {
                    Name = name,
                    Description = description ?? string.Empty
                };
                _isRecording = true;
            }
        }

        public Workflow Stop()
        {
            lock (_lock)
            {
                if (!_isRecording)
                {
                    throw new InvalidOperationException("No recording in progress");
                }

                _isRecording = false;
                var workflow = _workflow;
                _workflow = null;
                return workflow;
            }
        }

        public void RecordStep(string command, string workingDirectory, int? exitCode = null,
            bool success = true, List<string> output = null, List<string> errors = null,
            TimeSpan? duration = null)
        {
            lock (_lock)
            {
                if (!_isRecording)
                {
                    return;
                }

                var step = new WorkflowStep
                {
                    StepNumber = _workflow.Steps.Count + 1,
                    Command = command,
                    WorkingDirectory = workingDirectory,
                    Timestamp = DateTime.Now,
                    ExitCode = exitCode,
                    Success = success,
                    Duration = duration
                };

                if (output != null)
                {
                    step.Output.AddRange(output);
                }

                if (errors != null)
                {
                    step.Errors.AddRange(errors);
                }

                _workflow.Steps.Add(step);
            }
        }

        public void AddVariable(string name, string value)
        {
            lock (_lock)
            {
                if (_isRecording && _workflow != null)
                {
                    _workflow.DefaultVariables[name] = value;
                }
            }
        }

        public void AddTag(string tag)
        {
            lock (_lock)
            {
                if (_isRecording && _workflow != null && !_workflow.Tags.Contains(tag))
                {
                    _workflow.Tags.Add(tag);
                }
            }
        }
    }
}
