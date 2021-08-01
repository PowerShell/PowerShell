// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Management.Automation;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// HttpVersionCompleter for http version names.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated through late-bound reflection")]
    internal sealed class HttpVersionCompleter : IArgumentCompleter
    {
        /// <inheritdoc/>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var wordToCompletePattern = WildcardPattern.Get(string.IsNullOrWhiteSpace(wordToComplete) ? "*" : wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (var version in HttpVersionUtils.AllowedVersions)
            {
                if (wordToCompletePattern.IsMatch(version))
                {
                    yield return new CompletionResult(version, version, CompletionResultType.Text, version);
                }
            }
        }
    }
}
