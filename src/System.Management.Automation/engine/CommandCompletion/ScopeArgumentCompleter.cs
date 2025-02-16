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
    public sealed class ScopeArgumentCompleter : ArgumentCompleter
    {
        /// <summary>
        /// Gets possible completion values for Scope parameter.
        /// </summary>
        /// <returns>List of possible completion values.</returns>
        protected override IEnumerable<string> GetPossibleCompletionValues() => ["Global", "Local", "Script"];
    }
}
