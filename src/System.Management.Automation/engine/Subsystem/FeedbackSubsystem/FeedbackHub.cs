// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Subsystem.Feedback
{
    /// <summary>
    /// The class represents a result from a feedback provider.
    /// </summary>
    public class FeedbackResult
    {
        /// <summary>
        /// Gets the Id of the feedback provider.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the feedback provider.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the feedback item.
        /// </summary>
        public FeedbackItem Item { get; }

        internal FeedbackResult(Guid id, string name, FeedbackItem item)
        {
            Id = id;
            Name = name;
            Item = item;
        }
    }

    /// <summary>
    /// Provides a set of feedbacks for given input.
    /// </summary>
    public static class FeedbackHub
    {
        /// <summary>
        /// Collect the feedback from registered feedback providers using the default timeout.
        /// </summary>
        public static List<FeedbackResult>? GetFeedback(Runspace runspace)
        {
            return GetFeedback(runspace, millisecondsTimeout: 300);
        }

        /// <summary>
        /// Collect the feedback from registered feedback providers using the specified timeout.
        /// </summary>
        public static List<FeedbackResult>? GetFeedback(Runspace runspace, int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(millisecondsTimeout);

            if (runspace is not LocalRunspace localRunspace)
            {
                return null;
            }

            var providers = SubsystemManager.GetSubsystems<IFeedbackProvider>();
            if (providers.Count is 0)
            {
                return null;
            }

            ExecutionContext executionContext = localRunspace.ExecutionContext;
            bool questionMarkValue = executionContext.QuestionMarkVariableValue;

            // The command line would have run successfully in most cases during an interactive use of the shell.
            // So, we do a quick check to see whether we can skip proceeding, so as to avoid unneeded allocations
            // from the 'TryGetFeedbackContext' call below.
            if (questionMarkValue && CanSkip(providers))
            {
                return null;
            }

            // Get the last history item
            HistoryInfo[] histories = localRunspace.History.GetEntries(id: 0, count: 1, newest: true);
            if (histories.Length is 0)
            {
                return null;
            }

            // Try creating the feedback context object.
            if (!TryGetFeedbackContext(executionContext, questionMarkValue, histories[0], out FeedbackContext? feedbackContext))
            {
                return null;
            }

            int count = providers.Count;
            int maximumTimeoutMilliseconds = millisecondsTimeout;
            IFeedbackProvider? generalFeedback = null;
            List<Task<FeedbackResult?>>? tasks = null;
            CancellationTokenSource? cancellationSource = null;
            Func<object?, FeedbackResult?>? callBack = null;

            foreach (IFeedbackProvider provider in providers)
            {
                if (provider.TimeoutMilliseconds > maximumTimeoutMilliseconds)
                {
                    maximumTimeoutMilliseconds = provider.TimeoutMilliseconds;
                }

                if (!provider.Trigger.HasFlag(feedbackContext.Trigger))
                {
                    continue;
                }

                if (provider is GeneralCommandErrorFeedback)
                {
                    // This built-in feedback provider needs to run on the target Runspace.
                    generalFeedback = provider;
                    continue;
                }

                if (tasks is null)
                {
                    tasks = new List<Task<FeedbackResult?>>(capacity: count);
                    cancellationSource = new CancellationTokenSource();
                    callBack = GetCallBack(feedbackContext, cancellationSource);
                }

                // Other feedback providers will run on background threads in parallel.
                tasks.Add(Task.Factory.StartNew(
                    callBack!,
                    provider,
                    cancellationSource!.Token,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default));
            }

            Task<Task>? waitTask = null;
            if (tasks is not null)
            {
                waitTask = Task.WhenAny(
                    Task.WhenAll(tasks),
                    Task.Delay(maximumTimeoutMilliseconds, cancellationSource!.Token));
            }

            List<FeedbackResult>? resultList = null;
            if (generalFeedback is not null)
            {
                FeedbackResult? builtInResult = GetBuiltInFeedback(generalFeedback, localRunspace, feedbackContext, questionMarkValue);
                if (builtInResult is not null)
                {
                    resultList ??= new List<FeedbackResult>(count);
                    resultList.Add(builtInResult);
                }
            }

            if (waitTask is not null)
            {
                try
                {
                    waitTask.Wait();
                    cancellationSource!.Cancel();

                    foreach (Task<FeedbackResult?> task in tasks!)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            FeedbackResult? result = task.Result;
                            if (result is not null)
                            {
                                resultList ??= new List<FeedbackResult>(count);
                                resultList.Add(result);
                            }
                        }
                    }
                }
                finally
                {
                    cancellationSource!.Dispose();
                }
            }

            return resultList;
        }

        private static bool CanSkip(IEnumerable<IFeedbackProvider> providers)
        {
            const FeedbackTrigger possibleTriggerOnSuccess = FeedbackTrigger.Success | FeedbackTrigger.Comment;

            bool canSkip = true;
            foreach (IFeedbackProvider provider in providers)
            {
                if ((provider.Trigger & possibleTriggerOnSuccess) != 0)
                {
                    canSkip = false;
                    break;
                }
            }

            return canSkip;
        }

        private static FeedbackResult? GetBuiltInFeedback(
            IFeedbackProvider builtInFeedback,
            LocalRunspace localRunspace,
            FeedbackContext feedbackContext,
            bool questionMarkValue)
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

                FeedbackItem? item = builtInFeedback.GetFeedback(feedbackContext, CancellationToken.None);
                if (item is not null)
                {
                    return new FeedbackResult(builtInFeedback.Id, builtInFeedback.Name, item);
                }
            }
            finally
            {
                if (changedDefault)
                {
                    Runspace.DefaultRunspace = oldDefault;
                }

                // Restore $? for the target Runspace.
                localRunspace.ExecutionContext.QuestionMarkVariableValue = questionMarkValue;
            }

            return null;
        }

        private static bool TryGetFeedbackContext(
            ExecutionContext executionContext,
            bool questionMarkValue,
            HistoryInfo lastHistory,
            [NotNullWhen(true)] out FeedbackContext? feedbackContext)
        {
            feedbackContext = null;
            Ast ast = Parser.ParseInput(lastHistory.CommandLine, out Token[] tokens, out _);

            FeedbackTrigger trigger;
            ErrorRecord? lastError = null;

            if (IsPureComment(tokens))
            {
                trigger = FeedbackTrigger.Comment;
            }
            else if (questionMarkValue)
            {
                trigger = FeedbackTrigger.Success;
            }
            else if (TryGetLastError(executionContext, lastHistory, out lastError))
            {
                trigger = lastError.FullyQualifiedErrorId is "CommandNotFoundException"
                    ? FeedbackTrigger.CommandNotFound
                    : FeedbackTrigger.Error;
            }
            else
            {
                return false;
            }

            PathInfo cwd = executionContext.SessionState.Path.CurrentLocation;
            feedbackContext = new(trigger, ast, tokens, cwd, lastError);
            return true;
        }

        private static bool IsPureComment(Token[] tokens)
        {
            return tokens.Length is 2 && tokens[0].Kind is TokenKind.Comment && tokens[1].Kind is TokenKind.EndOfInput;
        }

        private static bool TryGetLastError(ExecutionContext context, HistoryInfo lastHistory, [NotNullWhen(true)] out ErrorRecord? lastError)
        {
            lastError = null;
            ArrayList errorList = (ArrayList)context.DollarErrorVariable;
            if (errorList.Count == 0)
            {
                return false;
            }

            lastError = errorList[0] as ErrorRecord;
            if (lastError is null && errorList[0] is RuntimeException rtEx)
            {
                lastError = rtEx.ErrorRecord;
            }

            if (lastError?.InvocationInfo is null || lastError.InvocationInfo.HistoryId != lastHistory.Id)
            {
                return false;
            }

            return true;
        }

        // A local helper function to avoid creating an instance of the generated delegate helper class
        // when no feedback provider is registered.
        private static Func<object?, FeedbackResult?> GetCallBack(
            FeedbackContext feedbackContext,
            CancellationTokenSource cancellationSource)
        {
            return state =>
            {
                var provider = (IFeedbackProvider)state!;
                var item = provider.GetFeedback(feedbackContext, cancellationSource.Token);
                return item is null ? null : new FeedbackResult(provider.Id, provider.Name, item);
            };
        }
    }
}
