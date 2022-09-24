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

namespace System.Management.Automation.Subsystem.Suggestion
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISuggestionProvider : ISubsystem
    {
        /// <summary>
        /// Default implementation. No function is required for a predictor.
        /// </summary>
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// Default implementation for `ISubsystem.Kind`.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.SuggestionProvider;

        /// <summary>
        /// Gets suggestion based on the given commandline and error record.
        /// </summary>
        /// <returns></returns>
        string? GetSuggestion(string commandLine, ErrorRecord lastError, CancellationToken token);
    }

    internal class GeneralCommandErrorSuggestion : ISuggestionProvider
    {
        private readonly Guid _guid;

        internal GeneralCommandErrorSuggestion()
        {
            _guid = new Guid("A3C6B07E-4A89-40C9-8BE6-2A9AAD2786A4");
        }

        public Guid Id => _guid;

        public string Name => "General";

        public string Description => "The built-in general suggestion source for command errors.";

        /// <summary>
        /// 
        /// </summary>
        public string? GetSuggestion(string commandLine, ErrorRecord lastError, CancellationToken token)
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

    internal class UnixCommandNotFoundSuggestion : ISuggestionProvider
    {
        private readonly Guid _guid;

        internal UnixCommandNotFoundSuggestion()
        {
            _guid = new Guid("47013747-CB9D-4EBC-9F02-F32B8AB19D48");
        }

        public Guid Id => _guid;

        public string Name => "UnixCommand";

        public string Description => "The built-in suggestion source for Unix command utility.";

        /// <summary>
        /// 
        /// </summary>
        public string? GetSuggestion(string commandLine, ErrorRecord lastError, CancellationToken token)
        {
            if (Platform.IsWindows || lastError.FullyQualifiedErrorId != "CommandNotFoundException")
            {
                return null;
            }

            const string cmd_not_found = "/usr/lib/command-not-found";
            var target = (string)lastError.TargetObject;
            var file = new FileInfo(cmd_not_found);
            if (file.Exists && file.UnixFileMode.HasFlag(UnixFileMode.OtherExecute))
            {
                var startInfo = new ProcessStartInfo(cmd_not_found);
                startInfo.ArgumentList.Add(target);
                startInfo.RedirectStandardError = true;

                var process = Process.Start(startInfo);
                var output = process?.StandardError.ReadToEnd();
                return output;
            }

            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class SuggestionHub
    {
        /// <summary>
        /// 
        /// </summary>
        public static List<SuggestionEntry>? GetSuggestions(Runspace runspace)
        {
            return GetSuggestions(runspace, millisecondsTimeout: 300);
        }

        /// <summary>
        /// Collect the predictive suggestions from registered predictors using the specified timeout.
        /// </summary>
        public static List<SuggestionEntry>? GetSuggestions(Runspace runspace, int millisecondsTimeout)
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

            var providers = SubsystemManager.GetSubsystems<ISuggestionProvider>();
            if (providers.Count == 0)
            {
                return null;
            }

            int length = providers.Count;
            var tasks = new List<Task<SuggestionEntry?>>(length);
            var resultList = new List<SuggestionEntry>(length);
            using var cancellationSource = new CancellationTokenSource();

            ISuggestionProvider? generalSuggestionSource = null;
            Func<object?, SuggestionEntry?> callBack = GetCallBack(lastHistory.CommandLine, lastError, cancellationSource);

            for (int i = 0; i < providers.Count; i++)
            {
                ISuggestionProvider provider = providers[i];
                if (provider is GeneralCommandErrorSuggestion)
                {
                    length--;
                    generalSuggestionSource = provider;
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

            if (generalSuggestionSource is not null)
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

                    string? text = generalSuggestionSource.GetSuggestion(lastHistory.CommandLine, lastError, cancellationSource.Token);
                    if (text is not null)
                    {
                        resultList.Add(new SuggestionEntry(generalSuggestionSource.Id, generalSuggestionSource.Name, text));
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

            foreach (Task<SuggestionEntry?> task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    SuggestionEntry? result = task.Result;
                    if (result != null)
                    {
                        resultList.Add(result);
                    }
                }
            }

            return resultList;

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered.
            static Func<object?, SuggestionEntry?> GetCallBack(
                string commandLine,
                ErrorRecord lastError,
                CancellationTokenSource cancellationSource)
            {
                return state =>
                {
                    var provider = (ISuggestionProvider)state!;
                    var text = provider.GetSuggestion(commandLine, lastError, cancellationSource.Token);
                    return text is null ? null : new SuggestionEntry(provider.Id, provider.Name, text);
                };
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SuggestionEntry
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

        internal SuggestionEntry(Guid id, string name, string text)
        {
            Id = id;
            Name = name;
            Text = text;
        }
    }
}
