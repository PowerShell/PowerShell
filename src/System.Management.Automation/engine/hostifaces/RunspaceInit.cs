// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;
using DWORD = System.UInt32;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Runspace class for local runspace.
    /// </summary>
    internal sealed partial
    class LocalRunspace : RunspaceBase
    {
        /// <summary>
        /// Initialize default values of preference vars.
        /// </summary>
        /// <returns>Does not return a value.</returns>
        private void InitializeDefaults()
        {
            SessionStateInternal ss = _engine.Context.EngineSessionState;
            Dbg.Assert(ss != null, "SessionState should not be null");

            // Add the variables that must always be there...
            ss.InitializeFixedVariables();
        }
    }
}
