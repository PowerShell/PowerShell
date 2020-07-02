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
        /// Collect the predictive suggestions from registered predictors.
        /// </summary>
        public static async Task<List<PredictionResult>> PredictInput(Ast ast, Token[] astTokens, int millisecondsTimeout = 20)
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
                        var predictor = (IPredictor) state;
                        List<string> texts = predictor.GetSuggestion(context, cancellationToken);
                        return texts?.Count > 0 ? new PredictionResult(predictor.Id, texts) : null;
                    }, predictor);
            }

            try
            {
                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(millisecondsTimeout, cancellationToken));
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
