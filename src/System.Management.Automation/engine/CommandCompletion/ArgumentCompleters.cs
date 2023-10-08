// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides argument completion for Scope parameter.
    /// </summary>
    public class ScopeArgumentCompleter : IArgumentCompleter
    {
        private readonly string[] scopes = new string[] { "Global", "Local", "Script" };

        /// <summary>
        /// Returns completion results for verb parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var scopePattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (string scope in scopes)
            {
                if (scopePattern.IsMatch(scope))
                {
                    yield return new CompletionResult(scope);
                }
            }
        }
    }
}
