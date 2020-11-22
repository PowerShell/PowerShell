// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace System.Management.Automation.Subsystem
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
        /// Gets the suggestions.
        /// </summary>
        public IReadOnlyList<PredictiveSuggestion> Suggestions { get; }

        internal PredictionResult(Guid id, string name, List<PredictiveSuggestion> suggestions)
        {
            Id = id;
            Name = name;
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
        /// <param name="ast">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="astTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        /// <returns>A list of <see cref="PredictionResult"/> objects.</returns>
        public static Task<List<PredictionResult>?> PredictInput(Ast ast, Token[] astTokens)
        {
            return PredictInput(ast, astTokens, millisecondsTimeout: 20);
        }

        /// <summary>
        /// Collect the predictive suggestions from registered predictors using the specified timeout.
        /// </summary>
        /// <param name="ast">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="astTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        /// <param name="millisecondsTimeout">The milliseconds to timeout.</param>
        /// <returns>A list of <see cref="PredictionResult"/> objects.</returns>
        public static async Task<List<PredictionResult>?> PredictInput(Ast ast, Token[] astTokens, int millisecondsTimeout)
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

            for (int i = 0; i < predictors.Count; i++)
            {
                ICommandPredictor predictor = predictors[i];

                tasks[i] = Task.Factory.StartNew(
                    state =>
                    {
                        var predictor = (ICommandPredictor)state!;
                        List<PredictiveSuggestion>? texts = predictor.GetSuggestion(context, cancellationSource.Token);
                        return texts?.Count > 0 ? new PredictionResult(predictor.Id, predictor.Name, texts) : null;
                    },
                    predictor,
                    cancellationSource.Token,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            await Task.WhenAny(
                Task.WhenAll(tasks),
                Task.Delay(millisecondsTimeout, cancellationSource.Token)).ConfigureAwait(false);
            cancellationSource.Cancel();

            var results = new List<PredictionResult>(predictors.Count);
            foreach (Task<PredictionResult?> task in tasks)
            {
                if (task.IsCompletedSuccessfully)
                {
                    PredictionResult? result = task.Result;
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Allow registered predictors to do early processing when a command line is accepted.
        /// </summary>
        /// <param name="history">History command lines provided as references for prediction.</param>
        public static void OnCommandLineAccepted(IReadOnlyList<string> history)
        {
            Requires.NotNull(history, nameof(history));

            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.SupportEarlyProcessing)
                {
                    ThreadPool.QueueUserWorkItem<ICommandPredictor>(
                        state => state.StartEarlyProcessing(history),
                        predictor,
                        preferLocal: false);
                }
            }
        }

        /// <summary>
        /// Send feedback to predictors about their last suggestions.
        /// </summary>
        /// <param name="predictorId">The identifier of the predictor whose prediction result was accepted.</param>
        /// <param name="suggestionText">The accepted suggestion text.</param>
        public static void OnSuggestionAccepted(Guid predictorId, string suggestionText)
        {
            Requires.NotNullOrEmpty(suggestionText, nameof(suggestionText));

            var predictors = SubsystemManager.GetSubsystems<ICommandPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (ICommandPredictor predictor in predictors)
            {
                if (predictor.AcceptFeedback && predictor.Id == predictorId)
                {
                    ThreadPool.QueueUserWorkItem<ICommandPredictor>(
                        state => state.OnSuggestionAccepted(suggestionText),
                        predictor,
                        preferLocal: false);
                }
            }
        }
    }
}
