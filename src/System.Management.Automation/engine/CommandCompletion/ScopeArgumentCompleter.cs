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
    public sealed class ScopeArgumentCompleter : IArgumentCompleter
    {
        private static readonly string[] s_Scopes = new string[] { "Global", "Local", "Script" };

        /// <summary>
        /// Gets all possible Scope completion values.
        /// </summary>
        public IEnumerable<string> PossibleCompletionValues => s_Scopes;
    }
}
