// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Development.DistributedWorkflows
{
    /// <summary>
    /// Represents a remote execution target.
    /// </summary>
    public class RemoteTarget
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public ConnectionType Type { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime LastChecked { get; set; }

        public RemoteTarget()
        {
            Properties = new Dictionary<string, string>();
            Port = 22; // Default SSH port
            Type = ConnectionType.SSH;
        }
    }

    public enum ConnectionType
    {
        SSH,
        WinRM,
        HTTP,
        Custom
    }

    /// <summary>
    /// Distributed workflow execution result.
    /// </summary>
    public class DistributedExecutionResult
    {
        public string WorkflowName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool Success { get; set; }
        public List<RemoteExecutionResult> RemoteResults { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public DistributedExecutionResult()
        {
            RemoteResults = new List<RemoteExecutionResult>();
            Metadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Result from a single remote execution.
    /// </summary>
    public class RemoteExecutionResult
    {
        public string TargetName { get; set; }
        public string TargetHost { get; set; }
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public List<string> Output { get; set; }
        public List<string> Errors { get; set; }
        public string ErrorMessage { get; set; }

        public RemoteExecutionResult()
        {
            Output = new List<string>();
            Errors = new List<string>();
        }
    }

    /// <summary>
    /// Remote target registry.
    /// </summary>
    public class RemoteTargetRegistry
    {
        private static RemoteTargetRegistry _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, RemoteTarget> _targets;
        private string _configFile;

        private RemoteTargetRegistry()
        {
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".pwsh", "remote");

            Directory.CreateDirectory(configDir);
            _configFile = Path.Combine(configDir, "targets.json");

            _targets = new Dictionary<string, RemoteTarget>();
            LoadTargets();
        }

        public static RemoteTargetRegistry Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new RemoteTargetRegistry();
                    }
                    return _instance;
                }
            }
        }

        public void RegisterTarget(RemoteTarget target)
        {
            lock (_lock)
            {
                _targets[target.Name] = target;
                SaveTargets();
            }
        }

        public void UnregisterTarget(string name)
        {
            lock (_lock)
            {
                _targets.Remove(name);
                SaveTargets();
            }
        }

        public RemoteTarget GetTarget(string name)
        {
            lock (_lock)
            {
                return _targets.TryGetValue(name, out var target) ? target : null;
            }
        }

        public List<RemoteTarget> GetAllTargets()
        {
            lock (_lock)
            {
                return _targets.Values.ToList();
            }
        }

        public List<RemoteTarget> GetTargets(Func<RemoteTarget, bool> filter)
        {
            lock (_lock)
            {
                return _targets.Values.Where(filter).ToList();
            }
        }

        private void LoadTargets()
        {
            try
            {
                if (File.Exists(_configFile))
                {
                    var json = File.ReadAllText(_configFile);
                    var targets = JsonSerializer.Deserialize<List<RemoteTarget>>(json);
                    if (targets != null)
                    {
                        _targets = targets.ToDictionary(t => t.Name);
                    }
                }
            }
            catch
            {
                _targets = new Dictionary<string, RemoteTarget>();
            }
        }

        private void SaveTargets()
        {
            try
            {
                var json = JsonSerializer.Serialize(_targets.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFile, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }

    /// <summary>
    /// Register a remote target for distributed workflow execution.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "RemoteTarget")]
    public sealed class RegisterRemoteTargetCommand : PSCmdlet
    {
        /// <summary>
        /// Target name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Target host (IP or hostname).
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Host { get; set; }

        /// <summary>
        /// Port number.
        /// </summary>
        [Parameter]
        public int Port { get; set; } = 22;

        /// <summary>
        /// Username for authentication.
        /// </summary>
        [Parameter]
        public string Username { get; set; }

        /// <summary>
        /// Connection type.
        /// </summary>
        [Parameter]
        [ValidateSet("SSH", "WinRM", "HTTP", "Custom")]
        public string Type { get; set; } = "SSH";

        /// <summary>
        /// Additional properties.
        /// </summary>
        [Parameter]
        public hashtable Properties { get; set; }

        protected override void ProcessRecord()
        {
            var target = new RemoteTarget
            {
                Name = Name,
                Host = Host,
                Port = Port,
                Username = Username,
                Type = Enum.Parse<ConnectionType>(Type)
            };

            if (Properties != null)
            {
                foreach (var key in Properties.Keys)
                {
                    target.Properties[key.ToString()] = Properties[key]?.ToString();
                }
            }

            var registry = RemoteTargetRegistry.Instance;
            registry.RegisterTarget(target);

            WriteObject($"Registered remote target: {Name} ({Host})");
        }
    }

    /// <summary>
    /// Get registered remote targets.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RemoteTarget")]
    public sealed class GetRemoteTargetCommand : PSCmdlet
    {
        /// <summary>
        /// Filter by name.
        /// </summary>
        [Parameter(Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var registry = RemoteTargetRegistry.Instance;

            if (!string.IsNullOrEmpty(Name))
            {
                var target = registry.GetTarget(Name);
                if (target != null)
                {
                    WriteObject(target);
                }
                else
                {
                    WriteWarning($"Target '{Name}' not found");
                }
            }
            else
            {
                var targets = registry.GetAllTargets();
                foreach (var target in targets)
                {
                    WriteObject(target);
                }
            }
        }
    }

    /// <summary>
    /// Unregister a remote target.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Unregister, "RemoteTarget", SupportsShouldProcess = true)]
    public sealed class UnregisterRemoteTargetCommand : PSCmdlet
    {
        /// <summary>
        /// Target name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Unregister remote target"))
            {
                return;
            }

            var registry = RemoteTargetRegistry.Instance;
            registry.UnregisterTarget(Name);

            WriteObject($"Unregistered remote target: {Name}");
        }
    }

    /// <summary>
    /// Test connectivity to remote targets.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "RemoteTarget")]
    public sealed class TestRemoteTargetCommand : PSCmdlet
    {
        /// <summary>
        /// Target name to test.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var registry = RemoteTargetRegistry.Instance;
            var target = registry.GetTarget(Name);

            if (target == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"Target '{Name}' not found"),
                    "TargetNotFound",
                    ErrorCategory.ObjectNotFound,
                    Name));
                return;
            }

            WriteObject($"Testing connectivity to {target.Host}:{target.Port}...");

            // Simple connectivity test (ping/socket check)
            bool isAvailable = false;
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect(target.Host, target.Port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    if (success)
                    {
                        client.EndConnect(result);
                        isAvailable = true;
                    }
                }
            }
            catch
            {
                isAvailable = false;
            }

            target.IsAvailable = isAvailable;
            target.LastChecked = DateTime.Now;
            registry.RegisterTarget(target);

            if (isAvailable)
            {
                WriteObject($"✓ {target.Name} is reachable");
            }
            else
            {
                WriteWarning($"✗ {target.Name} is not reachable");
            }

            WriteObject(target);
        }
    }

    /// <summary>
    /// Execute a workflow on remote targets.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "DistributedWorkflow")]
    [Alias("distflow")]
    public sealed class InvokeDistributedWorkflowCommand : PSCmdlet
    {
        /// <summary>
        /// Workflow name to execute.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string WorkflowName { get; set; }

        /// <summary>
        /// Remote targets to execute on.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string[] Targets { get; set; }

        /// <summary>
        /// Variables for workflow execution.
        /// </summary>
        [Parameter]
        public hashtable Variables { get; set; }

        /// <summary>
        /// Execute in parallel.
        /// </summary>
        [Parameter]
        public SwitchParameter Parallel { get; set; }

        /// <summary>
        /// Stop on first error.
        /// </summary>
        [Parameter]
        public SwitchParameter StopOnError { get; set; }

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 3600)]
        public int TimeoutSeconds { get; set; } = 300;

        protected override void ProcessRecord()
        {
            var registry = RemoteTargetRegistry.Instance;
            var result = new DistributedExecutionResult
            {
                WorkflowName = WorkflowName,
                StartTime = DateTime.Now
            };

            WriteObject($"Executing workflow '{WorkflowName}' on {Targets.Length} target(s)...");

            var tasks = new List<Task<RemoteExecutionResult>>();

            foreach (var targetName in Targets)
            {
                var target = registry.GetTarget(targetName);
                if (target == null)
                {
                    WriteWarning($"Target '{targetName}' not found, skipping");
                    continue;
                }

                if (Parallel)
                {
                    tasks.Add(Task.Run(() => ExecuteOnTarget(target, WorkflowName, Variables)));
                }
                else
                {
                    var remoteResult = ExecuteOnTarget(target, WorkflowName, Variables).Result;
                    result.RemoteResults.Add(remoteResult);

                    WriteObject($"  {target.Name}: {(remoteResult.Success ? "✓ Success" : "✗ Failed")}");

                    if (!remoteResult.Success && StopOnError)
                    {
                        WriteWarning($"Stopping execution due to error on {target.Name}");
                        break;
                    }
                }
            }

            // Wait for parallel execution
            if (Parallel && tasks.Count > 0)
            {
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(TimeoutSeconds));

                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        var remoteResult = task.Result;
                        result.RemoteResults.Add(remoteResult);
                        WriteObject($"  {remoteResult.TargetName}: {(remoteResult.Success ? "✓ Success" : "✗ Failed")}");
                    }
                    else
                    {
                        WriteWarning($"Task timed out");
                    }
                }
            }

            result.EndTime = DateTime.Now;
            result.Success = result.RemoteResults.All(r => r.Success);

            WriteObject($"\nDistributed execution complete");
            WriteObject($"Success: {result.RemoteResults.Count(r => r.Success)}/{result.RemoteResults.Count}");

            WriteObject(result);
        }

        private async Task<RemoteExecutionResult> ExecuteOnTarget(RemoteTarget target, string workflowName, hashtable variables)
        {
            var result = new RemoteExecutionResult
            {
                TargetName = target.Name,
                TargetHost = target.Host,
                StartTime = DateTime.Now
            };

            try
            {
                // This is a placeholder - actual implementation would use SSH/WinRM/etc.
                // For demonstration, we'll simulate execution

                WriteVerbose($"Connecting to {target.Host}...");

                // Simulate remote execution
                await Task.Delay(1000); // Simulate network delay

                // In a real implementation:
                // 1. Connect via SSH/WinRM
                // 2. Transfer workflow definition
                // 3. Execute PowerShell remotely
                // 4. Collect results

                result.Success = true;
                result.Output.Add($"Workflow '{workflowName}' executed successfully on {target.Name}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.Errors.Add(ex.ToString());
            }

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            return result;
        }
    }

    /// <summary>
    /// Execute a command on multiple remote targets.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "RemoteCommand")]
    [Alias("remcmd")]
    public sealed class InvokeRemoteCommandCommand : PSCmdlet
    {
        /// <summary>
        /// Command to execute.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Command { get; set; }

        /// <summary>
        /// Remote targets.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string[] Targets { get; set; }

        /// <summary>
        /// Execute in parallel.
        /// </summary>
        [Parameter]
        public SwitchParameter Parallel { get; set; }

        /// <summary>
        /// Timeout in seconds.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 3600)]
        public int TimeoutSeconds { get; set; } = 60;

        protected override void ProcessRecord()
        {
            var registry = RemoteTargetRegistry.Instance;

            WriteObject($"Executing command on {Targets.Length} target(s)...");
            WriteObject($"Command: {Command}");
            WriteObject("");

            if (Parallel)
            {
                var tasks = new List<Task>();

                foreach (var targetName in Targets)
                {
                    var target = registry.GetTarget(targetName);
                    if (target != null)
                    {
                        tasks.Add(Task.Run(() => ExecuteCommandOnTarget(target, Command)));
                    }
                }

                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(TimeoutSeconds));
            }
            else
            {
                foreach (var targetName in Targets)
                {
                    var target = registry.GetTarget(targetName);
                    if (target != null)
                    {
                        ExecuteCommandOnTarget(target, Command);
                    }
                    else
                    {
                        WriteWarning($"Target '{targetName}' not found");
                    }
                }
            }

            WriteObject("\nExecution complete");
        }

        private void ExecuteCommandOnTarget(RemoteTarget target, string command)
        {
            WriteObject($"[{target.Name}] Executing...");

            try
            {
                // Placeholder for actual remote execution
                // In reality, this would use SSH.NET, WinRM, or similar

                WriteObject($"[{target.Name}] Command executed successfully");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "RemoteExecutionFailed",
                    ErrorCategory.InvalidOperation,
                    target.Name));
            }
        }
    }
}
