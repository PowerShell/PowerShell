// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Subsystem.Prediction;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Subsystem.Feedback
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFeedbackProvider : ISubsystem
    {
        /// <summary>
        /// Default implementation. No function is required for a predictor.
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

        internal GeneralCommandErrorFeedback()
        {
            _guid = new Guid("A3C6B07E-4A89-40C9-8BE6-2A9AAD2786A4");
        }

        public Guid Id => _guid;

        public string Name => "General";

        public string Description => "The built-in general feedback source for command errors.";

        /// <summary>
        /// 
        /// </summary>
        public string? GetFeedback(string commandLine, ErrorRecord lastError, CancellationToken token)
        {
            var rsToUse = Runspace.DefaultRunspace;
            if (rsToUse is null)
            {
                return null;
            }

            EngineIntrinsics context = rsToUse.ExecutionContext.EngineIntrinsics;
            if (lastError.FullyQualifiedErrorId == "CommandNotFoundException")
            {
                var target = (string)lastError.TargetObject;
                CommandInvocationIntrinsics invocation = context.SessionState.InvokeCommand;

                // First, see if target is actually an executable file in current directory.
                {
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
                }

                if (ExperimentalFeature.IsEnabled("PSCommandNotFoundSuggestion"))
                {
                    var results = invocation.InvokeScript(@$"
                        $cmdNames = Get-Command {target} -UseFuzzyMatch | Select-Object -First 10 -Unique -ExpandProperty Name
                        [string]::Join(', ', $cmdNames)
                    ");

                    return StringUtil.Format(
                        SuggestionStrings.Suggestion_CommandNotFound,
                        results[0].ToString());
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

        /// <summary>
        /// 
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

            const string cmd_not_found = "/usr/lib/command-not-found";
            var file = new FileInfo(cmd_not_found);
            if (file.Exists && file.UnixFileMode.HasFlag(UnixFileMode.OtherExecute))
            {
                var startInfo = new ProcessStartInfo(cmd_not_found);
                startInfo.ArgumentList.Add(target);
                startInfo.RedirectStandardError = true;

                var process = Process.Start(startInfo);
                var output = process?.StandardError.ReadToEnd().Trim();

                // The feedback contains recommended actions only if the output has multiple lines of text.
                if (output?.IndexOfAny(new char[] { '\r', '\n' }) > 0)
                {
                    _notFoundFeedback = output;
                    return output;
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
            // TODO:
            // 1. text matching which is not reliable, need to re-visit.
            // 2. possible race condition???
            if (_candidates is null && _notFoundFeedback is not null)
            {
                string[] lines = _notFoundFeedback.Split(
                    new char[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (lines[0].EndsWith("but can be installed with:", StringComparison.Ordinal))
                {
                    _candidates = new List<string>(lines.Length);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        _candidates.Add(lines[i].Trim());
                    }
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

    /// <summary>
    /// 
    /// </summary>
    public static class FeedbackHub
    {
        /// <summary>
        /// 
        /// </summary>
        public static List<FeedbackEntry>? GetFeedback(Runspace runspace)
        {
            return GetFeedback(runspace, millisecondsTimeout: 300);
        }

        /// <summary>
        /// Collect the feedback from registered feedback providers using the specified timeout.
        /// </summary>
        public static List<FeedbackEntry>? GetFeedback(Runspace runspace, int millisecondsTimeout)
        {
            Requires.Condition(millisecondsTimeout > 0, nameof(millisecondsTimeout));

            var localRunspace = runspace as LocalRunspace;
            if (localRunspace is null)
            {
                return null;
            }

            // Get the last value of $?
            bool questionMarkValue = localRunspace.ExecutionContext.QuestionMarkVariableValue;
            if (questionMarkValue)
            {
                return null;
            }

            // Get the last history item
            HistoryInfo[] histories = localRunspace.History.GetEntries(id: 0, count: 1, newest: true);
            if (histories.Length == 0)
            {
                return null;
            }

            HistoryInfo lastHistory = histories[0];

            // Get the last error
            ArrayList errorList = (ArrayList)localRunspace.ExecutionContext.DollarErrorVariable;
            if (errorList.Count == 0)
            {
                return null;
            }

            var lastError = errorList[0] as ErrorRecord;
            if (lastError is null && errorList[0] is RuntimeException rtEx)
            {
                lastError = rtEx.ErrorRecord;
            }

            if (lastError?.InvocationInfo is null || lastError.InvocationInfo.HistoryId != lastHistory.Id)
            {
                return null;
            }

            var providers = SubsystemManager.GetSubsystems<IFeedbackProvider>();
            if (providers.Count == 0)
            {
                return null;
            }

            int length = providers.Count;
            var tasks = new List<Task<FeedbackEntry?>>(length);
            var resultList = new List<FeedbackEntry>(length);
            using var cancellationSource = new CancellationTokenSource();

            IFeedbackProvider? generalFeedback = null;
            Func<object?, FeedbackEntry?> callBack = GetCallBack(lastHistory.CommandLine, lastError, cancellationSource);

            for (int i = 0; i < providers.Count; i++)
            {
                IFeedbackProvider provider = providers[i];
                if (provider is GeneralCommandErrorFeedback)
                {
                    length--;
                    generalFeedback = provider;
                    continue;
                }

                tasks.Add(Task.Factory.StartNew(
                    callBack,
                    provider,
                    cancellationSource.Token,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default));
            }

            var waitTask = Task.WhenAny(
               Task.WhenAll(tasks),
               Task.Delay(millisecondsTimeout, cancellationSource.Token));

            if (generalFeedback is not null)
            {
                bool changedDefault = false;
                Runspace? oldDefault = Runspace.DefaultRunspace;

                try
                {
                    if (oldDefault != localRunspace)
                    {
                        changedDefault = true;
                        Runspace.DefaultRunspace = localRunspace;
                    }

                    string? text = generalFeedback.GetFeedback(lastHistory.CommandLine, lastError, cancellationSource.Token);
                    if (text is not null)
                    {
                        resultList.Add(new FeedbackEntry(generalFeedback.Id, generalFeedback.Name, text));
                    }
                }
                finally
                {
                    if (changedDefault)
                    {
                        Runspace.DefaultRunspace = oldDefault;
                    }

                    // Restore $?
                    localRunspace.ExecutionContext.QuestionMarkVariableValue = questionMarkValue;
                }
            }

            waitTask.Wait();
            cancellationSource.Cancel();

            foreach (Task<FeedbackEntry?> task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    FeedbackEntry? result = task.Result;
                    if (result != null)
                    {
                        resultList.Add(result);
                    }
                }
            }

            return resultList;

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered.
            static Func<object?, FeedbackEntry?> GetCallBack(
                string commandLine,
                ErrorRecord lastError,
                CancellationTokenSource cancellationSource)
            {
                return state =>
                {
                    var provider = (IFeedbackProvider)state!;
                    var text = provider.GetFeedback(commandLine, lastError, cancellationSource.Token);
                    return text is null ? null : new FeedbackEntry(provider.Id, provider.Name, text);
                };
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class FeedbackEntry
    {
        /// <summary>
        /// Gets the Id of the predictor.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the predictor.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Text { get; }

        internal FeedbackEntry(Guid id, string name, string text)
        {
            Id = id;
            Name = name;
            Text = text;
        }
    }
}
