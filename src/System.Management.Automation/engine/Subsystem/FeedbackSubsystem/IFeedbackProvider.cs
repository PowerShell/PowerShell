// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Subsystem.Prediction;
using System.Threading;

namespace System.Management.Automation.Subsystem.Feedback
{
    /// <summary>
    /// Interface for implementing a feedback provider on command failures.
    /// </summary>
    public interface IFeedbackProvider : ISubsystem
    {
        /// <summary>
        /// Default implementation. No function is required for a feedback provider.
        /// </summary>
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// Default implementation for `ISubsystem.Kind`.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.FeedbackProvider;

        /// <summary>
        /// Gets feedback based on the given commandline and error record.
        /// </summary>
        /// <returns></returns>
        string? GetFeedback(string commandLine, ErrorRecord lastError, CancellationToken token);
    }

    internal sealed class GeneralCommandErrorFeedback : IFeedbackProvider
    {
        private readonly Guid _guid;
        private readonly object[] _args;
        private ScriptBlock? _fuzzySb;

        internal GeneralCommandErrorFeedback()
        {
            _guid = new Guid("A3C6B07E-4A89-40C9-8BE6-2A9AAD2786A4");
            _args = new object[1];
        }

        public Guid Id => _guid;

        public string Name => "General";

        public string Description => "The built-in general feedback source for command errors.";

        public string? GetFeedback(string commandLine, ErrorRecord lastError, CancellationToken token)
        {
            var rsToUse = Runspace.DefaultRunspace;
            if (rsToUse is null)
            {
                return null;
            }

            if (lastError.FullyQualifiedErrorId == "CommandNotFoundException")
            {
                EngineIntrinsics context = rsToUse.ExecutionContext.EngineIntrinsics;

                var target = (string)lastError.TargetObject;
                CommandInvocationIntrinsics invocation = context.SessionState.InvokeCommand;

                // See if target is actually an executable file in current directory.
                var localTarget = Path.Combine(".", target);
                var command = invocation.GetCommand(
                    localTarget,
                    CommandTypes.Application | CommandTypes.ExternalScript);

                if (command is not null)
                {
                    return StringUtil.Format(
                        SuggestionStrings.Suggestion_CommandExistsInCurrentDirectory,
                        target,
                        localTarget);
                }

                // Check fuzzy matching command names.
                if (ExperimentalFeature.IsEnabled("PSCommandNotFoundSuggestion"))
                {
                    _fuzzySb ??= ScriptBlock.CreateDelayParsedScriptBlock(@$"
                        param([string] $target)
                        $cmdNames = Get-Command $target -UseFuzzyMatching -FuzzyMinimumDistance 1 | Select-Object -First 5 -Unique -ExpandProperty Name
                        if ($cmdNames) {{
                            [string]::Join(', ', $cmdNames)
                        }}
                    ", isProductCode: true);

                    _args[0] = target;
                    var result = _fuzzySb.InvokeReturnAsIs(_args);

                    if (result is not null && result != AutomationNull.Value)
                    {
                        return StringUtil.Format(
                            SuggestionStrings.Suggestion_CommandNotFound,
                            result.ToString());
                    }
                }
            }

            return null;
        }
    }

    internal sealed class UnixCommandNotFound : IFeedbackProvider, ICommandPredictor
    {
        private readonly Guid _guid;
        private string? _notFoundFeedback;
        private List<string>? _candidates;

        internal UnixCommandNotFound()
        {
            _guid = new Guid("47013747-CB9D-4EBC-9F02-F32B8AB19D48");
        }

        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        SubsystemKind ISubsystem.Kind => SubsystemKind.FeedbackProvider | SubsystemKind.CommandPredictor;

        public Guid Id => _guid;

        public string Name => "cmd-not-found";

        public string Description => "The built-in feedback/prediction source for the Unix command utility.";

        #region IFeedbackProvider

        private static string? GetUtilityPath()
        {
            string cmd_not_found = "/usr/lib/command-not-found";
            bool exist = IsFileExecutable(cmd_not_found);

            if (!exist)
            {
                cmd_not_found = "/usr/share/command-not-found/command-not-found";
                exist = IsFileExecutable(cmd_not_found);
            }

            return exist ? cmd_not_found : null;

            static bool IsFileExecutable(string path)
            {
                var file = new FileInfo(path);
                return file.Exists && file.UnixFileMode.HasFlag(UnixFileMode.OtherExecute);
            }
        }

        /// <summary>
        /// Gets feedback based on the given commandline and error record.
        /// </summary>
        public string? GetFeedback(string commandLine, ErrorRecord lastError, CancellationToken token)
        {
            if (Platform.IsWindows || lastError.FullyQualifiedErrorId != "CommandNotFoundException")
            {
                return null;
            }

            var target = (string)lastError.TargetObject;
            if (target is null)
            {
                return null;
            }

            if (target.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string? cmd_not_found = GetUtilityPath();
            if (cmd_not_found is not null)
            {
                var startInfo = new ProcessStartInfo(cmd_not_found);
                startInfo.ArgumentList.Add(target);
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                using var process = Process.Start(startInfo);
                var stderr = process?.StandardError.ReadToEnd().Trim();

                // The feedback contains recommended actions only if the output has multiple lines of text.
                if (stderr?.IndexOf('\n') > 0)
                {
                    _notFoundFeedback = stderr;

                    var stdout = process?.StandardOutput.ReadToEnd().Trim();
                    return string.IsNullOrEmpty(stdout) ? stderr : $"{stderr}\n{stdout}";
                }
            }

            return null;
        }

        #endregion

        #region ICommandPredictor

        public bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback)
        {
            return feedback switch
            {
                PredictorFeedbackKind.CommandLineAccepted => true,
                _ => false,
            };
        }

        public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken)
        {
            if (_candidates is null && _notFoundFeedback is not null)
            {
                var text = _notFoundFeedback.AsSpan();
                // Set to null to avoid potential race condition.
                _notFoundFeedback = null;

                // This loop searches for candidate results with almost no allocation.
                while (true)
                {
                    // The line is a candidate if it starts with "sudo ", such as "sudo apt install python3".
                    // 'sudo' is a command name that remains the same, so this check should work for all locales.
                    bool isCandidate = text.StartsWith("sudo ", StringComparison.Ordinal);
                    int index = text.IndexOf('\n');
                    if (isCandidate)
                    {
                        var line = index != -1 ? text.Slice(0, index) : text;
                        _candidates ??= new List<string>();
                        _candidates.Add(new string(line.TrimEnd()));
                    }

                    // Break out the loop if we are done with the last line.
                    if (index == -1 || index == text.Length - 1)
                    {
                        break;
                    }

                    // Point to the rest of feedback text.
                    text = text.Slice(index + 1);
                }
            }

            if (_candidates is not null)
            {
                string input = context.InputAst.Extent.Text;
                List<PredictiveSuggestion>? result = null;

                foreach (string c in _candidates)
                {
                    if (c.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    {
                        result ??= new List<PredictiveSuggestion>(_candidates.Count);
                        result.Add(new PredictiveSuggestion(c));
                    }
                }

                if (result is not null)
                {
                    return new SuggestionPackage(result);
                }
            }

            return default;
        }

        public void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history)
        {
            // Reset the candidate state.
            _notFoundFeedback = null;
            _candidates = null;
        }

        public void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }

        public void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }

        public void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }

        #endregion;
    }
}
