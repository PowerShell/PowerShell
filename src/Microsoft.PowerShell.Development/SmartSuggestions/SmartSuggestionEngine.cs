// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.SmartSuggestions
{
    /// <summary>
    /// Command suggestion with confidence score.
    /// </summary>
    public class SmartSuggestion
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public double Confidence { get; set; }
        public string Reason { get; set; }
        public List<string> RelatedCommands { get; set; }
        public Dictionary<string, object> Context { get; set; }

        public SmartSuggestion()
        {
            RelatedCommands = new List<string>();
            Context = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Command pattern for learning.
    /// </summary>
    public class CommandPattern
    {
        public string Pattern { get; set; }
        public int Frequency { get; set; }
        public DateTime LastUsed { get; set; }
        public string Context { get; set; }
        public List<string> Sequence { get; set; }

        public CommandPattern()
        {
            Sequence = new List<string>();
        }
    }

    /// <summary>
    /// Smart suggestion engine with learning capabilities.
    /// </summary>
    public class SmartSuggestionEngine
    {
        private static SmartSuggestionEngine _instance;
        private static readonly object _lock = new object();

        private Dictionary<string, CommandPattern> _patterns;
        private List<string> _commandHistory;
        private Dictionary<string, Dictionary<string, int>> _commandSequences;
        private string _dataDirectory;
        private bool _learningEnabled;

        private SmartSuggestionEngine()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".pwsh", "suggestions");

            Directory.CreateDirectory(_dataDirectory);

            _patterns = new Dictionary<string, CommandPattern>();
            _commandHistory = new List<string>();
            _commandSequences = new Dictionary<string, Dictionary<string, int>>();
            _learningEnabled = true;

            LoadPatterns();
        }

        public static SmartSuggestionEngine Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SmartSuggestionEngine();
                    }
                    return _instance;
                }
            }
        }

        public bool LearningEnabled
        {
            get => _learningEnabled;
            set => _learningEnabled = value;
        }

        public void RecordCommand(string command)
        {
            lock (_lock)
            {
                if (!_learningEnabled) return;

                _commandHistory.Add(command);

                // Update pattern frequency
                var normalizedCommand = NormalizeCommand(command);
                if (!_patterns.ContainsKey(normalizedCommand))
                {
                    _patterns[normalizedCommand] = new CommandPattern
                    {
                        Pattern = normalizedCommand,
                        Frequency = 0,
                        LastUsed = DateTime.Now
                    };
                }

                _patterns[normalizedCommand].Frequency++;
                _patterns[normalizedCommand].LastUsed = DateTime.Now;

                // Update command sequences (Markov chain)
                if (_commandHistory.Count >= 2)
                {
                    var prevCommand = NormalizeCommand(_commandHistory[_commandHistory.Count - 2]);
                    var currentCommand = normalizedCommand;

                    if (!_commandSequences.ContainsKey(prevCommand))
                    {
                        _commandSequences[prevCommand] = new Dictionary<string, int>();
                    }

                    if (!_commandSequences[prevCommand].ContainsKey(currentCommand))
                    {
                        _commandSequences[prevCommand][currentCommand] = 0;
                    }

                    _commandSequences[prevCommand][currentCommand]++;
                }

                // Save patterns periodically
                if (_commandHistory.Count % 10 == 0)
                {
                    SavePatterns();
                }
            }
        }

        public List<SmartSuggestion> GetSuggestions(string context = null, int maxSuggestions = 5)
        {
            lock (_lock)
            {
                var suggestions = new List<SmartSuggestion>();

                // Get suggestions based on recent command
                if (_commandHistory.Count > 0)
                {
                    var lastCommand = NormalizeCommand(_commandHistory.Last());
                    if (_commandSequences.ContainsKey(lastCommand))
                    {
                        var nextCommands = _commandSequences[lastCommand]
                            .OrderByDescending(kvp => kvp.Value)
                            .Take(maxSuggestions);

                        foreach (var kvp in nextCommands)
                        {
                            var totalCount = _commandSequences[lastCommand].Values.Sum();
                            var confidence = (double)kvp.Value / totalCount;

                            suggestions.Add(new SmartSuggestion
                            {
                                Command = kvp.Key,
                                Description = $"Often follows '{lastCommand}'",
                                Confidence = confidence,
                                Reason = "Sequence pattern",
                                Context = new Dictionary<string, object>
                                {
                                    { "PreviousCommand", lastCommand },
                                    { "Frequency", kvp.Value }
                                }
                            });
                        }
                    }
                }

                // Add frequently used commands
                var frequentCommands = _patterns.Values
                    .OrderByDescending(p => p.Frequency)
                    .Take(maxSuggestions)
                    .Select(p => new SmartSuggestion
                    {
                        Command = p.Pattern,
                        Description = "Frequently used command",
                        Confidence = Math.Min(1.0, p.Frequency / 100.0),
                        Reason = "Frequency pattern",
                        Context = new Dictionary<string, object>
                        {
                            { "Frequency", p.Frequency },
                            { "LastUsed", p.LastUsed }
                        }
                    });

                suggestions.AddRange(frequentCommands);

                // Add context-based suggestions
                if (!string.IsNullOrEmpty(context))
                {
                    var contextSuggestions = GetContextBasedSuggestions(context, maxSuggestions);
                    suggestions.AddRange(contextSuggestions);
                }

                // Sort by confidence and return top suggestions
                return suggestions
                    .OrderByDescending(s => s.Confidence)
                    .Take(maxSuggestions)
                    .ToList();
            }
        }

        private List<SmartSuggestion> GetContextBasedSuggestions(string context, int maxSuggestions)
        {
            var suggestions = new List<SmartSuggestion>();

            // Error context
            if (context.ToLowerInvariant().Contains("error"))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Command = "Get-AIErrorContext -Last 5",
                    Description = "Analyze recent errors",
                    Confidence = 0.8,
                    Reason = "Error detected in context"
                });

                suggestions.Add(new SmartSuggestion
                {
                    Command = "New-AIPrompt -Template Error -IncludeAll -ToClipboard",
                    Description = "Get AI help with errors",
                    Confidence = 0.75,
                    Reason = "Error detected in context"
                });
            }

            // Git context
            if (context.ToLowerInvariant().Contains("git"))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Command = "git status",
                    Description = "Check git status",
                    Confidence = 0.9,
                    Reason = "Git context detected"
                });

                suggestions.Add(new SmartSuggestion
                {
                    Command = "Get-TerminalSnapshot -IncludeGit",
                    Description = "Capture git state",
                    Confidence = 0.7,
                    Reason = "Git context detected"
                });
            }

            // Build context
            if (context.ToLowerInvariant().Contains("build"))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Command = "Get-ProjectContext | Select-Object SuggestedCommands",
                    Description = "Get project-specific build commands",
                    Confidence = 0.85,
                    Reason = "Build context detected"
                });
            }

            // Test context
            if (context.ToLowerInvariant().Contains("test"))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Command = "Get-Workflow -Tag test",
                    Description = "Get test workflows",
                    Confidence = 0.8,
                    Reason = "Test context detected"
                });
            }

            // Deploy context
            if (context.ToLowerInvariant().Contains("deploy"))
            {
                suggestions.Add(new SmartSuggestion
                {
                    Command = "Get-Workflow -Tag deploy",
                    Description = "Get deployment workflows",
                    Confidence = 0.85,
                    Reason = "Deploy context detected"
                });

                suggestions.Add(new SmartSuggestion
                {
                    Command = "New-AIPrompt -Template Deploy -IncludeAll",
                    Description = "Get AI deployment assistance",
                    Confidence = 0.75,
                    Reason = "Deploy context detected"
                });
            }

            return suggestions.Take(maxSuggestions).ToList();
        }

        private string NormalizeCommand(string command)
        {
            // Remove arguments and normalize to base command
            var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return command;

            var baseCommand = parts[0];

            // Normalize common variations
            if (baseCommand.StartsWith("./") || baseCommand.StartsWith(".\\"))
            {
                return "local-script";
            }

            return baseCommand;
        }

        private void LoadPatterns()
        {
            try
            {
                var patternsFile = Path.Combine(_dataDirectory, "patterns.json");
                if (File.Exists(patternsFile))
                {
                    var json = File.ReadAllText(patternsFile);
                    _patterns = JsonSerializer.Deserialize<Dictionary<string, CommandPattern>>(json)
                        ?? new Dictionary<string, CommandPattern>();
                }

                var sequencesFile = Path.Combine(_dataDirectory, "sequences.json");
                if (File.Exists(sequencesFile))
                {
                    var json = File.ReadAllText(sequencesFile);
                    _commandSequences = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json)
                        ?? new Dictionary<string, Dictionary<string, int>>();
                }
            }
            catch
            {
                // Start fresh if loading fails
                _patterns = new Dictionary<string, CommandPattern>();
                _commandSequences = new Dictionary<string, Dictionary<string, int>>();
            }
        }

        private void SavePatterns()
        {
            try
            {
                var patternsFile = Path.Combine(_dataDirectory, "patterns.json");
                var patternsJson = JsonSerializer.Serialize(_patterns, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(patternsFile, patternsJson);

                var sequencesFile = Path.Combine(_dataDirectory, "sequences.json");
                var sequencesJson = JsonSerializer.Serialize(_commandSequences, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sequencesFile, sequencesJson);
            }
            catch
            {
                // Ignore save errors
            }
        }

        public void ClearPatterns()
        {
            lock (_lock)
            {
                _patterns.Clear();
                _commandSequences.Clear();
                _commandHistory.Clear();
                SavePatterns();
            }
        }

        public Dictionary<string, int> GetTopCommands(int count = 10)
        {
            lock (_lock)
            {
                return _patterns.Values
                    .OrderByDescending(p => p.Frequency)
                    .Take(count)
                    .ToDictionary(p => p.Pattern, p => p.Frequency);
            }
        }
    }

    /// <summary>
    /// Get smart command suggestions based on context and history.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SmartSuggestion")]
    [Alias("suggest", "ss")]
    [OutputType(typeof(SmartSuggestion))]
    public sealed class GetSmartSuggestionCommand : PSCmdlet
    {
        /// <summary>
        /// Context for suggestions.
        /// </summary>
        [Parameter(Position = 0)]
        public string Context { get; set; }

        /// <summary>
        /// Maximum number of suggestions.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 20)]
        public int Count { get; set; } = 5;

        /// <summary>
        /// Include explanation.
        /// </summary>
        [Parameter]
        public SwitchParameter Detailed { get; set; }

        protected override void ProcessRecord()
        {
            var engine = SmartSuggestionEngine.Instance;
            var suggestions = engine.GetSuggestions(Context, Count);

            if (Detailed)
            {
                foreach (var suggestion in suggestions)
                {
                    WriteObject(suggestion);
                }
            }
            else
            {
                foreach (var suggestion in suggestions)
                {
                    WriteObject($"[{suggestion.Confidence:P0}] {suggestion.Command} - {suggestion.Description}");
                }
            }
        }
    }

    /// <summary>
    /// Enable or disable smart suggestion learning.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "SmartSuggestionLearning")]
    public sealed class EnableSmartSuggestionLearningCommand : PSCmdlet
    {
        /// <summary>
        /// Disable learning instead.
        /// </summary>
        [Parameter]
        public SwitchParameter Disable { get; set; }

        protected override void ProcessRecord()
        {
            var engine = SmartSuggestionEngine.Instance;
            engine.LearningEnabled = !Disable;

            if (Disable)
            {
                WriteObject("Smart suggestion learning disabled");
            }
            else
            {
                WriteObject("Smart suggestion learning enabled");
                WriteObject("Commands will be analyzed to improve suggestions");
            }
        }
    }

    /// <summary>
    /// Clear learned patterns.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "SmartSuggestionPatterns", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    public sealed class ClearSmartSuggestionPatternsCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            if (!ShouldProcess("Smart suggestion patterns", "Clear all learned patterns"))
            {
                return;
            }

            var engine = SmartSuggestionEngine.Instance;
            engine.ClearPatterns();

            WriteObject("Cleared all learned patterns");
        }
    }

    /// <summary>
    /// Get statistics about learned commands.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SmartSuggestionStats")]
    public sealed class GetSmartSuggestionStatsCommand : PSCmdlet
    {
        /// <summary>
        /// Number of top commands to show.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 50)]
        public int TopCount { get; set; } = 10;

        protected override void ProcessRecord()
        {
            var engine = SmartSuggestionEngine.Instance;
            var topCommands = engine.GetTopCommands(TopCount);

            WriteObject($"Smart Suggestion Statistics");
            WriteObject($"===========================");
            WriteObject($"Top {TopCount} Commands:");
            WriteObject("");

            var rank = 1;
            foreach (var kvp in topCommands)
            {
                WriteObject($"{rank}. {kvp.Key} - Used {kvp.Value} times");
                rank++;
            }
        }
    }

    /// <summary>
    /// Record a command for learning (typically called automatically).
    /// </summary>
    [Cmdlet(VerbsData.Update, "SmartSuggestionHistory")]
    public sealed class UpdateSmartSuggestionHistoryCommand : PSCmdlet
    {
        /// <summary>
        /// Command to record.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Command { get; set; }

        protected override void ProcessRecord()
        {
            var engine = SmartSuggestionEngine.Instance;
            engine.RecordCommand(Command);
        }
    }
}
