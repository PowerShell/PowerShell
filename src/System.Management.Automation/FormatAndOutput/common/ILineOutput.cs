// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Text;
using System.Globalization;

// interfaces for host interaction

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Base class providing support for string manipulation.
    /// This class is a tear off class provided by the LineOutput class
    ///
    /// Assumptions (in addition to the assumptions made for LineOutput):
    /// - characters map to one or more character cells
    ///
    /// NOTE: we provide a base class that is valid for devices that have a
    /// 1:1 mapping between a UNICODE character and a display cell.
    /// </summary>
    internal class DisplayCells
    {
        internal virtual int Length(string str)
        {
            return Length(str, 0);
        }

        internal virtual int Length(string str, int offset)
        {
            return str.Length - offset;
        }

        internal virtual int Length(char character) { return 1; }

        internal virtual int GetHeadSplitLength(string str, int displayCells)
        {
            return GetHeadSplitLength(str, 0, displayCells);
        }

        internal virtual int GetHeadSplitLength(string str, int offset, int displayCells)
        {
            int len = str.Length - offset;
            return (len < displayCells) ? len : displayCells;
        }

        internal virtual int GetTailSplitLength(string str, int displayCells)
        {
            return GetTailSplitLength(str, 0, displayCells);
        }

        internal virtual int GetTailSplitLength(string str, int offset, int displayCells)
        {
            int len = str.Length - offset;
            return (len < displayCells) ? len : displayCells;
        }

        #region Helpers

        /// <summary>
        /// Given a string and a number of display cells, it computes how many
        /// characters would fit starting from the beginning or end of the string.
        /// </summary>
        /// <param name="str">String to be displayed.</param>
        /// <param name="offset">Offset inside the string.</param>
        /// <param name="displayCells">Number of display cells.</param>
        /// <param name="head">If true compute from the head (i.e. k++) else from the tail (i.e. k--).</param>
        /// <returns>Number of characters that would fit.</returns>
        protected int GetSplitLengthInternalHelper(string str, int offset, int displayCells, bool head)
        {
            int filledDisplayCellsCount = 0; // number of cells that are filled in
            int charactersAdded = 0; // number of characters that fit
            int currCharDisplayLen; // scratch variable

            int k = (head) ? offset : str.Length - 1;
            int kFinal = (head) ? str.Length - 1 : offset;
            while (true)
            {
                if ((head && (k > kFinal)) || ((!head) && (k < kFinal)))
                {
                    break;
                }
                // compute the cell number for the current character
                currCharDisplayLen = this.Length(str[k]);

                if (filledDisplayCellsCount + currCharDisplayLen > displayCells)
                {
                    // if we added this character it would not fit, we cannot continue
                    break;
                }
                // keep adding, we fit
                filledDisplayCellsCount += currCharDisplayLen;
                charactersAdded++;

                // check if we fit exactly
                if (filledDisplayCellsCount == displayCells)
                {
                    // exact fit, we cannot add more
                    break;
                }

                k = (head) ? (k + 1) : (k - 1);
            }

            return charactersAdded;
        }
        #endregion

    }

    /// <summary>
    /// Base class providing information about the screen device capabilities
    /// and used to write the output strings to the text output device.
    /// Each device supported will have to derive from it.
    /// Examples of supported devices are:
    /// *   Screen Layout: it layers on top of Console and RawConsole
    /// *   File: it layers on top of a TextWriter
    /// *   In Memory text stream: it layers on top of an in memory buffer
    /// *   Printer: it layers on top of a memory buffer then sent to a printer device
    ///
    /// Assumptions:
    /// - Fixed pitch font: layout done in terms of character cells
    /// - character cell layout not affected by bold, reverse screen, color, etc.
    /// - returned values might change from call to call if the specific underlying
    ///   implementation allows window resizing.
    /// </summary>
    internal abstract class LineOutput
    {
        /// <summary>
        /// Whether the device requires full buffering of formatting
        /// objects before any processing.
        /// </summary>
        internal virtual bool RequiresBuffering { get { return false; } }

        /// <summary>
        /// Delegate the implementor of ExecuteBufferPlayBack should
        /// call to cause the playback to happen when ready to execute.
        /// </summary>
        internal delegate void DoPlayBackCall();

        /// <summary>
        /// If RequiresBuffering = true, this call will be made to
        /// start the playback.
        /// </summary>
        internal virtual void ExecuteBufferPlayBack(DoPlayBackCall playback) { }

        /// <summary>
        /// The number of columns the current device has.
        /// </summary>
        internal abstract int ColumnNumber { get; }

        /// <summary>
        /// The number of rows the current device has.
        /// </summary>
        internal abstract int RowNumber { get; }

        /// <summary>
        /// Write a line to the output device.
        /// </summary>
        /// <param name="s">
        ///     string to be written to the device
        /// </param>
        internal abstract void WriteLine(string s);

        internal WriteStreamType WriteStream
        {
            get;
            set;
        }

        /// <summary>
        /// Handle the stop processing signal.
        /// Set a flag that will be checked during operations.
        /// </summary>
        internal void StopProcessing()
        {
            _isStopping = true;
        }

        private bool _isStopping;

        internal void CheckStopProcessing()
        {
            if (!_isStopping)
                return;
            throw new PipelineStoppedException();
        }

        /// <summary>
        /// Return an instance of the display helper tear off.
        /// </summary>
        /// <value></value>
        internal virtual DisplayCells DisplayCells
        {
            get
            {
                CheckStopProcessing();
                // just return the default singleton implementation
                return _displayCellsDefault;
            }
        }

        /// <summary>
        /// Singleton used for the default implementation.
        /// NOTE: derived classes may chose to provide a different
        /// implementation by overriding.
        /// </summary>
        protected static DisplayCells _displayCellsDefault = new DisplayCells();
    }

    /// <summary>
    /// Helper class to provide line breaking (based on device width)
    /// and embedded newline processing
    /// It needs to be provided with two callbacks for line processing.
    /// </summary>
    internal class WriteLineHelper
    {
        #region callbacks

        /// <summary>
        /// Delegate definition.
        /// </summary>
        /// <param name="s">String to write.</param>
        internal delegate void WriteCallback(string s);

        /// <summary>
        /// Instance of the delegate previously defined
        /// for line that has EXACTLY this.ncols characters.
        /// </summary>
        private WriteCallback _writeCall = null;

        /// <summary>
        /// Instance of the delegate previously defined
        /// for generic line, less that this.ncols characters.
        /// </summary>
        private WriteCallback _writeLineCall = null;

        #endregion

        private bool _lineWrap;

        /// <summary>
        /// Construct an instance, given the two callbacks
        /// NOTE: if the underlying device treats the two cases as the
        /// same, the same delegate can be passed twice.
        /// </summary>
        /// <param name="lineWrap">True if we require line wrapping.</param>
        /// <param name="wlc">Delegate for WriteLine(), must ben non null.</param>
        /// <param name="wc">Delegate for Write(), if null, use the first parameter.</param>
        /// <param name="displayCells">Helper object for manipulating strings.</param>
        internal WriteLineHelper(bool lineWrap, WriteCallback wlc, WriteCallback wc, DisplayCells displayCells)
        {
            if (wlc == null)
                throw PSTraceSource.NewArgumentNullException("wlc");
            if (displayCells == null)
                throw PSTraceSource.NewArgumentNullException("displayCells");

            _displayCells = displayCells;
            _writeLineCall = wlc;
            _writeCall = wc ?? wlc;
            _lineWrap = lineWrap;
        }

        /// <summary>
        /// Main entry point to process a line.
        /// </summary>
        /// <param name="s">String to process.</param>
        /// <param name="cols">Width of the device.</param>
        internal void WriteLine(string s, int cols)
        {
            WriteLineInternal(s, cols);
        }

        /// <summary>
        /// Internal helper, needed because it might make recursive calls to itself.
        /// </summary>
        /// <param name="val">String to process.</param>
        /// <param name="cols">Width of the device.</param>
        private void WriteLineInternal(string val, int cols)
        {
            if (string.IsNullOrEmpty(val))
            {
                _writeLineCall(val);
                return;
            }

            // If the output is being redirected, then we don't break val
            if (!_lineWrap)
            {
                _writeCall(val);
                return;
            }

            // check for line breaks
            string[] lines = StringManipulationHelper.SplitLines(val);

            // process the substrings as separate lines
            for (int k = 0; k < lines.Length; k++)
            {
                // compute the display length of the string
                int displayLength = _displayCells.Length(lines[k]);

                if (displayLength < cols)
                {
                    // NOTE: this is the case where where System.Console.WriteLine() would work just fine
                    _writeLineCall(lines[k]);
                    continue;
                }

                if (displayLength == cols)
                {
                    // NOTE: this is the corner case where System.Console.WriteLine() cannot be called
                    _writeCall(lines[k]);
                    continue;
                }

                // the string does not fit, so we have to wrap around on multiple lines
                string s = lines[k];

                while (true)
                {
                    // the string is still too long to fit, write the first cols characters
                    // and go back for more wraparound
                    int splitLen = _displayCells.GetHeadSplitLength(s, cols);
                    WriteLineInternal(s.Substring(0, splitLen), cols);

                    // chop off the first fieldWidth characters, already printed
                    s = s.Substring(splitLen);
                    if (_displayCells.Length(s) <= cols)
                    {
                        // if we fit, print the tail of the string and we are done
                        WriteLineInternal(s, cols);
                        break;
                    }
                }
            }
        }

        private DisplayCells _displayCells;
    }

    /// <summary>
    /// Implementation of the ILineOutput interface accepting an instance of a
    /// TextWriter abstract class.
    /// </summary>
    internal class TextWriterLineOutput : LineOutput
    {
        #region ILineOutput methods

        /// <summary>
        /// Get the columns on the screen
        /// for files, it is settable at creation time.
        /// </summary>
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                return _columns;
            }
        }

        /// <summary>
        /// Get the # of rows on the screen: for files
        /// we return -1, meaning infinite.
        /// </summary>
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                return -1;
            }
        }

        /// <summary>
        /// Write a line by delegating to the writer underneath.
        /// </summary>
        /// <param name="s"></param>
        internal override void WriteLine(string s)
        {
            CheckStopProcessing();
            if (_suppressNewline)
            {
                _writer.Write(s);
            }
            else
            {
                _writer.WriteLine(s);
            }
        }
        #endregion

        /// <summary>
        /// Initialization of the object. It must be called before
        /// attempting any operation.
        /// </summary>
        /// <param name="writer">TextWriter to write to.</param>
        /// <param name="columns">Max columns widths for the text.</param>
        internal TextWriterLineOutput(TextWriter writer, int columns)
        {
            _writer = writer;
            _columns = columns;
        }

        /// <summary>
        /// Initialization of the object. It must be called before
        /// attempting any operation.
        /// </summary>
        /// <param name="writer">TextWriter to write to.</param>
        /// <param name="columns">Max columns widths for the text.</param>
        /// <param name="suppressNewline">False to add a newline to the end of the output string, true if not.</param>
        internal TextWriterLineOutput(TextWriter writer, int columns, bool suppressNewline)
            : this(writer, columns)
        {
            _suppressNewline = suppressNewline;
        }

        private int _columns = 0;

        private TextWriter _writer = null;

        private bool _suppressNewline = false;
    }

    /// <summary>
    /// TextWriter to generate data for the Monad pipeline in a streaming fashion:
    /// the provided callback will be called each time a line is written.
    /// </summary>
    internal class StreamingTextWriter : TextWriter
    {
        #region tracer
        [TraceSource("StreamingTextWriter", "StreamingTextWriter")]
        private static PSTraceSource s_tracer = PSTraceSource.GetTracer("StreamingTextWriter", "StreamingTextWriter");
        #endregion tracer

        /// <summary>
        /// Create an instance by passing a delegate.
        /// </summary>
        /// <param name="writeCall">Delegate to write to.</param>
        /// <param name="culture">Culture for this TextWriter.</param>
        internal StreamingTextWriter(WriteLineCallback writeCall, CultureInfo culture)
            : base(culture)
        {
            if (writeCall == null)
                throw PSTraceSource.NewArgumentNullException("writeCall");

            _writeCall = writeCall;
        }

        #region TextWriter overrides

        public override Encoding Encoding { get { return new UnicodeEncoding(); } }

        public override void WriteLine(string s)
        {
            _writeCall(s);
        }

        #endregion

        /// <summary>
        /// Delegate definition.
        /// </summary>
        /// <param name="s">String to write.</param>
        internal delegate void WriteLineCallback(string s);

        /// <summary>
        /// Instance of the delegate previously defined.
        /// </summary>
        private WriteLineCallback _writeCall = null;
    }
}
