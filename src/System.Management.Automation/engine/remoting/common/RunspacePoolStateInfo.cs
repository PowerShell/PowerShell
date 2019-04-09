// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines type which has information about RunspacePoolState
    /// and exception associated with that state.
    /// </summary>
    /// <remarks>This class is created so that a state change along
    /// with its reason can be transported from the server to the
    /// client in case of RemoteRunspacePool</remarks>
    public sealed class RunspacePoolStateInfo
    {
        /// <summary>
        /// State of the runspace pool when this event occured.
        /// </summary>
        public RunspacePoolState State { get; }

        /// <summary>
        /// Exception associated with that state.
        /// </summary>
        public Exception Reason { get; }

        /// <summary>
        /// Constructor for creating the state info.
        /// </summary>
        /// <param name="state">State.</param>
        /// <param name="reason">exception that resulted in this
        /// state change. Can be null</param>
        public RunspacePoolStateInfo(RunspacePoolState state, Exception reason)
        {
            State = state;
            Reason = reason;
        }
    }
}
