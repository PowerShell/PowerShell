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
        /// Gets the command abstract syntax tree (AST).
        /// </summary>
        /// <value>
        /// The command AST as a <see cref="CommandAst"/>.
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
        /// True if completion should be performed; otherwise, false.
        /// </value>
        protected virtual bool ShouldComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the type of the completion result.
        /// </summary>
        /// <value>
        /// The type of the completion result as a <see cref="CompletionResultType"/>.
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
        /// Gets or sets a flag to escape globbing paths.
        /// </summary>
        /// <value>
        /// True if globbing paths need to be escaped; otherwise, false.
        /// </value>
        protected virtual bool EscapeGlobbingPath { get; set; }

        /// <summary>
        /// Gets possible completion values.
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
        /// An <see cref="IEnumerable{CompletionResult}"/> containing the completion results.
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
            return ShouldComplete ? GetMatchingResults(wordToComplete) : new List<CompletionResult>();
        }

        /// <summary>
        /// Matches the word to complete against a value.
        /// </summary>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="value">The value to match.</param>
        /// <returns>
        /// True if the value was matched; otherwise, false.
        /// </returns>
        protected virtual bool IsMatch(string wordToComplete, string value)
            => WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase).IsMatch(value);

        /// <summary>
        /// Matches the possible completion values against the word to complete.
        /// </summary>
        /// <param name="wordToComplete">The word to complete, which is used as a pattern for matching possible values.</param>
        /// <returns>
        /// An <see cref="IEnumerable{CompletionResult}"/> containing the matching completion results.
        /// </returns>
        /// <remarks>
        /// This method handles different variations of completions, including considerations for quotes and escaping globbing paths.
        /// The <see cref="IsMatch(string, string)"/> method is used to determine if a possible value matches the word to complete.
        /// </remarks>
        private IEnumerable<CompletionResult> GetMatchingResults(string wordToComplete)
        {
            string quote = CompletionCompleters.HandleDoubleAndSingleQuote(ref wordToComplete);

            foreach (string value in GetPossibleCompletionValues())
            {
                if (IsMatch(wordToComplete, value))
                {
                    string completionText = QuoteCompletionText(value, quote, EscapeGlobbingPath);

                    string listItemText = value;

                    yield return new CompletionResult(
                        completionText,
                        listItemText,
                        resultType: CompletionResultType,
                        toolTip: ToolTipMapping?.Invoke(value) ?? listItemText);
                }
            }
        }

        /// <summary>
        /// Quotes the completion text.
        /// </summary>
        /// <param name="quote">The quote to use.</param>
        /// <param name="completionText">The text to complete.</param>
        /// <param name="escapeGlobbingPath">True if the globbing path needs to be escaped; otherwise, false.</param>
        /// <returns>
        /// A quoted string if quoting is necessary.
        /// </returns>
        private static string QuoteCompletionText(string quote, string completionText, bool escapeGlobbingPath)
        {
            if (CompletionCompleters.CompletionRequiresQuotes(completionText, escapeGlobbingPath))
            {
                string quoteInUse = quote == string.Empty ? "'" : quote;

                if (quoteInUse == "'")
                {
                    completionText = completionText.Replace("'", "''");
                }
                else
                {
                    completionText = completionText.Replace("`", "``");
                    completionText = completionText.Replace("$", "`$");
                }

                if (escapeGlobbingPath)
                {
                    if (quoteInUse == "'")
                    {
                        completionText = completionText.Replace("[", "`[");
                        completionText = completionText.Replace("]", "`]");
                    }
                    else
                    {
                        completionText = completionText.Replace("[", "``[");
                        completionText = completionText.Replace("]", "``]");
                    }
                }

                completionText = quoteInUse + completionText + quoteInUse;
            }
            else
            {
                completionText = quote + completionText + quote;
            }

            return completionText;
        }
    }
}
