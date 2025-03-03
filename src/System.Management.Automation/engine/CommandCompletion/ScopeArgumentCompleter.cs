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
        private static readonly string[] s_Scopes = new string[] { "Global", "Local", "Script" };

        /// <summary>
        /// Configures argument completer options.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <returns>Configured options.</returns>
        public ArgumentCompleterOptions ConfigureArgumentCompleterOptions(ArgumentCompleterOptions options)
        {
            options.PossibleCompletionValues = s_Scopes;
            return options;
        }
    }
}
