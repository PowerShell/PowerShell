/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.IO;
using System.Security.Permissions;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal.Host;

namespace Microsoft.PowerShell.Commands
{
	/// <summary>
	/// This subclass of TraceListener allows for the trace output
	/// coming from a System.Management.Automation.TraceSwitch
	/// to be passed to the Msh host's RawUI methods.
	/// </summary>
	/// 
    /// <remarks>
    /// This trace listener cannot be specified in the app.config file.
    /// It must be added through the add-tracelistener cmdlet.
    /// </remarks>
    /// 
	internal class PSHostTraceListener 
        : System.Diagnostics.TraceListener
	{
        #region TraceListener constructors and disposer

        /// <summary>
        /// Default constructor used if no.
        /// </summary>
        internal PSHostTraceListener (PSCmdlet cmdlet)
            : base("")
		{
            if (cmdlet == null)
            {
                throw new PSArgumentNullException("cmdlet");
            }

            Diagnostics.Assert(
                cmdlet.Host.UI is InternalHostUserInterface,
                "The internal host must be available to trace");

            this.ui = cmdlet.Host.UI as InternalHostUserInterface;
		}

        ~PSHostTraceListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Closes the TraceListenerDialog so that it no longer
        /// receives trace output.
        /// </summary>
        /// 
        /// <param name="disposing">
        /// true if the TraceListener is being disposed, false
        /// otherwise.
        /// </param>
        /// 
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

        /// <summary>
        /// Closes the dialog and then calls the base class Close
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void Close()
        {
            // Call the base class close

            base.Close();
        }

        #endregion TraceListener constructors and disposer

        /// <summary>
        /// Sends the given output string to the host for processing
        /// </summary>
        /// <param name="output">
        /// The trace output to be written
        /// </param>
        ///
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void Write(string output)
        {
            try
            {
                cachedWrite.Append(output);
            }
            catch (Exception e)
            {
                UtilityCommon.CheckForSevereException(null, e);
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }
        private StringBuilder cachedWrite = new StringBuilder();

        /// <summary>
        /// Sends the given output string to the host for processing
        /// </summary>
        /// <param name="output">
        /// The trace output to be written
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand)]
        public override void WriteLine(string output)
        {
            try
            {
                cachedWrite.Append(output);

                ui.WriteDebugLine(cachedWrite.ToString());
                cachedWrite.Remove(0, cachedWrite.Length);
            }
            catch (Exception e)
            {
                UtilityCommon.CheckForSevereException(null, e);
                // Catch and ignore all exceptions while tracing
                // We don't want tracing to bring down the process.
            }
        }

        /// <summary>
        /// The host interface to write the debug line to.
        /// </summary>
        private InternalHostUserInterface ui;

    } // class PSHostTraceListener
} // namespace System.Management.Automation
