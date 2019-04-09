// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Management.Automation.Internal;
    using Microsoft.PowerShell.Commands.Internal.Format;

    /// <summary>
    /// Null sink to absorb pipeline output.
    /// </summary>
    [CmdletAttribute("Out", "Null", SupportsShouldProcess = false,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113366", RemotingCapability = RemotingCapability.None)]
    public class OutNullCommand : PSCmdlet
    {
        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        /// <summary>
        /// Do nothing.
        /// </summary>
        protected override void ProcessRecord()
        {
            // explicitely overridden:
            // do not do any processing
        }
    }

    /// <summary>
    /// Implementation for the out-default command
    /// this command it implicitly inject by the
    /// powershell host at the end of the pipeline as the
    /// default sink (display to console screen)
    /// </summary>
    [Cmdlet(VerbsData.Out, "Default", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113362", RemotingCapability = RemotingCapability.None)]
    public class OutDefaultCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Determines whether objects should be sent to API consumers.
        /// This command is automatically added to the pipeline when PowerShell is transcribing and
        /// invoked via API. This ensures that the objects pass through the formatting and output
        /// system, but can still make it to the API consumer.
        /// </summary>
        [Parameter()]
        public SwitchParameter Transcript { get; set; }

        /// <summary>
        /// Set inner command.
        /// </summary>
        public OutDefaultCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// Just hook up the LineOutput interface.
        /// </summary>
        protected override void BeginProcessing()
        {
            PSHostUserInterface console = this.Host.UI;
            ConsoleLineOutput lineOutput = new ConsoleLineOutput(console, false, new TerminatingErrorContext(this));

            ((OutputManagerInner)this.implementation).LineOutput = lineOutput;

            MshCommandRuntime mrt = this.CommandRuntime as MshCommandRuntime;

            if (mrt != null)
            {
                mrt.MergeUnclaimedPreviousErrorResults = true;
            }

            if (Transcript)
            {
                _transcribeOnlyCookie = Host.UI.SetTranscribeOnly();
            }

            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            base.BeginProcessing();

            if (Context.CurrentCommandProcessor.CommandRuntime.OutVarList != null)
            {
                _outVarResults = new ArrayList();
            }
        }

        /// <summary>
        /// Process the OutVar, if set.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Transcript)
            {
                WriteObject(InputObject);
            }

            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            if (_outVarResults != null)
            {
                object inputObjectBase = PSObject.Base(InputObject);

                // Ignore errors and formatting records, as those can't be captured
                if (
                    (inputObjectBase != null) &&
                    (!(inputObjectBase is ErrorRecord)) &&
                    (!inputObjectBase.GetType().FullName.StartsWith(
                        "Microsoft.PowerShell.Commands.Internal.Format", StringComparison.OrdinalIgnoreCase)))
                {
                    _outVarResults.Add(InputObject);
                }
            }

            base.ProcessRecord();
        }

        /// <summary>
        /// Swap the outVar with what we've processed, if OutVariable is set.
        /// </summary>
        protected override void EndProcessing()
        {
            // This needs to be done directly through the command runtime, as Out-Default
            // doesn't actually write pipeline objects.
            if ((_outVarResults != null) && (_outVarResults.Count > 0))
            {
                Context.CurrentCommandProcessor.CommandRuntime.OutVarList.Clear();
                foreach (Object item in _outVarResults)
                {
                    Context.CurrentCommandProcessor.CommandRuntime.OutVarList.Add(item);
                }

                _outVarResults = null;
            }

            base.EndProcessing();
        }

        /// <summary>
        /// Revert transcription state on Dispose.
        /// </summary>
        protected override void InternalDispose()
        {
            try
            {
                base.InternalDispose();
            }
            finally
            {
                if (_transcribeOnlyCookie != null)
                {
                    _transcribeOnlyCookie.Dispose();
                    _transcribeOnlyCookie = null;
                }
            }
        }

        private ArrayList _outVarResults = null;
        private IDisposable _transcribeOnlyCookie = null;
    }

    /// <summary>
    /// Implementation for the out-host command.
    /// </summary>
    [Cmdlet(VerbsData.Out, "Host", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113365", RemotingCapability = RemotingCapability.None)]
    public class OutHostCommand : FrontEndCommandBase
    {
        #region Command Line Parameters

        /// <summary>
        /// Non positional parameter to specify paging.
        /// </summary>
        private bool _paging;

        #endregion

        /// <summary>
        /// Constructor of OutHostCommand.
        /// </summary>
        public OutHostCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// Optional, non positional parameter to specify paging
        /// FALSE: names only
        /// TRUE: full info.
        /// </summary>
        [Parameter]
        public SwitchParameter Paging
        {
            get { return _paging; }

            set { _paging = value; }
        }

        /// <summary>
        /// Just hook up the LineOutput interface.
        /// </summary>
        protected override void BeginProcessing()
        {
            PSHostUserInterface console = this.Host.UI;
            ConsoleLineOutput lineOutput = new ConsoleLineOutput(console, _paging, new TerminatingErrorContext(this));

            ((OutputManagerInner)this.implementation).LineOutput = lineOutput;
            base.BeginProcessing();
        }
    }
}
