/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
#region Using directives

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;


#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements a cmdlet that applies a script block
    /// to each element of the pipeline.
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Measure, "Command", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113348", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(TimeSpan))]
    public sealed class MeasureCommandCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject
        {
            set
            {
                _inputObject = value;
            }
            get
            {
                return _inputObject;
            }
        }
        private PSObject _inputObject = AutomationNull.Value;


        /// <summary>
        /// The script block to apply
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock Expression
        {
            set
            {
                _script = value;
            }
            get
            {
                return _script;
            }
        }
        private ScriptBlock _script;

        #endregion

        #region private members

        private System.Diagnostics.Stopwatch _stopWatch = new System.Diagnostics.Stopwatch();

        #endregion

        #region methods


        /// <summary>
        /// Output the timer
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(_stopWatch.Elapsed);
        } // EndProcessing


        /// <summary>
        /// Execute the script block passing in the current pipeline object as
        /// it's only parameter.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Only accumulate the time used by this scriptblock...
            // As results are discarded, write directly to a null pipe instead of accumulating.
            _stopWatch.Start();
            _script.InvokeWithPipe(
                useLocalScope: false,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToCurrentErrorPipe,
                dollarUnder: InputObject,   // $_
                input: new object[0], // $input
                scriptThis: AutomationNull.Value,
                outputPipe: new Pipe { NullPipe = true },
                invocationInfo: null);

            _stopWatch.Stop();
        }

        #endregion
    }
} // namespace Microsoft.PowerShell.Commands

