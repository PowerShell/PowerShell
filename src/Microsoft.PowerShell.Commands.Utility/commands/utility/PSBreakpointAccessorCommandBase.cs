// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Base class for Get/Set-PSBreakpoint.
    /// </summary>
    public abstract class PSBreakpointAccessorCommandBase : PSBreakpointCommandBase
    {
        #region strings

        internal const string CommandParameterSetName = "Command";
        internal const string LineParameterSetName = "Line";
        internal const string VariableParameterSetName = "Variable";

        #endregion strings
    }
}
