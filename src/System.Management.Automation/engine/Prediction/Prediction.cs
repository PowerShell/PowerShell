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
        List<string> GetSuggestion(PredictionContext context, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Context information about the user input.
    /// </summary>
    public class PredictionContext
    {
        private PredictionContext() { }

        /// <summary>
        /// Gets the abstract syntax tree (AST) generated from parsing the user input.
        /// </summary>
        public Ast InputAst { get; private set; }

        /// <summary>
        /// Gets the tokens generated from parsing the user input.
        /// </summary>
        public Token[] InputTokens { get; private set; }

        /// <summary>
        /// Gets the cursor position, which is assumed always at the end of the input line.
        /// </summary>
        public IScriptPosition CursorPosition { get; private set; }

        /// <summary>
        /// Gets the token at the cursor.
        /// </summary>
        public Token TokenAtCursor { get; private set; }

        /// <summary>
        /// Gets all ASTs that are related to the cursor position,
        /// which is assumed always at the end of the input line.
        /// </summary>
        public IReadOnlyList<Ast> RelatedAsts { get; private set; }

        /// <summary>
        /// Creates a context instance from the user input line.
        /// </summary>
        public static PredictionContext Create(string input)
        {
            Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
            return Create(ast, tokens);
        }

        /// <summary>
        /// Creates a context instance from the AST and tokens that represent the user input.
        /// </summary>
        public static PredictionContext Create(Ast inputAst, Token[] inputTokens)
        {
            var cursor = inputAst.Extent.EndScriptPosition;
            var astContext = CompletionAnalysis.ExtractAstContext(inputAst, inputTokens, cursor);
            return new PredictionContext() {
                InputAst = inputAst,
                InputTokens = inputTokens,
                CursorPosition = cursor,
                TokenAtCursor = astContext.TokenAtCursor,
                RelatedAsts = astContext.RelatedAsts,
            };
        }
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
        public static async Task<List<PredictionResult>> PredictInput(Ast ast, Token[] astTokens, int millisecondsTimeout = 20)
        {
            var predictors = SubsystemManager.GetSubsystems<IPredictor>();
            if (predictors.Count == 0)
            {
                return null;
            }

            var context = PredictionContext.Create(ast, astTokens);
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
