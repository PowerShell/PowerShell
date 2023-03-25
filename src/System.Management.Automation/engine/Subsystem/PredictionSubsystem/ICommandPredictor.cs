// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Threading;

namespace System.Management.Automation.Subsystem.Prediction
{
    /// <summary>
    /// Interface for implementing a predictor plugin.
    /// </summary>
    public interface ICommandPredictor : ISubsystem
    {
        /// <summary>
        /// Default implementation. No function is required for a predictor.
        /// </summary>
        Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;

        /// <summary>
        /// Get the predictive suggestions. It indicates the start of a suggestion rendering session.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="context">The <see cref="PredictionContext"/> object to be used for prediction.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the prediction.</param>
        /// <returns>An instance of <see cref="SuggestionPackage"/>.</returns>
        SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a value indicating whether the predictor accepts a specific kind of feedback.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="feedback">A specific type of feedback.</param>
        /// <returns>True or false, to indicate whether the specific feedback is accepted.</returns>
        bool CanAcceptFeedback(PredictionClient client, PredictorFeedbackKind feedback) => false;

        /// <summary>
        /// One or more suggestions provided by the predictor were displayed to the user.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="session">The mini-session where the displayed suggestions came from.</param>
        /// <param name="countOrIndex">
        /// When the value is greater than 0, it's the number of displayed suggestions from the list returned in <paramref name="session"/>, starting from the index 0.
        /// When the value is less than or equal to 0, it means a single suggestion from the list got displayed, and the index is the absolute value.
        /// </param>
        void OnSuggestionDisplayed(PredictionClient client, uint session, int countOrIndex) { }

        /// <summary>
        /// The suggestion provided by the predictor was accepted.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="session">Represents the mini-session where the accepted suggestion came from.</param>
        /// <param name="acceptedSuggestion">The accepted suggestion text.</param>
        void OnSuggestionAccepted(PredictionClient client, uint session, string acceptedSuggestion) { }

        /// <summary>
        /// A command line was accepted to execute.
        /// The predictor can start processing early as needed with the latest history.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="history">History command lines provided as references for prediction.</param>
        void OnCommandLineAccepted(PredictionClient client, IReadOnlyList<string> history) { }

        /// <summary>
        /// A command line was done execution.
        /// </summary>
        /// <param name="client">Represents the client that initiates the call.</param>
        /// <param name="commandLine">The last accepted command line.</param>
        /// <param name="success">Shows whether the execution was successful.</param>
        void OnCommandLineExecuted(PredictionClient client, string commandLine, bool success) { }
    }

    /// <summary>
    /// Kinds of feedback a predictor can choose to accept.
    /// </summary>
    public enum PredictorFeedbackKind
    {
        /// <summary>
        /// Feedback when one or more suggestions are displayed to the user.
        /// </summary>
        SuggestionDisplayed,

        /// <summary>
        /// Feedback when a suggestion is accepted by the user.
        /// </summary>
        SuggestionAccepted,

        /// <summary>
        /// Feedback when a command line is accepted by the user.
        /// </summary>
        CommandLineAccepted,

        /// <summary>
        /// Feedback when the accepted command line finishes its execution.
        /// </summary>
        CommandLineExecuted,
    }

    /// <summary>
    /// Kinds of prediction clients.
    /// </summary>
    public enum PredictionClientKind
    {
        /// <summary>
        /// A terminal client, representing the command-line experience.
        /// </summary>
        Terminal,

        /// <summary>
        /// An editor client, representing the editor experience.
        /// </summary>
        Editor,
    }

    /// <summary>
    /// The class represents a client that interacts with predictors.
    /// </summary>
    public sealed class PredictionClient
    {
        /// <summary>
        /// Gets the client name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the client kind.
        /// </summary>
        public PredictionClientKind Kind { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionClient"/> class.
        /// </summary>
        /// <param name="name">Name of the interactive client.</param>
        /// <param name="kind">Kind of the interactive client.</param>
        public PredictionClient(string name, PredictionClientKind kind)
        {
            Name = name;
            Kind = kind;
        }
    }

    /// <summary>
    /// Context information about the user input.
    /// </summary>
    public sealed class PredictionContext
    {
        /// <summary>
        /// Gets the abstract syntax tree (AST) generated from parsing the user input.
        /// </summary>
        public Ast InputAst { get; }

        /// <summary>
        /// Gets the tokens generated from parsing the user input.
        /// </summary>
        public IReadOnlyList<Token> InputTokens { get; }

        /// <summary>
        /// Gets the cursor position, which is assumed always at the end of the input line.
        /// </summary>
        public IScriptPosition CursorPosition { get; }

        /// <summary>
        /// Gets the token at the cursor.
        /// </summary>
        public Token? TokenAtCursor { get; }

        /// <summary>
        /// Gets all ASTs that are related to the cursor position,
        /// which is assumed always at the end of the input line.
        /// </summary>
        public IReadOnlyList<Ast> RelatedAsts { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictionContext"/> class from the AST and tokens that represent the user input.
        /// </summary>
        /// <param name="inputAst">The <see cref="Ast"/> object from parsing the current command line input.</param>
        /// <param name="inputTokens">The <see cref="Token"/> objects from parsing the current command line input.</param>
        public PredictionContext(Ast inputAst, Token[] inputTokens)
        {
            ArgumentNullException.ThrowIfNull(inputAst);
            ArgumentNullException.ThrowIfNull(inputTokens);

            var cursor = inputAst.Extent.EndScriptPosition;
            var astContext = CompletionAnalysis.ExtractAstContext(inputAst, inputTokens, cursor);

            InputAst = inputAst;
            InputTokens = inputTokens;
            CursorPosition = cursor;
            TokenAtCursor = astContext.TokenAtCursor;
            RelatedAsts = astContext.RelatedAsts;
        }

        /// <summary>
        /// Creates a context instance from the user input line.
        /// </summary>
        /// <param name="input">The user input.</param>
        /// <returns>A <see cref="PredictionContext"/> object.</returns>
        public static PredictionContext Create(string input)
        {
            ArgumentException.ThrowIfNullOrEmpty(input);

            Ast ast = Parser.ParseInput(input, out Token[] tokens, out _);
            return new PredictionContext(ast, tokens);
        }
    }

    /// <summary>
    /// The class represents a predictive suggestion generated by a predictor.
    /// </summary>
    public sealed class PredictiveSuggestion
    {
        /// <summary>
        /// Gets the suggestion.
        /// </summary>
        public string SuggestionText { get; }

        /// <summary>
        /// Gets the tooltip of the suggestion.
        /// </summary>
        public string? ToolTip { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictiveSuggestion"/> class.
        /// </summary>
        /// <param name="suggestion">The predictive suggestion text.</param>
        public PredictiveSuggestion(string suggestion)
            : this(suggestion, toolTip: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredictiveSuggestion"/> class.
        /// </summary>
        /// <param name="suggestion">The predictive suggestion text.</param>
        /// <param name="toolTip">The tooltip of the suggestion.</param>
        public PredictiveSuggestion(string suggestion, string? toolTip)
        {
            ArgumentException.ThrowIfNullOrEmpty(suggestion);

            SuggestionText = suggestion;
            ToolTip = toolTip;
        }
    }

    /// <summary>
    /// A package returned from <see cref="ICommandPredictor.GetSuggestion"/>.
    /// </summary>
    public struct SuggestionPackage
    {
        /// <summary>
        /// Gets the mini-session that represents a specific invocation to <see cref="ICommandPredictor.GetSuggestion"/>.
        /// When it's not specified, it's considered by a client that the predictor doesn't expect feedback.
        /// </summary>
        public uint? Session { get; }

        /// <summary>
        /// Gets the suggestion entries returned from that mini-session.
        /// </summary>
        public List<PredictiveSuggestion>? SuggestionEntries { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SuggestionPackage"/> struct without providing a session id.
        /// Note that, when a session id is not specified, it's considered by a client that the predictor doesn't expect feedback.
        /// </summary>
        /// <param name="suggestionEntries">The suggestions to return.</param>
        public SuggestionPackage(List<PredictiveSuggestion> suggestionEntries)
        {
            Requires.NotNullOrEmpty(suggestionEntries, nameof(suggestionEntries));

            Session = null;
            SuggestionEntries = suggestionEntries;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SuggestionPackage"/> struct with the mini-session id and the suggestions.
        /// </summary>
        /// <param name="session">The mini-session where suggestions came from.</param>
        /// <param name="suggestionEntries">The suggestions to return.</param>
        public SuggestionPackage(uint session, List<PredictiveSuggestion> suggestionEntries)
        {
            Requires.NotNullOrEmpty(suggestionEntries, nameof(suggestionEntries));

            Session = session;
            SuggestionEntries = suggestionEntries;
        }
    }
}
