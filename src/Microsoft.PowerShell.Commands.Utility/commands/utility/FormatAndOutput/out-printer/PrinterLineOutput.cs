#if !CORECLR
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Implementation of the LineOutput interface for printer
    /// </summary>
    internal sealed class PrinterLineOutput : LineOutput
    {

        #region LineOutput implementation

        /// <summary>
        /// full buffering for printer
        /// </summary>
        internal override bool RequiresBuffering { get { return true; } }

        /// <summary>
        /// do the printing on playback
        /// </summary>
        internal override void ExecuteBufferPlayBack (DoPlayBackCall playback) 
        {
            this.playbackCall = playback;
            DoPrint ();
        }

        /// <summary>
        /// the # of columns for the printer
        /// </summary>
        /// <value></value>
        internal override int ColumnNumber
        {
            get
            {
                CheckStopProcessing();
                return this.deviceColumns;
            }
        }

        /// <summary>
        /// the # of rows for the printer
        /// </summary>
        /// <value></value>
        internal override int RowNumber
        {
            get
            {
                CheckStopProcessing();
                return this.deviceRows;
            }
        }

        /// <summary>
        /// write a line to the output device
        /// </summary>
        /// <param name="s">line to write</param>
        internal override void WriteLine (string s)
        {
            CheckStopProcessing();
            // delegate the action to the helper,
            // that will properly break the string into
            // screen lines
            this.writeLineHelper.WriteLine (s, this.ColumnNumber);
        }
        #endregion

        /// <summary>
        /// Used for static initializations like DefaultPrintFontName
        /// </summary>
        static PrinterLineOutput()
        {
            // This default must be loaded from a resource file as different
            // cultures will have different defaults and the localizer would
            // know the default for different cultures.
            DefaultPrintFontName = OutPrinterDisplayStrings.DefaultPrintFontName;
        }

        /// <summary>
        /// constructor for the class
        /// </summary>
        /// <param name="printerName">name of printer, if null use default printer</param>
        internal PrinterLineOutput (string printerName)
        {
            this.printerName = printerName;

            // instantiate the helper to do the line processing when LineOutput.WriteXXX() is called
            WriteLineHelper.WriteCallback wl = new WriteLineHelper.WriteCallback (this.OnWriteLine);
            WriteLineHelper.WriteCallback w = new WriteLineHelper.WriteCallback (this.OnWrite);

            this.writeLineHelper = new WriteLineHelper (true, wl, w, this.DisplayCells);
        }

        /// <summary>
        /// callback to be called when IConsole.WriteLine() is called by WriteLineHelper 
        /// </summary>
        /// <param name="s">string to write</param>
        private void OnWriteLine (string s)
        {
            this.lines.Enqueue (s);
        }

        /// <summary>
        /// callback to be called when Console.Write() is called by WriteLineHelper
        /// This is called when the WriteLineHelper needs to write a line whose length
        /// is the same as the width of the screen buffer
        /// </summary>
        /// <param name="s">string to write</param>
        private void OnWrite (string s)
        {
            this.lines.Enqueue (s);
        }

        /// <summary>
        /// do the printing
        /// </summary>
        private void DoPrint ()
        {
            try
            {
                // create a new print document object and set the printer name, if available
                PrintDocument pd = new PrintDocument ();

                if (!string.IsNullOrEmpty (this.printerName))
                {
                    pd.PrinterSettings.PrinterName = this.printerName;
                }

                // set up the callaback mechanism
                pd.PrintPage += new PrintPageEventHandler (this.pd_PrintPage);

                // start printing
                pd.Print ();
            }
            finally
            {
                // make sure we do not leak the font
                if (this.printFont != null)
                {
                    this.printFont.Dispose ();
                    this.printFont = null;
                }
            }
        }

        /// <summary>
        /// helper to create a font. 
        /// If the font object exists, it does nothing.
        /// Else, the a new object is created and verified
        /// </summary>
        /// <param name="g">GDI+ graphics object needed for verification</param>
        private void CreateFont (Graphics g)
        {
            if (this.printFont != null)
                return;

            // create the font
            
            // do we have a specified font?
            if (string.IsNullOrEmpty (this.printFontName))
            {
                this.printFontName = DefaultPrintFontName;
            }

            if (this.printFontSize <= 0)
            {
                this.printFontSize = DefaultPrintFontSize;
            }

            this.printFont = new Font (this.printFontName, this.printFontSize);
            VerifyFont (g);
        }

        /// <summary>
        /// internal helper to verify that the font is fixed pitch. If the test fails,
        /// it reverts to the default font
        /// </summary>
        /// <param name="g">GDI+ graphics object needed for verification</param>
        private void VerifyFont (Graphics g)
        {
            // check if the font is fixed pitch
            // HEURISTICS:
            // we compute the length of two strings, one made of "large" characters
            // one made of "narrow" ones. If they are the same length, we assume that
            // the font is fixed pitch.
            string large = "ABCDEF";
            float wLarge = g.MeasureString (large, this.printFont).Width / large.Length;
            string narrow = ".;'}l|";
            float wNarrow = g.MeasureString (narrow, this.printFont).Width / narrow.Length;

            if (Math.Abs((float)(wLarge - wNarrow)) < 0.001F)
            {
                // we passed the test
                return;
            }

            // just get back to the default, since it's not fixed pitch
            this.printFont.Dispose ();
            this.printFont = new Font (DefaultPrintFontName, DefaultPrintFontSize);
        }

        /// <summary>
        /// Event fired for each page to print 
        /// </summary>
        /// <param name="sender">sender, not used</param>
        /// <param name="ev">print page event</param>
        private void pd_PrintPage (object sender, PrintPageEventArgs ev)
        {
            float yPos = 0; // GDI+ coordinate down the page
            int linesPrinted = 0; // linesPrinted
            float leftMargin = ev.MarginBounds.Left;
            float topMargin = ev.MarginBounds.Top;

            CreateFont (ev.Graphics);

            // compute the height of a line of text
            float lineHeight = this.printFont.GetHeight (ev.Graphics);

            // Work out the number of lines per page
            // Use the MarginBounds on the event to do this
            float linesPerPage = ev.MarginBounds.Height / this.printFont.GetHeight (ev.Graphics);

            if (!this.printingInitalized)
            {
                // on the first page we have to initialize the metrics for LineOutput

                // work out the number of columns per page assuming fixed pitch font
                string s = "ABCDEF";
                float w = ev.Graphics.MeasureString (s, printFont).Width / s.Length;
                float columnsPerPage = ev.MarginBounds.Width / w;

                this.printingInitalized = true;
                this.deviceRows = (int)linesPerPage;
                this.deviceColumns = (int)columnsPerPage;

                // now that we initialized the column and row count for the LineOutput
                // interface we can tell the outputter to playback from cache to do the
                // proper computations of line widths
                // returning from this call, the string queue on this object is full of
                // lines of text to print
                this.playbackCall ();
            }

            // now iterate over the file printing out each line
            while ((linesPrinted < linesPerPage) && (this.lines.Count > 0))
            {
                // get the string to be printed
                String line = this.lines.Dequeue ();

                // compute the Y position where to draw
                yPos = topMargin + (linesPrinted * lineHeight);

                // do the actual drawing
                ev.Graphics.DrawString (line, printFont, Brushes.Black, leftMargin, yPos, new StringFormat ());
                linesPrinted++;
            }

            //If we have more lines then print another page
            ev.HasMorePages = this.lines.Count > 0;
        }

       
        /// <summary>
        /// flag for one time initialization of the interface (columns, etc.)
        /// </summary>
        private bool printingInitalized = false;

        /// <summary>
        /// callback to ask the outputter to playback its cache
        /// </summary>
        private DoPlayBackCall playbackCall;

        /// <summary>
        /// name of the printer to print to. Null means default printer
        /// </summary>
        private string printerName = null;

        /// <summary>
        /// name of the font to use, if null the default is used
        /// </summary>
        private string printFontName = null;

        /// <summary>
        /// font size
        /// </summary>
        private int printFontSize = 0;


        /// <summary>
        /// default font, used if the printFont is not specified or if the
        /// printFont is not fixed pitch. 
        /// </summary>
        /// <remarks>
        /// This default must be loaded from a resource file as different
        /// cultures will have different defaults and the localizer would
        /// know the default for different cultures.
        /// </remarks>
        private static readonly string DefaultPrintFontName;

        /// <summary>
        /// default size for the default font
        /// </summary>
        const int DefaultPrintFontSize = 8;

        /// <summary>
        /// number of columns on the sheet
        /// </summary>
        private int deviceColumns = 80;
        
        // number of rows per sheet
        private int deviceRows = 40;

        /// <summary>
        /// text lines ready to print (after output cache playback)
        /// </summary>
        private Queue<string> lines = new Queue<string> ();

        /// <summary>
        /// cached font object
        /// </summary>
        private Font printFont = null;
        
        private WriteLineHelper writeLineHelper;
    }

}

#endif
