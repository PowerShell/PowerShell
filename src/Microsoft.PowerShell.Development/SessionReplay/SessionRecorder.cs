// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.PowerShell.Development.SessionReplay
{
    /// <summary>
    /// Represents a recorded terminal session.
    /// </summary>
    public class TerminalSession
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public List<SessionEvent> Events { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public string WorkingDirectory { get; set; }
        public Dictionary<string, string> Environment { get; set; }

        public TerminalSession()
        {
            Events = new List<SessionEvent>();
            Metadata = new Dictionary<string, string>();
            Environment = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Represents a session event.
    /// </summary>
    public class SessionEvent
    {
        public int Sequence { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public SessionEventType Type { get; set; }
        public string Command { get; set; }
        public string Output { get; set; }
        public string ErrorOutput { get; set; }
        public int? ExitCode { get; set; }
        public TimeSpan? ExecutionTime { get; set; }
        public string WorkingDirectory { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public SessionEvent()
        {
            Data = new Dictionary<string, object>();
        }
    }

    public enum SessionEventType
    {
        Command,
        Output,
        Error,
        DirectoryChange,
        EnvironmentChange,
        Marker,
        Annotation
    }

    /// <summary>
    /// Session recorder singleton.
    /// </summary>
    public class SessionRecorder
    {
        private static SessionRecorder _instance;
        private static readonly object _lock = new object();

        private TerminalSession _currentSession;
        private DateTime _sessionStartTime;
        private int _eventSequence;
        private string _storageDirectory;

        private SessionRecorder()
        {
            _storageDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".pwsh", "sessions");

            Directory.CreateDirectory(_storageDirectory);
        }

        public static SessionRecorder Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SessionRecorder();
                    }
                    return _instance;
                }
            }
        }

        public bool IsRecording => _currentSession != null;

        public void StartRecording(string name, string description = null)
        {
            lock (_lock)
            {
                if (IsRecording)
                {
                    throw new InvalidOperationException("A session is already being recorded");
                }

                _currentSession = new TerminalSession
                {
                    Name = name,
                    Description = description,
                    StartTime = DateTime.Now,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                _sessionStartTime = DateTime.Now;
                _eventSequence = 0;

                // Capture initial environment
                foreach (var key in new[] { "PATH", "HOME", "USER", "SHELL" })
                {
                    var value = Environment.GetEnvironmentVariable(key);
                    if (value != null)
                    {
                        _currentSession.Environment[key] = value;
                    }
                }
            }
        }

        public TerminalSession StopRecording(bool save = true)
        {
            lock (_lock)
            {
                if (!IsRecording)
                {
                    throw new InvalidOperationException("No session is currently being recorded");
                }

                _currentSession.EndTime = DateTime.Now;
                _currentSession.Duration = _currentSession.EndTime.Value - _currentSession.StartTime;

                if (save)
                {
                    SaveSession(_currentSession);
                }

                var session = _currentSession;
                _currentSession = null;

                return session;
            }
        }

        public void RecordCommand(string command, string workingDirectory = null)
        {
            lock (_lock)
            {
                if (!IsRecording) return;

                var evt = new SessionEvent
                {
                    Sequence = ++_eventSequence,
                    Timestamp = DateTime.Now,
                    ElapsedTime = DateTime.Now - _sessionStartTime,
                    Type = SessionEventType.Command,
                    Command = command,
                    WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
                };

                _currentSession.Events.Add(evt);
            }
        }

        public void RecordOutput(string output, string errorOutput = null, int? exitCode = null, TimeSpan? executionTime = null)
        {
            lock (_lock)
            {
                if (!IsRecording) return;

                // Update the last command event with output
                var lastCommand = _currentSession.Events.LastOrDefault(e => e.Type == SessionEventType.Command);
                if (lastCommand != null)
                {
                    lastCommand.Output = output;
                    lastCommand.ErrorOutput = errorOutput;
                    lastCommand.ExitCode = exitCode;
                    lastCommand.ExecutionTime = executionTime;
                }
            }
        }

        public void RecordMarker(string markerName, Dictionary<string, object> data = null)
        {
            lock (_lock)
            {
                if (!IsRecording) return;

                var evt = new SessionEvent
                {
                    Sequence = ++_eventSequence,
                    Timestamp = DateTime.Now,
                    ElapsedTime = DateTime.Now - _sessionStartTime,
                    Type = SessionEventType.Marker,
                    Command = markerName,
                    Data = data ?? new Dictionary<string, object>()
                };

                _currentSession.Events.Add(evt);
            }
        }

        public void RecordAnnotation(string annotation)
        {
            lock (_lock)
            {
                if (!IsRecording) return;

                var evt = new SessionEvent
                {
                    Sequence = ++_eventSequence,
                    Timestamp = DateTime.Now,
                    ElapsedTime = DateTime.Now - _sessionStartTime,
                    Type = SessionEventType.Annotation,
                    Command = annotation
                };

                _currentSession.Events.Add(evt);
            }
        }

        private void SaveSession(TerminalSession session)
        {
            var fileName = $"{session.Name}_{session.StartTime:yyyyMMdd-HHmmss}.json";
            var filePath = Path.Combine(_storageDirectory, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(session, options);
            File.WriteAllText(filePath, json);
        }

        public List<TerminalSession> GetSessions(string namePattern = null)
        {
            var sessions = new List<TerminalSession>();

            foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var session = JsonSerializer.Deserialize<TerminalSession>(json);

                    if (string.IsNullOrEmpty(namePattern) ||
                        session.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase))
                    {
                        sessions.Add(session);
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }

            return sessions.OrderByDescending(s => s.StartTime).ToList();
        }

        public TerminalSession LoadSession(string name)
        {
            var files = Directory.GetFiles(_storageDirectory, $"{name}_*.json");
            if (files.Length == 0)
            {
                throw new FileNotFoundException($"Session '{name}' not found");
            }

            // Get the most recent session with this name
            var latestFile = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
            var json = File.ReadAllText(latestFile);
            return JsonSerializer.Deserialize<TerminalSession>(json);
        }

        public void DeleteSession(string name)
        {
            var files = Directory.GetFiles(_storageDirectory, $"{name}_*.json");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }

    /// <summary>
    /// Start recording a terminal session.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "SessionRecording")]
    [Alias("rec", "record")]
    public sealed class StartSessionRecordingCommand : PSCmdlet
    {
        /// <summary>
        /// Name for the session.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the session.
        /// </summary>
        [Parameter]
        public string Description { get; set; }

        /// <summary>
        /// Stop existing recording if active.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;

            if (recorder.IsRecording)
            {
                if (Force)
                {
                    recorder.StopRecording(save: true);
                    WriteWarning("Stopped previous recording");
                }
                else
                {
                    throw new InvalidOperationException("A session is already being recorded. Use -Force to stop it.");
                }
            }

            recorder.StartRecording(Name, Description);

            WriteObject($"Started recording session: {Name}");
            WriteObject("All commands and outputs will be recorded");
            WriteObject("Use Stop-SessionRecording to finish");
        }
    }

    /// <summary>
    /// Stop recording the current session.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "SessionRecording")]
    public sealed class StopSessionRecordingCommand : PSCmdlet
    {
        /// <summary>
        /// Don't save the session.
        /// </summary>
        [Parameter]
        public SwitchParameter NoSave { get; set; }

        /// <summary>
        /// Return the session object.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;

            if (!recorder.IsRecording)
            {
                WriteWarning("No session is currently being recorded");
                return;
            }

            var session = recorder.StopRecording(save: !NoSave);

            WriteObject($"Stopped recording session: {session.Name}");
            WriteObject($"Duration: {session.Duration}");
            WriteObject($"Events recorded: {session.Events.Count}");

            if (!NoSave)
            {
                WriteObject($"Session saved: {session.Name}");
            }

            if (PassThru)
            {
                WriteObject(session);
            }
        }
    }

    /// <summary>
    /// Add a marker to the current recording.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "SessionMarker")]
    public sealed class AddSessionMarkerCommand : PSCmdlet
    {
        /// <summary>
        /// Marker name.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Additional data.
        /// </summary>
        [Parameter]
        public hashtable Data { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;

            if (!recorder.IsRecording)
            {
                WriteWarning("No session is currently being recorded");
                return;
            }

            var data = new Dictionary<string, object>();
            if (Data != null)
            {
                foreach (var key in Data.Keys)
                {
                    data[key.ToString()] = Data[key];
                }
            }

            recorder.RecordMarker(Name, data);
            WriteObject($"Marker added: {Name}");
        }
    }

    /// <summary>
    /// Add an annotation to the current recording.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "SessionAnnotation")]
    public sealed class AddSessionAnnotationCommand : PSCmdlet
    {
        /// <summary>
        /// Annotation text.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Text { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;

            if (!recorder.IsRecording)
            {
                WriteWarning("No session is currently being recorded");
                return;
            }

            recorder.RecordAnnotation(Text);
            WriteObject($"Annotation added: {Text}");
        }
    }

    /// <summary>
    /// Get recorded sessions.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RecordedSession")]
    [Alias("getsessions")]
    public sealed class GetRecordedSessionCommand : PSCmdlet
    {
        /// <summary>
        /// Filter by name pattern.
        /// </summary>
        [Parameter(Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;
            var sessions = recorder.GetSessions(Name);

            foreach (var session in sessions)
            {
                WriteObject(session);
            }
        }
    }

    /// <summary>
    /// Replay a recorded session.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SessionReplay")]
    [Alias("replay")]
    public sealed class InvokeSessionReplayCommand : PSCmdlet
    {
        /// <summary>
        /// Session name to replay.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Replay speed multiplier.
        /// </summary>
        [Parameter]
        [ValidateRange(0.1, 10.0)]
        public double Speed { get; set; } = 1.0;

        /// <summary>
        /// Pause between commands (milliseconds).
        /// </summary>
        [Parameter]
        [ValidateRange(0, 60000)]
        public int PauseMs { get; set; } = 0;

        /// <summary>
        /// Actually execute commands (dangerous!).
        /// </summary>
        [Parameter]
        public SwitchParameter Execute { get; set; }

        /// <summary>
        /// Skip to specific event number.
        /// </summary>
        [Parameter]
        public int? SkipTo { get; set; }

        /// <summary>
        /// Stop at specific event number.
        /// </summary>
        [Parameter]
        public int? StopAt { get; set; }

        protected override void ProcessRecord()
        {
            var recorder = SessionRecorder.Instance;
            var session = recorder.LoadSession(Name);

            WriteObject($"Replaying session: {session.Name}");
            WriteObject($"Started: {session.StartTime}");
            WriteObject($"Duration: {session.Duration}");
            WriteObject($"Events: {session.Events.Count}");
            WriteObject("");

            var events = session.Events.AsEnumerable();

            if (SkipTo.HasValue)
            {
                events = events.Where(e => e.Sequence >= SkipTo.Value);
            }

            if (StopAt.HasValue)
            {
                events = events.Where(e => e.Sequence <= StopAt.Value);
            }

            foreach (var evt in events)
            {
                // Display event
                WriteObject($"[{evt.Sequence}] [{evt.ElapsedTime:mm\\:ss}] {evt.Type}: {evt.Command}");

                if (!string.IsNullOrEmpty(evt.Output))
                {
                    WriteObject($"  Output: {evt.Output.Substring(0, Math.Min(100, evt.Output.Length))}");
                }

                if (!string.IsNullOrEmpty(evt.ErrorOutput))
                {
                    WriteWarning($"  Error: {evt.ErrorOutput.Substring(0, Math.Min(100, evt.ErrorOutput.Length))}");
                }

                // Execute if requested
                if (Execute && evt.Type == SessionEventType.Command)
                {
                    if (ShouldContinue($"Execute: {evt.Command}", "Execute Command"))
                    {
                        try
                        {
                            var result = InvokeCommand.InvokeScript(evt.Command);
                            foreach (var item in result)
                            {
                                WriteObject(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "ReplayExecutionFailed", ErrorCategory.InvalidOperation, evt.Command));
                        }
                    }
                }

                // Pause
                if (PauseMs > 0)
                {
                    System.Threading.Thread.Sleep(PauseMs);
                }
                else if (evt.ExecutionTime.HasValue)
                {
                    var delay = (int)(evt.ExecutionTime.Value.TotalMilliseconds / Speed);
                    if (delay > 0)
                    {
                        System.Threading.Thread.Sleep(Math.Min(delay, 5000));
                    }
                }
            }

            WriteObject("");
            WriteObject("Replay complete");
        }
    }

    /// <summary>
    /// Remove a recorded session.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "RecordedSession", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public sealed class RemoveRecordedSessionCommand : PSCmdlet
    {
        /// <summary>
        /// Session name to remove.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Name, "Remove recorded session"))
            {
                return;
            }

            var recorder = SessionRecorder.Instance;
            recorder.DeleteSession(Name);

            WriteObject($"Removed session: {Name}");
        }
    }
}
