// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if UNIX

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
#region Restart-Computer

    /// <summary>
    /// Cmdlet to restart computer.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Restart, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097060", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class RestartComputerCommand : CommandLineCmdletBase
    {
        // TODO: Support remote computers?

#region "Overrides"

        /// <summary>
        /// BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (InternalTestHooks.TestStopComputer)
            {
                var retVal = InternalTestHooks.TestStopComputerResults;
                if (retVal != 0)
                {
                    string errMsg = StringUtil.Format("Command returned 0x{0:X}", retVal);
                    ErrorRecord error = new ErrorRecord(
                        new InvalidOperationException(errMsg), "Command Failed", ErrorCategory.OperationStopped, "localhost");
                    WriteError(error);
                }
                return;
            }

            RunCommand("/sbin/shutdown", "-r now");
        }
#endregion "Overrides"
    }
#endregion Restart-Computer

#region Stop-Computer

    /// <summary>
    /// Cmdlet to stop computer.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097151", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class StopComputerCommand : CommandLineCmdletBase
    {
        // TODO: Support remote computers?

#region "Overrides"

        /// <summary>
        /// BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            var args = "-P now";
            if (Platform.IsMacOS)
            {
                args = "now";
            }
            if (InternalTestHooks.TestStopComputer)
            {
                var retVal = InternalTestHooks.TestStopComputerResults;
                if (retVal != 0)
                {
                    string errMsg = StringUtil.Format("Command returned 0x{0:X}", retVal);
                    ErrorRecord error = new ErrorRecord(
                        new InvalidOperationException(errMsg), "Command Failed", ErrorCategory.OperationStopped, "localhost");
                    WriteError(error);
                }
                return;
            }

            RunCommand("/sbin/shutdown", args);
        }
#endregion "Overrides"
    }

    /// <summary>
    /// A base class for cmdlets that can run shell commands.
    /// </summary>
    public class CommandLineCmdletBase : PSCmdlet, IDisposable
    {
#region Private Members
        private Process _process = null;
#endregion

#region "IDisposable Members"

        /// <summary>
        /// Releases all resources used by the <see cref="CommandLineCmdletBase"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="CommandLineCmdletBase"/>
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _process?.Dispose();
            }
        }

#endregion "IDisposable Members"

#region "Overrides"
        /// <summary>
        /// To implement ^C.
        /// </summary>
        protected override void StopProcessing()
        {
            if (_process == null) {
                return;
            }

            try {
                if (!_process.HasExited) {
                    _process.Kill();
                }
                WriteObject(_process.ExitCode);
            }
            catch (InvalidOperationException) {}
            catch (NotSupportedException) {}
        }
#endregion "Overrides"

#region "Internals"

        /// <summary>
        /// Run a command.
        /// </summary>
        protected void RunCommand(String command = "/sbin/shutdown", String args = "") {
            _process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            _process.Start();
        }
#endregion
    }
#endregion
}
#endif
