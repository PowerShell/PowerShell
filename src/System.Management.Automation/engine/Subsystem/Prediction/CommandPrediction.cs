// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        /// Gets the suggestions.
        /// </summary>
        public List<string> Suggestions { get; }

        internal PredictionResult(Guid id, List<string> suggestions)
        {
            Id = id;
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
        public static Task<List<PredictionResult>> PredictInput(Ast ast, Token[] astTokens)
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
        public static async Task<List<PredictionResult>> PredictInput(Ast ast, Token[] astTokens, int millisecondsTimeout)
        {
            var predictors = SubsystemManager.GetSubsystems<IPredictor>();
            if (predictors.Count == 0)
            {
                return null;
            }

            var context = new PredictionContext(ast, astTokens);
            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
            var tasks = new Task<PredictionResult>[predictors.Count];

            for (int i = 0; i < predictors.Count; i++)
            {
                var predictor = predictors[i];

                tasks[i] = Task.Factory.StartNew(
                    state => {
                        var predictor = (IPredictor)state;
                        List<string> texts = predictor.GetSuggestion(context, cancellationToken);
                        return texts?.Count > 0 ? new PredictionResult(predictor.Id, texts) : null;
                    }, predictor);
            }

            try
            {
                await Task.WhenAny(
                    Task.WhenAll(tasks),
                    Task.Delay(millisecondsTimeout, cancellationToken)
                ).ConfigureAwait(false);
                cancellationSource.Cancel();

                var results = new List<PredictionResult>(predictors.Count);
                foreach (Task<PredictionResult> task in tasks)
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        var result = task.Result;
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }

                return results;
            }
            finally
            {
                cancellationSource.Dispose();
            }
        }

        /// <summary>
        /// Allow registered predictors to do early processing when a command line is accepted.
        /// </summary>
        /// <param name="history">History command lines provided as references for prediction.</param>
        public static void LineAccepted(IReadOnlyList<string> history)
        {
            var predictors = SubsystemManager.GetSubsystems<IPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (IPredictor predictor in predictors)
            {
                if (predictor.SupportEarlyProcessing)
                {
                    ThreadPool.QueueUserWorkItem(
                        state => ((IPredictor)state).EarlyProcessWithHistory(history),
                        predictor);
                }
            }
        }

        /// <summary>
        /// Send feedback to predictors about their last suggestions.
        /// </summary>
        /// <param name="predictorIds">Identifiers of the predictors from which we received prediction results.</param>
        /// <param name="acceptedId">The identifier of the predictor whose prediction result was accepted.</param>
        /// <param name="acceptedSuggestion">The accepted suggestion text.</param>
        public static void SuggestionFeedback(HashSet<Guid> predictorIds, Guid acceptedId, string acceptedSuggestion = null)
        {
            if (acceptedId != Guid.Empty && string.IsNullOrEmpty(acceptedSuggestion))
            {
                throw new ArgumentNullException(nameof(acceptedSuggestion));
            }

            var predictors = SubsystemManager.GetSubsystems<IPredictor>();
            if (predictors.Count == 0)
            {
                return;
            }

            foreach (IPredictor predictor in predictors)
            {
                if (!predictor.AcceptFeedback || !predictorIds.Contains(predictor.Id))
                {
                    continue;
                }

                if (predictor.Id == acceptedId)
                {
                    ThreadPool.QueueUserWorkItem(
                        state => ((IPredictor)state).LastSuggestionAccepted(acceptedSuggestion),
                        predictor);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(
                        state => ((IPredictor)state).LastSuggestionDenied(),
                        predictor);
                }
            }
        }
    }
}
