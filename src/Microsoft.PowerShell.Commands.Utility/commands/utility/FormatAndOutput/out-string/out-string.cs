/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Host;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
 

    /// <summary>
    /// implementation for the out-string command
    /// </summary>
    [Cmdlet("Out", "String", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113368", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class OutStringCommand : FrontEndCommandBase
    {
        
#region Command Line Parameters
        /// <summary>
        /// optional, non positional parameter to specify the
        /// streaming behavior
        /// FALSE: accumulate all the data, then write a single string
        /// TRUE: write one line at the time
        /// </summary>
        [Parameter]
        public SwitchParameter Stream
        {
            get { return stream; }
            set { stream = value; }
        }

        private bool stream;

        /// <summary>
        /// optional, number of columns to use when writing to device
        /// </summary>
        [ValidateRangeAttribute (2, int.MaxValue)]
        [Parameter]
        public int Width
        {
            get { return (width != null) ? width.Value : 0; }
            set { width = value; }
        }

        private Nullable<int> width = null;
#endregion

        /// <summary>
        /// set inner command
        /// </summary>
        public OutStringCommand ()
        {
            this.implementation = new OutputManagerInner ();
        }

        /// <summary>
        /// read command line parameters
        /// </summary>
        protected override void BeginProcessing ()
        {
            // set up the LineOutput interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            outInner.LineOutput = InstantiateLineOutputInterface ();

            // finally call the base class for general hookup
            base.BeginProcessing ();
        }

        /// <summary>
        /// one time initialization: acquire a screen host interface
        /// by creating one on top of a stream
        /// </summary>
        private LineOutput InstantiateLineOutputInterface ()
        {
            // set up the streaming text writer
            StreamingTextWriter.WriteLineCallback callback = new StreamingTextWriter.WriteLineCallback (this.OnWriteLine);

            this.writer = new StreamingTextWriter (callback, Host.CurrentCulture);

            // compute the # of columns available
            int computedWidth = 120;

            if (this.width != null)
            {
                // use the value from the command line
                computedWidth = this.width.Value;
            }
            else
            {
                // use the value we get from the console
                try
                {
                    // NOTE: we subtract 1 because we want to properly handle
                    // the following scenario:
                    // MSH>get-foo|format-table|out-string
                    // in this case, if the computed width is (say) 80, get-content
                    // would cause a wrapping of the 80 column long raw strings.
                    // Hence we set the width to 79.
                    computedWidth = this.Host.UI.RawUI.BufferSize.Width - 1;
                }
                catch (HostException)
                {
                    // non interactive host
                }
            }

            // use it to create and initialize the Line Output writer
            TextWriterLineOutput twlo = new TextWriterLineOutput (this.writer, computedWidth);

            // finally have the LineOutput interface extracted
            return (LineOutput)twlo;
        }

        /// <summary>
        /// callback to add lines to the buffer or to write them to
        /// the output stream
        /// </summary>
        /// <param name="s"></param>
        private void OnWriteLine (string s)
        {
            if (this.stream)
                this.WriteObject (s);
            else
                this.buffer.AppendLine (s);
        }

        /// <summary>
        /// execution entry point
        /// </summary>
        protected override void ProcessRecord ()
        {
            base.ProcessRecord ();
            this.writer.Flush ();
        }

        /// <summary>
        /// execution entry point
        /// </summary>
        protected override void EndProcessing ()
        {
            base.EndProcessing ();

            //close the writer
            this.writer.Flush ();
            this.writer.Dispose();
            
            if (!this.stream)
                this.WriteObject (this.buffer.ToString ());
        }

        /// <summary>
        /// writer used by the LineOutput
        /// </summary>
        private StreamingTextWriter writer = null;

        /// <summary>
        ///  buffer used when buffering until the end
        /// </summary>
        private StringBuilder buffer = new StringBuilder ();

    }
}

