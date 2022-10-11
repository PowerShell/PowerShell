// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// This class contains information about the debugging capability of the server side of the
    /// MS-PSRDP connection. The functionality that is supported is determined by the PowerShell
    /// version on the server. These capabilities will be used in remote debugging sessions to
    /// determine what is supported by the server.
    /// </summary>
    internal sealed class RemoteDebuggingCapability
    {
        private readonly HashSet<string> _supportedCommands = new HashSet<string>();

        internal Version PSVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteDebuggingCapability"/> class.
        /// </summary>
        /// <param name="powerShellVersion">
        /// The version of PowerShell used on the remote server debugger.
        /// </param>
        /// <remarks>
        /// This should only be invoked by the static create method.
        /// </remarks>
        private RemoteDebuggingCapability(Version powerShellVersion)
        {
            PSVersion = powerShellVersion;

            // Commands available in all server versions
            _supportedCommands.Add(RemoteDebuggingCommands.GetDebuggerStopArgs);
            _supportedCommands.Add(RemoteDebuggingCommands.SetDebuggerAction);
            _supportedCommands.Add(RemoteDebuggingCommands.SetDebugMode);

            if (PSVersion == null)
            {
                return;
            }

            // Commands added in v5
            if (PSVersion.Major >= 5)
            {
                _supportedCommands.Add(RemoteDebuggingCommands.SetDebuggerStepMode);
                _supportedCommands.Add(RemoteDebuggingCommands.SetUnhandledBreakpointMode);
            }

            // Commands added in v7
            if (PSVersion.Major >= 7)
            {
                _supportedCommands.Add(RemoteDebuggingCommands.GetBreakpoint);
                _supportedCommands.Add(RemoteDebuggingCommands.SetBreakpoint);
                _supportedCommands.Add(RemoteDebuggingCommands.EnableBreakpoint);
                _supportedCommands.Add(RemoteDebuggingCommands.DisableBreakpoint);
                _supportedCommands.Add(RemoteDebuggingCommands.RemoveBreakpoint);
            }
        }

        /// <summary>
        /// Creates a <see cref="RemoteDebuggingCapability"/> instance that can be
        /// used to identify the remoting capabilities of the server debugger.
        /// </summary>
        /// <param name="powerShellVersion">
        /// The version of PowerShell used on the remote server debugger.
        /// </param>
        /// <returns>
        /// A new RemoteDebuggingCapability instance that is based on the version
        /// of PowerShell used on the remote server debugger.
        /// </returns>
        internal static RemoteDebuggingCapability CreateDebuggingCapability(Version powerShellVersion) =>
        new RemoteDebuggingCapability(powerShellVersion);

        /// <summary>
        /// Checks if a command is supported in the server version used to create
        /// this instance.
        /// </summary>
        /// <param name="commandName">
        /// The name of the command to check.
        /// </param>
        /// <returns>
        /// True if the command is supported; false otherwise.
        /// </returns>
        internal bool IsCommandSupported(string commandName) =>
            _supportedCommands.Contains(commandName);
    }

    internal static class RemoteDebuggingCommands
    {
        #region DO NOT REMOVE OR CHANGE THE VALUES OF THESE CONSTANTS - it will break remote debugging compatibility with PowerShell

        // Commands related to debugger stop events
        internal const string GetDebuggerStopArgs = "__Get-PSDebuggerStopArgs";
        internal const string SetDebuggerAction = "__Set-PSDebuggerAction";

        // Miscellaneous debug commands
        internal const string SetDebuggerStepMode = "__Set-PSDebuggerStepMode";
        internal const string SetDebugMode = "__Set-PSDebugMode";
        internal const string SetUnhandledBreakpointMode = "__Set-PSUnhandledBreakpointMode";

        // Breakpoint commands
        internal const string GetBreakpoint = "__Get-PSBreakpoint";
        internal const string SetBreakpoint = "__Set-PSBreakpoint";
        internal const string EnableBreakpoint = "__Enable-PSBreakpoint";
        internal const string DisableBreakpoint = "__Disable-PSBreakpoint";
        internal const string RemoveBreakpoint = "__Remove-PSBreakpoint";

        #endregion

        internal static string CleanCommandName(string commandName)
        {
            return commandName.TrimStart('_');
        }
    }
}
