// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if UNIX

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands
{
    #region Stop-Computer

    /// <summary>
    /// Cmdlet to stop computer.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Stop, "Computer", SupportsShouldProcess = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097151", RemotingCapability = RemotingCapability.SupportedByCommand)]
    public sealed class StopComputerCommand : PSCmdlet, IDisposable
    {
        #region Private Members

        private Process _process = null;

        #endregion

        // TODO: Support remote computers?

        #region "IDisposable Members"

        /// <summary>
        /// Dispose Method.
        /// </summary>
        public void Dispose()
        {
            _process.Dispose();
        }

        #endregion "IDisposable Members"

        #region "Overrides"

        /// <summary>
        /// BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            doShutdown();
        }

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

        private void doShutdown() {
            String cmd = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                cmd = "-P now";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                cmd = "now";
            }

            _process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/sbin/shutdown",
                    Arguments = cmd,
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
