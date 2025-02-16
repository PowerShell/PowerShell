// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Base class for writing custom Argument Completers.
    /// </summary>
    public abstract class ArgumentCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        /// <value>
        /// The name of the command as a <see cref="string"/>.
        /// </value>
        protected string CommandName { get; private set; }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        /// <value>
        /// The name of the parameter as a <see cref="string"/>.
        /// </value>
        protected string ParameterName { get; private set; }

        /// <summary>
        /// Gets the word to complete.
        /// </summary>
        /// <value>
        /// The word to complete as a <see cref="string"/>.
        /// </value>
        protected string WordToComplete { get; private set; }

        /// <summary>
        /// Gets the command AST.
        /// </summary>
        /// <value>
        /// The command AST as a <see cref="Language.CommandAst"/>.
        /// </value>
        protected CommandAst CommandAst { get; private set; }

        /// <summary>
        /// Gets the fake bound parameters.
        /// </summary>
        /// <value>
        /// An <see cref="IDictionary"/> that contains the fake bound parameters.
        /// </value>
        protected IDictionary FakeBoundParameters { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform completion.
        /// </summary>
        /// <value>
        /// Indicates completion should be performed as a <see cref="bool"/>.
        /// </value>
        protected virtual bool ShouldComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the type of the completion result.
        /// </summary>
        /// <value>
        /// The type of the completion result as a <see cref="Automation.CompletionResultType"/>.
        /// </value>
        protected virtual CompletionResultType CompletionResultType { get; set; } = CompletionResultType.Text;

        /// <summary>
        /// Gets or sets the tool tip mapping delegate.
        /// </summary>
        /// <value>
        /// A function that maps a <see cref="string"/> to a <see cref="string"/> for tool tips.
        /// </value>
        protected virtual Func<string, string> ToolTipMapping { get; set; }

        /// <summary>
        /// Get possible completion values.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> containing the possible completion values.
        /// </returns>
        protected abstract IEnumerable<string> GetPossibleCompletionValues();

        /// <summary>
        /// Completes the argument using matching results from the word to complete.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command abstract syntax tree.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>
        /// An <see cref="IEnumerable"/> containing the completion results.
        /// </returns>
        public virtual IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            CommandName = commandName;
            ParameterName = parameterName;
            WordToComplete = wordToComplete;
            CommandAst = commandAst;
            FakeBoundParameters = fakeBoundParameters;
            return ShouldComplete ? GetMatchingResults(wordToComplete) : [];
        }

        private IEnumerable<CompletionResult> GetMatchingResults(string wordToComplete)
        {
            string quote = CompletionCompleters.HandleDoubleAndSingleQuote(ref wordToComplete);
            var pattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (string value in GetPossibleCompletionValues())
            {
                if (pattern.IsMatch(value))
                {
                    string completionText = quote == string.Empty
                        ? value
                        : quote + value + quote;

                    string listItemText = value;

                    yield return new CompletionResult(
                        completionText,
                        listItemText,
                        resultType: CompletionResultType,
                        toolTip: ToolTipMapping?.Invoke(value) ?? listItemText);
                }
            }
        }
    }
}
