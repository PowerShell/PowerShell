/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Management.Automation.Internal;
    using Microsoft.PowerShell.Commands.Internal.Format;

    ///
    /// <summary>
    /// Null sink to absorb pipeline output
    /// </summary>
    [CmdletAttribute("Out", "Null", SupportsShouldProcess = false,
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113366", RemotingCapability = RemotingCapability.None)]
    public class OutNullCommand : PSCmdlet
    {
        /// <summary>
        /// This parameter specifies the current pipeline object
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { set; get; } = AutomationNull.Value;

        /// 
        /// <summary>
        /// Do nothing
        /// </summary>
        protected override void ProcessRecord()
        {
            // explicitely overriden:
            // do not do any processing
        }
    }

    /// <summary>
    /// implementation for the out-default command
    /// this command it impicitely inject by the
    /// powershell.exe host at the end of the pipeline as the
    /// default sink (display to console screen)
    /// </summary>
    [Cmdlet("Out", "Default", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113362", RemotingCapability = RemotingCapability.None)]
    public class OutDefaultCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Determines whether objects should be sent to API consumers.
        /// This command is automatically added to the pipeline when PowerShell is transcripting and
        /// invoked via API. This ensures that the objects pass through the formatting and output
        /// system, but can still make it to the API consumer.
        /// </summary>
        [Parameter()]
        public SwitchParameter Transcript { get; set; }

        /// <summary>
        /// set inner command
        /// </summary>
        public OutDefaultCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// just hook up the LineOutput interface
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

            _savedTranscribeOnly = Host.UI.TranscribeOnly;
            if (Transcript)
            {
                Host.UI.TranscribeOnly = true;
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
        /// Process the OutVar, if set
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
                Object inputObjectBase = PSObject.Base(InputObject);

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

            if (Transcript)
            {
                Host.UI.TranscribeOnly = _savedTranscribeOnly;
            }
        }

        private ArrayList _outVarResults = null;
        private bool _savedTranscribeOnly = false;
    }

    /// <summary>
    /// implementation for the out-host command
    /// </summary>
    [Cmdlet("Out", "Host", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113365", RemotingCapability = RemotingCapability.None)]
    public class OutHostCommand : FrontEndCommandBase
    {
        #region Command Line Parameters

        /// <summary>
        /// non positional parameter to specify paging
        /// </summary>
        private bool _paging;

        #endregion

        /// <summary>
        /// constructor of OutHostCommand
        /// </summary>
        public OutHostCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// optional, non positional parameter to specify paging
        /// FALSE: names only
        /// TRUE: full info
        /// </summary>
        [Parameter]
        public SwitchParameter Paging
        {
            get { return _paging; }
            set { _paging = value; }
        }

        /// <summary>
        /// just hook up the LineOutput interface
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