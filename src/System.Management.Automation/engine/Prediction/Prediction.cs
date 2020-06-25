// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Interface for implementing a predictor plugin.
    /// </summary>
    public interface IPredictor : ISubsystem
    {
        /// <summary>
        /// Default implementation. No function is required for a predictor.
        /// </summary>
        ReadOnlyDictionary<string, string> ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// Default implementation for `ISubsystem.Kind`.
        /// </summary>
        SubsystemKind ISubsystem.Kind => SubsystemKind.CommandPredictor;

        /// <summary>
        /// Gets whether the predictor supports early processing.
        /// </summary>
        bool SupportEarlyProcessing { get; }

        /// <summary>
        /// Gets whether the predictor accepts feedback about the previous suggestion.
        /// </summary>
        bool AcceptFeedback { get; }

        /// <summary>
        /// A command line was accepted to execute.
        /// The predictor can start processing early as needed with the latest history.
        /// </summary>
        void EarlyProcessWithHistory(IReadOnlyList<string> history);

        /// <summary>
        /// The suggestion given by the predictor was accepted.
        /// </summary>
        void LastSuggestionAccepted(string acceptedSuggestion);

        /// <summary>
        /// The suggestion given by the predictor was denied.
        /// </summary>
        void LastSuggestionDenied();

        /// <summary>
        /// Get the predictive suggestions.
        /// </summary>
        List<string> GetSuggestion(string userInput, CancellationToken token);

        /// <summary>
        /// Get the predictive suggestions.
        /// </summary>
        List<string> GetSuggestion(Ast ast, CancellationToken token);
    }

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
        public static Task<List<PredictionResult>> PredictInput(Ast ast, int millisecondsTimeout = 50)
        {
            return PredictInputImpl(ast, userInput: null, millisecondsTimeout);
        }

        /// <summary>
        /// Collect the predictive suggestions from registered predictors.
        /// </summary>
        public static Task<List<PredictionResult>> PredictInput(string userInput, int millisecondsTimeout)
        {
            return PredictInputImpl(ast: null, userInput, millisecondsTimeout);
        }

        private static async Task<List<PredictionResult>> PredictInputImpl(Ast ast, string userInput, int millisecondsTimeout)
        {
            var predictors = SubsystemManager.GetSubsystems<IPredictor>();
            if (predictors.Count == 0)
            {
                return null;
            }

            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            var tasks = new Task<PredictionResult>[predictors.Count];

            for (int i = 0; i < predictors.Count; i++)
            {
                var predictor = predictors[i];
                tasks[i] = Task.Factory.StartNew(
                    state => {
                        var predictor = (IPredictor) state;
                        List<string> texts = ast != null
                            ? predictor.GetSuggestion(ast, token)
                            : predictor.GetSuggestion(userInput, token);

                        return texts?.Count > 0 ? new PredictionResult(predictor.Id, texts) : null;
                    }, predictor);
            }

            try
            {
                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(millisecondsTimeout, token));
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
