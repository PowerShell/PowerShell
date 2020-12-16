// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Text;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implementation for the out-string command.
    /// </summary>
    [Cmdlet(VerbsData.Out, "String", DefaultParameterSetName = "NoNewLineFormatting", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097024", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public class OutStringCommand : FrontEndCommandBase
    {
        #region Command Line Parameters
        /// <summary>
        /// Optional, non positional parameter to specify the streaming behavior.
        /// FALSE: accumulate all the data, then write a single string.
        /// TRUE: write one line at the time.
        /// </summary>
        [Parameter(ParameterSetName = "StreamFormatting")]
        public SwitchParameter Stream
        {
            get { return _stream; }

            set { _stream = value; }
        }

        private bool _stream;

        /// <summary>
        /// Optional, number of columns to use when writing to device.
        /// </summary>
        [ValidateRangeAttribute(2, int.MaxValue)]
        [Parameter]
        public int Width
        {
            get { return (_width != null) ? _width.Value : 0; }

            set { _width = value; }
        }

        private int? _width = null;

        /// <summary>
        /// False to add a newline to the end of the output string, true if not.
        /// </summary>
        [Parameter(ParameterSetName = "NoNewLineFormatting")]
        public SwitchParameter NoNewline
        {
            get { return _noNewLine; }

            set { _noNewLine = value; }
        }

        private bool _noNewLine = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OutStringCommand"/> class
        /// and sets the inner command.
        /// </summary>
        public OutStringCommand()
        {
            this.implementation = new OutputManagerInner();
        }

        /// <summary>
        /// Read command line parameters.
        /// </summary>
        protected override void BeginProcessing()
        {
            // set up the LineOutput interface
            OutputManagerInner outInner = (OutputManagerInner)this.implementation;

            outInner.LineOutput = InstantiateLineOutputInterface();

            // finally call the base class for general hookup
            base.BeginProcessing();
        }

        /// <summary>
        /// One-time initialization: acquire a screen host interface
        /// by creating one on top of a stream.
        /// </summary>
        private LineOutput InstantiateLineOutputInterface()
        {
            // set up the streaming text writer
            StreamingTextWriter.WriteLineCallback callback = new(this.OnWriteLine);

            _writer = new StreamingTextWriter(callback, Host.CurrentCulture);

            // compute the # of columns available
            int computedWidth = int.MaxValue;

            if (_width != null)
            {
                // use the value from the command line
                computedWidth = _width.Value;
            }

            // use it to create and initialize the Line Output writer
            TextWriterLineOutput twlo = new(_writer, computedWidth);

            // finally have the LineOutput interface extracted
            return (LineOutput)twlo;
        }

        /// <summary>
        /// Callback to add lines to the buffer or to write them to the output stream.
        /// </summary>
        /// <param name="s"></param>
        private void OnWriteLine(string s)
        {
            if (_stream)
            {
                this.WriteObject(s);
            }
            else
            {
                if (_noNewLine)
                {
                    _buffer.Append(s);
                }
                else
                {
                    _buffer.AppendLine(s);
                }
            }
        }

        /// <summary>
        /// Execution entry point.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            _writer.Flush();
        }

        /// <summary>
        /// Execution entry point.
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            // close the writer
            _writer.Flush();
            _writer.Dispose();

            if (!_stream)
                this.WriteObject(_buffer.ToString());
        }

        /// <summary>
        /// Writer used by the LineOutput.
        /// </summary>
        private StreamingTextWriter _writer = null;

        /// <summary>
        /// Buffer used when buffering until the end.
        /// </summary>
        private readonly StringBuilder _buffer = new();
    }
}
