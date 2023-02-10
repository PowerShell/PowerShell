// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerShell.Commands;
using static System.Net.Mime.MediaTypeNames;

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
            int count = providers.Count;
            if (count == 0)
            {
                return null;
            }

            IFeedbackProvider? generalFeedback = null;
            List<Task<FeedbackResult?>>? tasks = null;
            CancellationTokenSource? cancellationSource = null;
            Func<object?, FeedbackResult?>? callBack = null;

            for (int i = 0; i < providers.Count; i++)
            {
                IFeedbackProvider provider = providers[i];
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
                    callBack = GetCallBack(lastHistory.CommandLine, lastError, cancellationSource);
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
                    Task.Delay(millisecondsTimeout, cancellationSource!.Token));
            }

            List<FeedbackResult>? resultList = null;
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

                    FeedbackItem? item = generalFeedback.GetFeedback(lastHistory.CommandLine, lastError, CancellationToken.None);
                    if (item is not null)
                    {
                        resultList ??= new List<FeedbackResult>(count);
                        resultList.Add(new FeedbackResult(generalFeedback.Id, generalFeedback.Name, item));
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

        // A local helper function to avoid creating an instance of the generated delegate helper class
        // when no feedback provider is registered.
        private static Func<object?, FeedbackResult?> GetCallBack(
            string commandLine,
            ErrorRecord lastError,
            CancellationTokenSource cancellationSource)
        {
            return state =>
            {
                var provider = (IFeedbackProvider)state!;
                var item = provider.GetFeedback(commandLine, lastError, cancellationSource.Token);
                return item is null ? null : new FeedbackResult(provider.Id, provider.Name, item);
            };
        }
    }
}
