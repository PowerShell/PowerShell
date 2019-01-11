// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Implementation of the LineOutput interface for printer.
    /// </summary>
    internal sealed class PrinterLineOutput : LineOutput
    {
        #region LineOutput implementation

        /// <summary>
        /// Full buffering for printer.
        /// </summary>
        internal override bool RequiresBuffering { get { return true; } }

        /// <summary>
        /// Do the printing on playback.
        /// </summary>
        internal override void ExecuteBufferPlayBack(DoPlayBackCall playback)
        {
            _playbackCall = playback;
            DoPrint();
        }

        /// <summary>
        /// The # of columns for the printer.
        /// </summary>
        /// <value></value>
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                return _deviceColumns;
            }
        }

        /// <summary>
        /// The # of rows for the printer.
        /// </summary>
        /// <value></value>
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                return _deviceRows;
            }
        }

        /// <summary>
        /// Write a line to the output device.
        /// </summary>
        /// <param name="s">Line to write.</param>
        internal override void WriteLine(string s)
        {
            CheckStopProcessing();
            // delegate the action to the helper,
            // that will properly break the string into
            // screen lines
            _writeLineHelper.WriteLine(s, this.ColumnNumber);
        }
        #endregion

        /// <summary>
        /// Used for static initializations like DefaultPrintFontName.
        /// </summary>
        static PrinterLineOutput()
        {
            // This default must be loaded from a resource file as different
            // cultures will have different defaults and the localizer would
            // know the default for different cultures.
            s_defaultPrintFontName = OutPrinterDisplayStrings.DefaultPrintFontName;
        }

        /// <summary>
        /// Constructor for the class.
        /// </summary>
        /// <param name="printerName">Name of printer, if null use default printer.</param>
        internal PrinterLineOutput(string printerName)
        {
            _printerName = printerName;

            // instantiate the helper to do the line processing when LineOutput.WriteXXX() is called
            WriteLineHelper.WriteCallback wl = new WriteLineHelper.WriteCallback(this.OnWriteLine);
            WriteLineHelper.WriteCallback w = new WriteLineHelper.WriteCallback(this.OnWrite);

            _writeLineHelper = new WriteLineHelper(true, wl, w, this.DisplayCells);
        }

        /// <summary>
        /// Callback to be called when IConsole.WriteLine() is called by WriteLineHelper.
        /// </summary>
        /// <param name="s">String to write.</param>
        private void OnWriteLine(string s)
        {
            _lines.Enqueue(s);
        }

        /// <summary>
        /// Callback to be called when Console.Write() is called by WriteLineHelper.
        /// This is called when the WriteLineHelper needs to write a line whose length
        /// is the same as the width of the screen buffer.
        /// </summary>
        /// <param name="s">String to write.</param>
        private void OnWrite(string s)
        {
            _lines.Enqueue(s);
        }

        /// <summary>
        /// Do the printing.
        /// </summary>
        private void DoPrint()
        {
            try
            {
                // create a new print document object and set the printer name, if available
                PrintDocument pd = new PrintDocument();

                if (!string.IsNullOrEmpty(_printerName))
                {
                    pd.PrinterSettings.PrinterName = _printerName;
                }

                // set up the callback mechanism
                pd.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);

                // start printing
                pd.Print();
            }
            finally
            {
                // make sure we do not leak the font
                if (_printFont != null)
                {
                    _printFont.Dispose();
                    _printFont = null;
                }
            }
        }

        /// <summary>
        /// Helper to create a font.
        /// If the font object exists, it does nothing.
        /// Else, the a new object is created and verified.
        /// </summary>
        /// <param name="g">GDI+ graphics object needed for verification.</param>
        private void CreateFont(Graphics g)
        {
            if (_printFont != null)
                return;

            // create the font

            // do we have a specified font?
            if (string.IsNullOrEmpty(_printFontName))
            {
                _printFontName = s_defaultPrintFontName;
            }

            if (_printFontSize <= 0)
            {
                _printFontSize = DefaultPrintFontSize;
            }

            _printFont = new Font(_printFontName, _printFontSize);
            VerifyFont(g);
        }

        /// <summary>
        /// Internal helper to verify that the font is fixed pitch. If the test fails,
        /// it reverts to the default font.
        /// </summary>
        /// <param name="g">GDI+ graphics object needed for verification.</param>
        private void VerifyFont(Graphics g)
        {
            // check if the font is fixed pitch
            // HEURISTICS:
            // we compute the length of two strings, one made of "large" characters
            // one made of "narrow" ones. If they are the same length, we assume that
            // the font is fixed pitch.
            string large = "ABCDEF";
            float wLarge = g.MeasureString(large, _printFont).Width / large.Length;
            string narrow = ".;'}l|";
            float wNarrow = g.MeasureString(narrow, _printFont).Width / narrow.Length;

            if (Math.Abs((float)(wLarge - wNarrow)) < 0.001F)
            {
                // we passed the test
                return;
            }

            // just get back to the default, since it's not fixed pitch
            _printFont.Dispose();
            _printFont = new Font(s_defaultPrintFontName, DefaultPrintFontSize);
        }

        /// <summary>
        /// Event fired for each page to print.
        /// </summary>
        /// <param name="sender">Sender, not used.</param>
        /// <param name="ev">Print page event.</param>
        private void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            float yPos = 0; // GDI+ coordinate down the page
            int linesPrinted = 0; // linesPrinted
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;

            CreateFont(ev.Graphics);

            // compute the height of a line of text
            float lineHeight = _printFont.GetHeight(ev.Graphics);

            // Work out the number of lines per page
            // Use the MarginBounds on the event to do this
            float linesPerPage = ev.MarginBounds.Height / _printFont.GetHeight(ev.Graphics);

            if (!_printingInitialized)
            {
                // on the first page we have to initialize the metrics for LineOutput

                // work out the number of columns per page assuming fixed pitch font
                string s = "ABCDEF";
                float w = ev.Graphics.MeasureString(s, _printFont).Width / s.Length;
                float columnsPerPage = ev.MarginBounds.Width / w;

                _printingInitialized = true;
                _deviceRows = (int)linesPerPage;
                _deviceColumns = (int)columnsPerPage;

                // now that we initialized the column and row count for the LineOutput
                // interface we can tell the outputter to playback from cache to do the
                // proper computations of line widths
                // returning from this call, the string queue on this object is full of
                // lines of text to print
                _playbackCall();
            }

            // now iterate over the file printing out each line
            while ((linesPrinted < linesPerPage) && (_lines.Count > 0))
            {
                // get the string to be printed
                string line = _lines.Dequeue();

                // compute the Y position where to draw
                yPos = topMargin + (linesPrinted * lineHeight);

                // do the actual drawing
                ev.Graphics.DrawString(line, _printFont, Brushes.Black, leftMargin, yPos, new StringFormat());
                linesPrinted++;
            }

            // If we have more lines then print another page
            ev.HasMorePages = _lines.Count > 0;
        }

        /// <summary>
        /// Flag for one-time initialization of the interface (columns, etc.).
        /// </summary>
        private bool _printingInitialized = false;

        /// <summary>
        /// Callback to ask the outputter to playback its cache.
        /// </summary>
        private DoPlayBackCall _playbackCall;

        /// <summary>
        /// Name of the printer to print to. Null means default printer.
        /// </summary>
        private string _printerName = null;

        /// <summary>
        /// Name of the font to use, if null the default is used.
        /// </summary>
        private string _printFontName = null;

        /// <summary>
        /// Font size.
        /// </summary>
        private int _printFontSize = 0;

        /// <summary>
        /// Default font, used if the printFont is not specified or if the
        /// printFont is not fixed pitch.
        /// </summary>
        /// <remarks>
        /// This default must be loaded from a resource file as different
        /// cultures will have different defaults and the localizer would
        /// know the default for different cultures.
        /// </remarks>
        private static readonly string s_defaultPrintFontName;

        /// <summary>
        /// Default size for the default font.
        /// </summary>
        private const int DefaultPrintFontSize = 8;

        /// <summary>
        /// Number of columns on the sheet.
        /// </summary>
        private int _deviceColumns = 80;

        // number of rows per sheet
        private int _deviceRows = 40;

        /// <summary>
        /// Text lines ready to print (after output cache playback).
        /// </summary>
        private Queue<string> _lines = new Queue<string>();

        /// <summary>
        /// Cached font object.
        /// </summary>
        private Font _printFont = null;

        private WriteLineHelper _writeLineHelper;
    }
}
