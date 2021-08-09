// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A completer for HTTP version names.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated through late-bound reflection")]
    internal sealed class HttpVersionCompleter : IArgumentCompleter
    {
        /// <inheritdoc/>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var valueToComplete = wordToComplete.Trim('\'');

            var wordToCompletePattern = WildcardPattern.Get(string.IsNullOrWhiteSpace(valueToComplete) ? "*" : valueToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (var version in HttpVersionUtils.AllowedVersions)
            {
                if (wordToCompletePattern.IsMatch(version))
                {
                    var quotedVersion = Quoted(version);
                    yield return new CompletionResult(quotedVersion, quotedVersion, CompletionResultType.ParameterValue, quotedVersion);
                }
            }
        }

        private static string Quoted(string version) => "'" + version + "'";
    }
}
