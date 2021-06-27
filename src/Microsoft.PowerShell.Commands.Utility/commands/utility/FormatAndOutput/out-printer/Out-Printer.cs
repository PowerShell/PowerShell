// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the out-printer command.
    /// </summary>
    [Cmdlet(VerbsData.Out, "Printer", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109553")]
    public class OutPrinterCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutPrinterCommand"/> class
        /// and sets the inner command.
        /// </summary>
        public OutPrinterCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// Optional name of the printer to print to.
        /// The alias allows "lp -P printer".
        /// </summary>
        [Parameter(Position = 0)]
        [Alias("PrinterName")]
        public string Name
        {
            get { return _printerName; }

            set { _printerName = value; }
        }

        private string _printerName;

        /// <summary>
        /// Read command line parameters.
        /// </summary>
        protected override void BeginProcessing()
        {
            // set up the Screen Host interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            outInner.LineOutput = InstantiateLineOutputInterface();

            // finally call the base class for general hookup
            base.BeginProcessing();
        }

        /// <summary>
        /// One-time initialization: acquire a screen host interface by creating one on top of a memory buffer.
        /// </summary>
        private LineOutput InstantiateLineOutputInterface()
        {
            PrinterLineOutput printOutput = new(_printerName);
            return (LineOutput)printOutput;
        }
    }
}
