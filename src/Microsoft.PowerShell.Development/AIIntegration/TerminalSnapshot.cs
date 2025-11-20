// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.Development.AIIntegration
{
    /// <summary>
    /// Represents a snapshot of the terminal state.
    /// </summary>
    public class TerminalSnapshot
    {
        public DateTime Timestamp { get; set; }
        public string WorkingDirectory { get; set; }
        public GitInfo Git { get; set; }
        public List<CommandHistoryItem> RecentCommands { get; set; }
        public Dictionary<string, string> RelevantEnvironment { get; set; }
        public List<ProcessInfo> RelevantProcesses { get; set; }
        public List<string> RecentErrors { get; set; }
        public ProjectInfo Project { get; set; }
        public SystemInfo System { get; set; }

        public TerminalSnapshot()
        {
            RecentCommands = new List<CommandHistoryItem>();
            RelevantEnvironment = new Dictionary<string, string>();
            RelevantProcesses = new List<ProcessInfo>();
            RecentErrors = new List<string>();
        }
    }

    /// <summary>
    /// Git repository information.
    /// </summary>
    public class GitInfo
    {
        public bool IsRepository { get; set; }
        public string CurrentBranch { get; set; }
        public List<string> ModifiedFiles { get; set; }
        public List<string> UntrackedFiles { get; set; }
        public int AheadBy { get; set; }
        public int BehindBy { get; set; }
        public string RemoteUrl { get; set; }
        public string LastCommit { get; set; }

        public GitInfo()
        {
            ModifiedFiles = new List<string>();
            UntrackedFiles = new List<string>();
        }
    }

    /// <summary>
    /// Command history item.
    /// </summary>
    public class CommandHistoryItem
    {
        public long Id { get; set; }
        public string CommandLine { get; set; }
        public string ExecutionStatus { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    /// <summary>
    /// Process information.
    /// </summary>
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string CommandLine { get; set; }
        public TimeSpan CpuTime { get; set; }
        public long WorkingSet { get; set; }
    }

    /// <summary>
    /// Project information.
    /// </summary>
    public class ProjectInfo
    {
        public string Type { get; set; }
        public string Language { get; set; }
        public string BuildTool { get; set; }
        public List<string> ConfigFiles { get; set; }

        public ProjectInfo()
        {
            ConfigFiles = new List<string>();
        }
    }

    /// <summary>
    /// System information.
    /// </summary>
    public class SystemInfo
    {
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string PowerShellVersion { get; set; }
        public string DotNetVersion { get; set; }
        public int ProcessorCount { get; set; }
        public long TotalMemory { get; set; }
        public long AvailableMemory { get; set; }
    }

    /// <summary>
    /// Captures a snapshot of the current terminal state.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TerminalSnapshot")]
    [OutputType(typeof(TerminalSnapshot))]
    [Alias("gts", "snapshot")]
    public sealed class GetTerminalSnapshotCommand : PSCmdlet
    {
        /// <summary>
        /// Include git information.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeGit { get; set; }

        /// <summary>
        /// Include command history.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeHistory { get; set; }

        /// <summary>
        /// Include environment variables.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeEnvironment { get; set; }

        /// <summary>
        /// Include running processes.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeProcesses { get; set; }

        /// <summary>
        /// Include recent errors.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeErrors { get; set; }

        /// <summary>
        /// Include project context.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeProject { get; set; }

        /// <summary>
        /// Include all information (shortcut).
        /// </summary>
        [Parameter]
        public SwitchParameter All { get; set; }

        /// <summary>
        /// Number of recent commands to include.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 100)]
        public int HistoryCount { get; set; } = 20;

        protected override void ProcessRecord()
        {
            var snapshot = new TerminalSnapshot
            {
                Timestamp = DateTime.Now,
                WorkingDirectory = SessionState.Path.CurrentFileSystemLocation.Path
            };

            // Git information
            if (IncludeGit || All)
            {
                snapshot.Git = GetGitInfo();
            }

            // Command history
            if (IncludeHistory || All)
            {
                snapshot.RecentCommands = GetCommandHistory();
            }

            // Environment variables
            if (IncludeEnvironment || All)
            {
                snapshot.RelevantEnvironment = GetRelevantEnvironment();
            }

            // Running processes
            if (IncludeProcesses || All)
            {
                snapshot.RelevantProcesses = GetRelevantProcesses();
            }

            // Recent errors
            if (IncludeErrors || All)
            {
                snapshot.RecentErrors = GetRecentErrors();
            }

            // Project context
            if (IncludeProject || All)
            {
                snapshot.Project = GetProjectInfo();
            }

            // System information
            snapshot.System = GetSystemInfo();

            WriteObject(snapshot);
        }

        private GitInfo GetGitInfo()
        {
            var gitInfo = new GitInfo();

            try
            {
                // Check if in git repository
                var checkGitResult = ExecuteGitCommand("rev-parse --is-inside-work-tree");
                gitInfo.IsRepository = checkGitResult?.Trim() == "true";

                if (!gitInfo.IsRepository)
                {
                    return gitInfo;
                }

                // Current branch
                gitInfo.CurrentBranch = ExecuteGitCommand("branch --show-current")?.Trim();

                // Modified files
                var statusOutput = ExecuteGitCommand("status --porcelain");
                if (!string.IsNullOrEmpty(statusOutput))
                {
                    foreach (var line in statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Length < 3) continue;

                        var status = line.Substring(0, 2).Trim();
                        var file = line.Substring(3);

                        if (status == "??")
                        {
                            gitInfo.UntrackedFiles.Add(file);
                        }
                        else
                        {
                            gitInfo.ModifiedFiles.Add(file);
                        }
                    }
                }

                // Ahead/behind
                var aheadBehind = ExecuteGitCommand("rev-list --left-right --count HEAD...@{u}");
                if (!string.IsNullOrEmpty(aheadBehind))
                {
                    var parts = aheadBehind.Trim().Split('\t');
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out var ahead);
                        int.TryParse(parts[1], out var behind);
                        gitInfo.AheadBy = ahead;
                        gitInfo.BehindBy = behind;
                    }
                }

                // Remote URL
                gitInfo.RemoteUrl = ExecuteGitCommand("remote get-url origin")?.Trim();

                // Last commit
                gitInfo.LastCommit = ExecuteGitCommand("log -1 --oneline")?.Trim();
            }
            catch
            {
                // Git not available or other error
                gitInfo.IsRepository = false;
            }

            return gitInfo;
        }

        private string ExecuteGitCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return null;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 ? output : null;
                }
            }
            catch
            {
                return null;
            }
        }

        private List<CommandHistoryItem> GetCommandHistory()
        {
            var history = new List<CommandHistoryItem>();

            try
            {
                var historyResult = InvokeCommand.InvokeScript(
                    $"Get-History -Count {HistoryCount} | Select-Object Id, CommandLine, ExecutionStatus, StartExecutionTime, EndExecutionTime"
                );

                foreach (var item in historyResult)
                {
                    var psObj = item.BaseObject as PSObject;
                    if (psObj == null) continue;

                    var historyItem = new CommandHistoryItem
                    {
                        Id = Convert.ToInt64(psObj.Properties["Id"]?.Value ?? 0),
                        CommandLine = psObj.Properties["CommandLine"]?.Value?.ToString(),
                        ExecutionStatus = psObj.Properties["ExecutionStatus"]?.Value?.ToString()
                    };

                    if (psObj.Properties["StartExecutionTime"]?.Value is DateTime startTime)
                    {
                        historyItem.StartTime = startTime;
                    }

                    if (psObj.Properties["EndExecutionTime"]?.Value is DateTime endTime)
                    {
                        historyItem.EndTime = endTime;

                        if (historyItem.StartTime.HasValue)
                        {
                            historyItem.Duration = endTime - historyItem.StartTime.Value;
                        }
                    }

                    history.Add(historyItem);
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get command history: {ex.Message}");
            }

            return history;
        }

        private Dictionary<string, string> GetRelevantEnvironment()
        {
            var env = new Dictionary<string, string>();

            // Key environment variables for development
            string[] relevantVars = {
                "PATH", "SHELL", "TERM", "LANG", "HOME", "USER", "USERNAME",
                "DOTNET_ROOT", "DOTNET_CLI_TELEMETRY_OPTOUT",
                "NODE_ENV", "NODE_PATH",
                "PYTHONPATH", "VIRTUAL_ENV",
                "GOPATH", "GOROOT",
                "JAVA_HOME",
                "CARGO_HOME", "RUSTUP_HOME",
                "GIT_AUTHOR_NAME", "GIT_AUTHOR_EMAIL",
                "CI", "GITHUB_ACTIONS", "JENKINS_URL"
            };

            foreach (var varName in relevantVars)
            {
                var value = Environment.GetEnvironmentVariable(varName);
                if (!string.IsNullOrEmpty(value))
                {
                    env[varName] = value;
                }
            }

            return env;
        }

        private List<ProcessInfo> GetRelevantProcesses()
        {
            var processes = new List<ProcessInfo>();

            try
            {
                // Get development-related processes
                string[] relevantProcessNames = {
                    "dotnet", "node", "npm", "cargo", "rustc", "python",
                    "java", "mvn", "gradle", "docker", "kubectl", "git"
                };

                var allProcesses = Process.GetProcesses();

                foreach (var proc in allProcesses)
                {
                    try
                    {
                        if (relevantProcessNames.Any(name =>
                            proc.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        {
                            processes.Add(new ProcessInfo
                            {
                                ProcessId = proc.Id,
                                ProcessName = proc.ProcessName,
                                CpuTime = proc.TotalProcessorTime,
                                WorkingSet = proc.WorkingSet64
                            });
                        }
                    }
                    catch
                    {
                        // Process may have exited
                    }
                }

                // Sort by CPU time
                processes = processes.OrderByDescending(p => p.CpuTime).Take(10).ToList();
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get process information: {ex.Message}");
            }

            return processes;
        }

        private List<string> GetRecentErrors()
        {
            var errors = new List<string>();

            try
            {
                var errorResult = InvokeCommand.InvokeScript(
                    "$Error | Select-Object -First 5 | ForEach-Object { $_.ToString() }"
                );

                foreach (var error in errorResult)
                {
                    errors.Add(error.ToString());
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to get recent errors: {ex.Message}");
            }

            return errors;
        }

        private ProjectInfo GetProjectInfo()
        {
            var project = new ProjectInfo();

            try
            {
                var result = InvokeCommand.InvokeScript(
                    "Get-ProjectContext -ErrorAction SilentlyContinue"
                );

                if (result.Any())
                {
                    var psObj = result.First().BaseObject as PSObject;
                    if (psObj != null)
                    {
                        project.Type = psObj.Properties["Type"]?.Value?.ToString();
                        project.Language = psObj.Properties["Language"]?.Value?.ToString();
                        project.BuildTool = psObj.Properties["BuildTool"]?.Value?.ToString();

                        var configFiles = psObj.Properties["ConfigFiles"]?.Value as object[];
                        if (configFiles != null)
                        {
                            project.ConfigFiles = configFiles.Select(f => f.ToString()).ToList();
                        }
                    }
                }
            }
            catch
            {
                // Get-ProjectContext not available or failed
            }

            return project;
        }

        private SystemInfo GetSystemInfo()
        {
            var system = new SystemInfo
            {
                OS = Environment.OSVersion.Platform.ToString(),
                OSVersion = Environment.OSVersion.VersionString,
                PowerShellVersion = PSVersionInfo.PSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount
            };

            // Try to get .NET version
            try
            {
                var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                var versionFile = Path.Combine(runtimeDir, ".version");
                if (File.Exists(versionFile))
                {
                    system.DotNetVersion = File.ReadAllText(versionFile).Trim();
                }
                else
                {
                    system.DotNetVersion = Environment.Version.ToString();
                }
            }
            catch
            {
                system.DotNetVersion = Environment.Version.ToString();
            }

            // Memory information (in bytes)
            try
            {
                var gc = GC.GetGCMemoryInfo();
                system.TotalMemory = gc.TotalAvailableMemoryBytes;
            }
            catch
            {
                // Not available
            }

            return system;
        }
    }
}
