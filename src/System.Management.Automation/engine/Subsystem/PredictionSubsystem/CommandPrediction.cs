// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace System.Management.Automation.Subsystem.Prediction
{
    /// <summary>
    /// The class represents the prediction result from a predictor.
    /// </summary>
    public sealed class PredictionResult
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
        /// Gets the mini-session id that represents a specific invocation to the <see cref="ICommandPredictor.GetSuggestion"/> API of the predictor.
        /// When it's not specified, it's considered by a client that the predictor doesn't expect feedback.
        /// </summary>
        public uint? Session { get; }

        /// <summary>
        /// Gets the suggestions.
        /// </summary>
        public IReadOnlyList<PredictiveSuggestion> Suggestions { get; }

        internal PredictionResult(Guid id, string name, uint? session, List<PredictiveSuggestion> suggestions)
        {
            Id = id;
            Name = name;
            Session = session;
            Suggestions = suggestions;
        }
    }

    /// <summary>
    /// Provides a set of possible predictions for given input.
    /// </summary>
    public static class CommandPrediction
    {
        /// <summary>
        /// Collect the predictive suggestions from registered predictors using the default timeout.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="ast">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="astTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        /// <returns>A list of <see cref="PredictionResult"/> objects.</returns>
        public static Task<List<PredictionResult>?> PredictInputAsync(PredictionClient client, Ast ast, Token[] astTokens)
        {
            return PredictInputAsync(client, ast, astTokens, millisecondsTimeout: 20);
        }

        /// <summary>
        /// Collect the predictive suggestions from registered predictors using the specified timeout.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="ast">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="astTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        /// <param name="millisecondsTimeout">The milliseconds to timeout.</param>
        /// <returns>A list of <see cref="PredictionResult"/> objects.</returns>
        public static async Task<List<PredictionResult>?> PredictInputAsync(PredictionClient client, Ast ast, Token[] astTokens, int millisecondsTimeout)
        {
            Requires.Condition(millisecondsTimeout > 0, nameof(millisecondsTimeout));

            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return null;
            }

            var context = new PredictionContext(ast, astTokens);
            var tasks = new Task<PredictionResult?>[predictors.Count];
            using var cancellationSource = new CancellationTokenSource();

            Func<object?, PredictionResult?> callBack = GetCallBack(client, context, cancellationSource);

            for (int i = 0; i < predictors.Count; i++)
            {
                ICommandPredictor predictor = predictors[i];
                tasks[i] = Task.Factory.StartNew(
                    callBack,
                    predictor,
                    cancellationSource.Token,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            await Task.WhenAny(
                Task.WhenAll(tasks),
                Task.Delay(millisecondsTimeout, cancellationSource.Token)).ConfigureAwait(false);
            cancellationSource.Cancel();

            var resultList = new List<PredictionResult>(predictors.Count);
            foreach (Task<PredictionResult?> task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    PredictionResult? result = task.Result;
                    if (result != null)
                    {
                        resultList.Add(result);
                    }
                }
            }

            return resultList;

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered.
            static Func<object?, PredictionResult?> GetCallBack(
                PredictionClient client,
                PredictionContext context,
                CancellationTokenSource cancellationSource)
            {
                return state =>
                {
                    var predictor = (ICommandPredictor)state!;
                    SuggestionPackage pkg = predictor.GetSuggestion(client, context, cancellationSource.Token);
                    return pkg.SuggestionEntries?.Count > 0 ? new PredictionResult(predictor.Id, predictor.Name, pkg.Session, pkg.SuggestionEntries) : null;
                };
            }
        }

        /// <summary>
        /// Allow registered predictors to do early processing when a command line is accepted.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="history">History command lines provided as references for prediction.</param>
        public static void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history)
        {
            ArgumentNullException.ThrowIfNull(history);

            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            Action<ICommandPredictor>? callBack = null;
            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.CanAcceptFeedback(client, PredictorFeedbackKind.CommandLineAccepted))
                {
                    callBack ??= GetCallBack(client, history);
                    ThreadPool.QueueUserWorkItem<ICommandPredictor>(callBack, predictor, preferLocal: false);
                }
            }

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered, or no registered predictor accepts this feedback.
            static Action<ICommandPredictor> GetCallBack(PredictionClient client, IReadOnlyList<string> history)
            {
                return predictor => predictor.OnCommandLineAccepted(client, history);
            }
        }

        /// <summary>
        /// Allow registered predictors to know the execution result (success/failure) of the last accepted command line.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="commandLine">The last accepted command line.</param>
        /// <param name="success">Whether the execution of the last command line was successful.</param>
        public static void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success)
        {
            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            Action<ICommandPredictor>? callBack = null;
            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.CanAcceptFeedback(client, PredictorFeedbackKind.CommandLineExecuted))
                {
                    callBack ??= GetCallBack(client, commandLine, success);
                    ThreadPool.QueueUserWorkItem<ICommandPredictor>(callBack, predictor, preferLocal: false);
                }
            }

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered, or no registered predictor accepts this feedback.
            static Action<ICommandPredictor> GetCallBack(PredictionClient client, string commandLine, bool success)
            {
                return predictor => predictor.OnCommandLineExecuted(client, commandLine, success);
            }
        }

        /// <summary>
        /// Send feedback to a predictor when one or more suggestions from it were displayed to the user.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="predictorId">The identifier of the predictor whose prediction result was accepted.</param>
        /// <param name="session">The mini-session where the displayed suggestions came from.</param>
        /// <param name="countOrIndex">
        /// When the value is greater than 0, it's the number of displayed suggestions from the list returned in <paramref name="session"/>, starting from the index 0.
        /// When the value is less than or equal to 0, it means a single suggestion from the list got displayed, and the index is the absolute value.
        /// </param>
        public static void OnSuggestionDisplayed(PredictionClient client, Guid predictorId, uint session, int countOrIndex)
        {
            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.Id == predictorId)
                {
                    if (predictor.CanAcceptFeedback(client, PredictorFeedbackKind.SuggestionDisplayed))
                    {
                        Action<ICommandPredictor> callBack = GetCallBack(client, session, countOrIndex);
                        ThreadPool.QueueUserWorkItem<ICommandPredictor>(callBack, predictor, preferLocal: false);
                    }

                    break;
                }
            }

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered, or no registered predictor accepts this feedback.
            static Action<ICommandPredictor> GetCallBack(PredictionClient client, uint session, int countOrIndex)
            {
                return predictor => predictor.OnSuggestionDisplayed(client, session, countOrIndex);
            }
        }

        /// <summary>
        /// Send feedback to a predictor when a suggestion from it was accepted.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="predictorId">The identifier of the predictor whose prediction result was accepted.</param>
        /// <param name="session">The mini-session where the accepted suggestion came from.</param>
        /// <param name="suggestionText">The accepted suggestion text.</param>
        public static void OnSuggestionAccepted(PredictionClient client, Guid predictorId, uint session, string suggestionText)
        {
            ArgumentException.ThrowIfNullOrEmpty(suggestionText);

            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.Id == predictorId)
                {
                    if (predictor.CanAcceptFeedback(client, PredictorFeedbackKind.SuggestionAccepted))
                    {
                        Action<ICommandPredictor> callBack = GetCallBack(client, session, suggestionText);
                        ThreadPool.QueueUserWorkItem<ICommandPredictor>(callBack, predictor, preferLocal: false);
                    }

                    break;
                }
            }

            // A local helper function to avoid creating an instance of the generated delegate helper class
            // when no predictor is registered, or no registered predictor accepts this feedback.
            static Action<ICommandPredictor> GetCallBack(PredictionClient client, uint session, string suggestionText)
            {
                return predictor => predictor.OnSuggestionAccepted(client, session, suggestionText);
            }
        }
    }
}
