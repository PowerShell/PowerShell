// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal.Host;
using System.Security.Permissions;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This subclass of TraceListener allows for the trace output
    /// coming from a System.Management.Automation.TraceSwitch
    /// to be passed to the Msh host's RawUI methods.
    /// </summary>
    /// <remarks>
    /// This trace listener cannot be specified in the app.config file.
    /// It must be added through the add-tracelistener cmdlet.
    /// </remarks>
    internal class PSHostTraceListener
        : System.Diagnostics.TraceListener
    {
        #region TraceListener constructors and disposer

        /// <summary>
        /// Default constructor used if no.
        /// </summary>
        internal PSHostTraceListener(PSCmdlet cmdlet)
            : base(string.Empty)
        {
            if (cmdlet == null)
            {
                throw new PSArgumentNullException("cmdlet");
            }

            Diagnostics.Assert(
                cmdlet.Host.UI is InternalHostUserInterface,
                "The internal host must be available to trace");

            _ui = cmdlet.Host.UI as InternalHostUserInterface;
        }

        ~PSHostTraceListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Closes the TraceListenerDialog so that it no longer receives trace output.
        /// </summary>
        /// <param name="disposing">
        /// True if the TraceListener is being disposed, false otherwise.
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.Close();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion TraceListener constructors and disposer

        /// <summary>
        /// Sends the given output string to the host for processing.
        /// </summary>
        /// <param name="output">
        /// The trace output to be written.
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void Write(string output)
        {
            try
            {
                _cachedWrite.Append(output);
            }
            catch (Exception)
            {
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }

        private StringBuilder _cachedWrite = new StringBuilder();

        /// <summary>
        /// Sends the given output string to the host for processing.
        /// </summary>
        /// <param name="output">
        /// The trace output to be written.
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void WriteLine(string output)
        {
            try
            {
                _cachedWrite.Append(output);
                _cachedWrite.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff "));

                _ui.WriteDebugLine(_cachedWrite.ToString());
                _cachedWrite.Remove(0, _cachedWrite.Length);
            }
            catch (Exception)
            {
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }

        /// <summary>
        /// The host interface to write the debug line to.
        /// </summary>
        private InternalHostUserInterface _ui;
    }
}
