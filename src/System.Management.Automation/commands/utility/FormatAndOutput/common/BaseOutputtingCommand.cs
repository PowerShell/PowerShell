/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// OutCommand base implementation
    /// it manages the formatting protocol and it writes to a generic
    /// screen host
    /// </summary>
    class OutCommandInner : ImplementationCommandBase
    {
        #region tracer
        [TraceSource ("format_out_OutCommandInner", "OutCommandInner")]
        internal static PSTraceSource tracer = PSTraceSource.GetTracer ("format_out_OutCommandInner", "OutCommandInner");
        #endregion tracer

        internal override void BeginProcessing()
        {
            base.BeginProcessing ();

            this.formatObjectDeserializer = new FormatObjectDeserializer (this.TerminatingErrorContext);

            // hook up the event handlers for the context manager object
            this.ctxManager.contextCreation = new FormatMessagesContextManager.FormatContextCreationCallback (this.CreateOutputContext);
            this.ctxManager.fs = new FormatMessagesContextManager.FormatStartCallback (this.ProcessFormatStart);
            this.ctxManager.fe = new FormatMessagesContextManager.FormatEndCallback (this.ProcessFormatEnd);
            this.ctxManager.gs = new FormatMessagesContextManager.GroupStartCallback (this.ProcessGroupStart);
            this.ctxManager.ge = new FormatMessagesContextManager.GroupEndCallback (this.ProcessGroupEnd);
            this.ctxManager.payload = new FormatMessagesContextManager.PayloadCallback (this.ProcessPayload);
        }

        /// <summary>
        /// execution entry point override
        /// we assume that a LineOutput interface instance already has been acquired
        /// 
        /// IMPORTANT: it assumes the presence of a pre-processing formatting command
        /// </summary>
        internal override void ProcessRecord()
        {
            PSObject so = this.ReadObject ();

            if (so == null || so == AutomationNull.Value)
                return;

            // try to process the object
            if (ProcessObject (so))
                return;

            // send to the formatter for preprocessing
            Array results = ApplyFormatting (so);

            if (results != null)
            {
                foreach (object r in results)
                {
                    PSObject obj = PSObjectHelper.AsPSObject(r);

                    obj.IsHelpObject = so.IsHelpObject;

                    ProcessObject(obj);
                }
            }
        }

        internal override void EndProcessing ()
        {
            base.EndProcessing ();
            if (this.command != null)
            {
                // shut down the command processor, if we ever used it
                Array results = this.command.ShutDown ();

                if (results != null)
                {
                    foreach (object o in results)
                    {
                        ProcessObject(PSObjectHelper.AsPSObject(o));
                    }
                }
            }

            if (this.LineOutput.RequiresBuffering)
            {
                // we need to notify the interface implementor that
                // we are about to do the playback
                LineOutput.DoPlayBackCall playBackCall = new LineOutput.DoPlayBackCall (this.DrainCache);

                this.LineOutput.ExecuteBufferPlayBack (playBackCall);
            }
            else
            {
                // we drain the cache ourselves
                DrainCache ();
            }
        }

        private void DrainCache()
        {
            if (this.cache != null)
            {
                // drain the cache, since we are shutting down
                List<PacketInfoData> unprocessedObjects = this.cache.Drain ();

                if (unprocessedObjects != null)
                {
                    foreach (object obj in unprocessedObjects)
                    {
                        ctxManager.Process (obj);
                    }
                }
            }
        }


        private bool ProcessObject(PSObject so)
        {
            object o = this.formatObjectDeserializer.Deserialize (so);

            //Console.WriteLine("OutCommandInner.Execute() retrieved object {0}, of type {1}", o.ToString(), o.GetType());
            if (NeedsPreprocessing(o))
            {
                return false;
            }
            
            // instantiate the cache if not done yet
            if (this.cache == null)
            {
                this.cache = new FormattedObjectsCache(this.LineOutput.RequiresBuffering);
            }

            // no need for formatting, just process the object
            FormatStartData formatStart = o as FormatStartData;

            if (formatStart != null)
            {
                // get autosize flag from object
                // turn on group caching
                if (formatStart.autosizeInfo != null)
                {
                    FormattedObjectsCache.ProcessCachedGroupNotification callBack = new FormattedObjectsCache.ProcessCachedGroupNotification(ProcessCachedGroup);
                    this.cache.EnableGroupCaching(callBack, formatStart.autosizeInfo.objectCount);
                }
                else
                {
                    // If the format info doesn't define column widths, then auto-size based on the first ten elements
                    TableHeaderInfo headerInfo = formatStart.shapeInfo as TableHeaderInfo;
                    if ((headerInfo != null) &&
                        (headerInfo.tableColumnInfoList.Count > 0) &&
                        (headerInfo.tableColumnInfoList[0].width == 0))
                    {
                        FormattedObjectsCache.ProcessCachedGroupNotification callBack = new FormattedObjectsCache.ProcessCachedGroupNotification(ProcessCachedGroup);
                        this.cache.EnableGroupCaching(callBack, TimeSpan.FromMilliseconds(300));
                    }
                }
            }

            //Console.WriteLine("OutCommandInner.Execute() calling ctxManager.Process({0})",o.ToString());
            List<PacketInfoData> info = this.cache.Add((PacketInfoData)o);

            if (info != null)
            {
                for (int k = 0; k < info.Count; k++)
                    ctxManager.Process(info[k]);
            }
            return true;
        }
     
        /// <summary>
        /// helper to return what shape we have to use to format the output
        /// </summary>
        private FormatShape ActiveFormattingShape
        {
            get 
            {
                // we assume that the format context
                // contains the information
                FormatShape shape = FormatShape.Table; // default
                FormatOutputContext foc = this.FormatContext;

                if (foc == null || foc.Data.shapeInfo == null)
                    return shape;

                if (foc.Data.shapeInfo is TableHeaderInfo)
                    return FormatShape.Table;

                if (foc.Data.shapeInfo is ListViewHeaderInfo)
                    return FormatShape.List;

                if (foc.Data.shapeInfo is WideViewHeaderInfo)
                    return FormatShape.Wide;

                if (foc.Data.shapeInfo is ComplexViewHeaderInfo)
                    return FormatShape.Complex;

                return shape;
            }
        }

        protected override void InternalDispose ()
        {
            base.InternalDispose ();
            if (this.command != null)
            {
                this.command.Dispose ();
                this.command = null;
            }
        }


        /// <summary>
        /// enum describing the state for the output finite state machine
        /// </summary>
        private enum FormattingState 
        { 
            /// <summary>
            /// we are in the clear state: no formatting in process
            /// </summary>
            Reset, 
            
            /// <summary>
            /// we received a Format Start message, but we are not inside a group
            /// </summary>
            Formatting, 
            
            /// <summary>
            /// we are inside a group because we received a Group Start
            /// </summary>
            InsideGroup 
        }

        /// <summary>
        /// toggle to signal if we are in a formatting sequence
        /// </summary>
        private FormattingState currentFormattingState = FormattingState.Reset;


        /// <summary>
        /// instance of a command wrapper to execute the 
        /// default formatter when needed
        /// </summary>
        private CommandWrapper command;
 

        /// <summary>
        /// enumeration to drive the preprocessing stage
        /// </summary>
        private enum PreprocessingState { raw, processed, error }

        /// <summary>
        /// test if an object coming from the pipeline needs to be
        /// preprocessed by the default formatter
        /// </summary>
        /// <param name="o">object to examine for formatting</param>
        /// <returns>whether the object needs to be shunted to preprocessing</returns>
        private bool NeedsPreprocessing(object o)
        {
            FormatEntryData fed = o as FormatEntryData;
            if (fed != null)
            {
                // we got an already pre-processed object
                if (!fed.outOfBand)
                {
                    // we allow out of band data in any state
                    ValidateCurrentFormattingState(FormattingState.InsideGroup, o);
                }
                return false;
            }
            else if (o is FormatStartData)
            {
                // when encountering FormatStartDate out of sequemce,
                // pretend that the previous formatting directives were properly closed
                if (this.currentFormattingState == FormattingState.InsideGroup)
                {
                    this.EndProcessing();
                    this.BeginProcessing();
                }

                // we got a Fs message, we enter a sequence
                ValidateCurrentFormattingState(FormattingState.Reset, o);
                this.currentFormattingState = FormattingState.Formatting;

                return false;
            }
            else if (o is FormatEndData)
            {
                // we got a Fe message, we exit a sequence
                ValidateCurrentFormattingState(FormattingState.Formatting, o);
                this.currentFormattingState = FormattingState.Reset;

                return false;
            }
            else if (o is GroupStartData)
            {
                ValidateCurrentFormattingState(FormattingState.Formatting, o);
                this.currentFormattingState = FormattingState.InsideGroup;

                return false;
            }
            else if (o is GroupEndData)
            {
                ValidateCurrentFormattingState(FormattingState.InsideGroup, o);
                this.currentFormattingState = FormattingState.Formatting;

                return false;
            }

            // this is a raw object
            return true;
        }

        private void ValidateCurrentFormattingState(FormattingState expectedFormattingState, object obj)
        {
            // chec if we are in the expected formatting state
            if (this.currentFormattingState != expectedFormattingState)
            {
                // we are not in the expected state, some message is out of sequence,
                // need to abort the command

                string violatingCommand = "format-*";
                StartData sdObj = obj as StartData;
                if (sdObj != null)
                {
                    if (sdObj.shapeInfo.GetType() == typeof(WideViewHeaderInfo))
                    {
                        violatingCommand = "format-wide";
                    }
                    else if (sdObj.shapeInfo.GetType() == typeof(TableHeaderInfo))
                    {
                        violatingCommand = "format-table";
                    }
                    else if (sdObj.shapeInfo.GetType() == typeof(ListViewHeaderInfo))
                    {
                        violatingCommand = "format-list";
                    }
                    else if (sdObj.shapeInfo.GetType() == typeof(ComplexViewHeaderInfo))
                    {
                        violatingCommand = "format-complex";
                    }
                }

                string msg = StringUtil.Format(FormatAndOut_out_xxx.OutLineOutput_OutOfSequencePacket,
                    obj.GetType().FullName, violatingCommand);

                ErrorRecord errorRecord = new ErrorRecord (
                                                new InvalidOperationException(),
                                                "ConsoleLineOutputOutOfSequencePacket",
                                                ErrorCategory.InvalidData,
                                                null);

                errorRecord.ErrorDetails = new ErrorDetails (msg);
                this.TerminatingErrorContext.ThrowTerminatingError(errorRecord);
            }
        }

        /// <summary>
        /// shunt object to the formatting pipeline for preprocessing
        /// </summary>
        /// <param name="o">object to be preprocessed</param>
        /// <returns>array of objects returned by the preprocessing step</returns>
        private Array ApplyFormatting (object o)
        {
            if (this.command == null)
            {
                this.command = new CommandWrapper ();
                this.command.Initialize(this.OuterCmdlet().Context, "format-default", typeof(FormatDefaultCommand));
            }

            return this.command.Process (o);
        }

        /// <summary>
        /// class factory for output context
        /// </summary>
        /// <param name="parentContext">parent context in the stack</param>
        /// <param name="formatInfoData"> fromat info data received from the pipeline</param>
        /// <returns></returns>
        private FormatMessagesContextManager.OutputContext CreateOutputContext(
                                        FormatMessagesContextManager.OutputContext parentContext,
                                        FormatInfoData formatInfoData)
        {
            FormatStartData formatStartData = formatInfoData as FormatStartData;
            // initialize the format context 
            if (formatStartData != null)
            {
                FormatOutputContext foc = new FormatOutputContext(parentContext, formatStartData);

                return foc;
            }

            GroupStartData gsd = formatInfoData as GroupStartData;
            // we are starting a group, initialize the group context
            if (gsd != null)
            {
                GroupOutputContext goc = null;

                switch (ActiveFormattingShape)
                {
                    case FormatShape.Table:
                        {
                            goc = new TableOutputContext (this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.List:
                        {
                            goc = new ListOutputContext (this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.Wide:
                        {
                            goc = new WideOutputContext (this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.Complex:
                        {
                            goc = new ComplexOutputContext (this, parentContext, gsd);
                            break;
                        }

                    default:
                        {
                            Diagnostics.Assert (false, "Invalid shape. This should never happen");
                        }
                        break;
                }
                goc.Initialize ();
                return goc;
            }

            return null;
        }

        /// <summary>
        /// callback for Fs processing
        /// </summary>
        /// <param name="c">the context containing the Fs entry</param>
        private void ProcessFormatStart(FormatMessagesContextManager.OutputContext c)
        {
            // we just add an empty line to the display
            this.LineOutput.WriteLine ("");
        }

        /// <summary>
        /// callback for Fe processing
        /// </summary>
        /// <param name="fe">Fe notification message</param>
        /// <param name="c">current context, with Fs in it</param>
        private void ProcessFormatEnd(FormatEndData fe, FormatMessagesContextManager.OutputContext c)
        {
            //Console.WriteLine("ProcessFormatEnd");
            // we just add an empty line to the display
            this.LineOutput.WriteLine ("");
        }

        /// <summary>
        /// callback for Gs processing
        /// </summary>
        /// <param name="c">the context containing the Gs entry</param>
        private void ProcessGroupStart(FormatMessagesContextManager.OutputContext c)
        {
            //Console.WriteLine("ProcessGroupStart");
            GroupOutputContext goc = (GroupOutputContext)c;


            if (goc.Data.groupingEntry != null)
            {
                this.lo.WriteLine ("");

                ComplexWriter writer = new ComplexWriter ();
                writer.Initialize (this.lo, this.lo.ColumnNumber);
                writer.WriteObject (goc.Data.groupingEntry.formatValueList);

                this.LineOutput.WriteLine ("");
            }
            goc.GroupStart ();
        }

        /// <summary>
        /// callback for Ge processing
        /// </summary>
        /// <param name="ge">Ge notification message</param>
        /// <param name="c">current context, with Gs in it</param>
        private void ProcessGroupEnd(GroupEndData ge, FormatMessagesContextManager.OutputContext c)
        {
            //Console.WriteLine("ProcessGroupEnd");
            GroupOutputContext goc = (GroupOutputContext)c;

            goc.GroupEnd ();
            this.LineOutput.WriteLine ("");
        }

        /// <summary>
        /// process the current payload object
        /// </summary>
        /// <param name="fed">FormatEntryData to process</param>
        /// <param name="c">currently active context</param>
        private void ProcessPayload(FormatEntryData fed, FormatMessagesContextManager.OutputContext c)
        {
            // we assume FormatEntryData as a standard wrapper
            if (fed == null)
            {
                PSTraceSource.NewArgumentNullException ("fed");
            }
            if (fed.formatEntryInfo == null)
            {
                PSTraceSource.NewArgumentNullException ("fed.formatEntryInfo");
            }

            WriteStreamType oldWSState = this.lo.WriteStream;
            try
            {
                this.lo.WriteStream = fed.writeStream;

                if (c == null)
                {
                    ProcessOutOfBandPayload(fed);
                }
                else
                {
                    GroupOutputContext goc = (GroupOutputContext)c;

                    goc.ProcessPayload(fed);
                }
            }
            finally
            {
                this.lo.WriteStream = oldWSState;
            }
        }

        private void ProcessOutOfBandPayload (FormatEntryData fed)
        {
            // try if it is raw text
            RawTextFormatEntry rte = fed.formatEntryInfo as RawTextFormatEntry;
            if (rte != null)
            {
                if (fed.isHelpObject)
                {
                    ComplexWriter complexWriter = new ComplexWriter();

                    complexWriter.Initialize(this.lo, this.lo.ColumnNumber);
                    complexWriter.WriteString(rte.text);
                }
                else
                {
                    this.lo.WriteLine(rte.text);

                }

                return;
            }

            // try if it is a complex entry
            ComplexViewEntry cve = fed.formatEntryInfo as ComplexViewEntry;
            if (cve != null && cve.formatValueList != null)
            {
                ComplexWriter complexWriter = new ComplexWriter ();

                complexWriter.Initialize (this.lo, this.lo.ColumnNumber);
                complexWriter.WriteObject (cve.formatValueList);

                return;
            }
            // try if it is a list view
            ListViewEntry lve = fed.formatEntryInfo as ListViewEntry;
            if (lve != null && lve.listViewFieldList != null)
            {
                ListWriter listWriter = new ListWriter ();

                this.lo.WriteLine ("");

                string[] properties = ListOutputContext.GetProperties (lve);
                listWriter.Initialize (properties, this.lo.ColumnNumber, this.lo.DisplayCells);
                string[] values = ListOutputContext.GetValues (lve);
                listWriter.WriteProperties (values, this.lo);
                
                this.lo.WriteLine ("");

                return;
            }
        }

        /// <summary>
        /// the screen host associated with this outputter
        /// </summary>
        private LineOutput lo = null;

        internal LineOutput LineOutput
        {
            set { this.lo = value; }
            get { return this.lo; }
        }

        private ShapeInfo ShapeInfoOnFormatContext
        {
            get
            {
                FormatOutputContext foc = this.FormatContext;

                if (foc == null)
                    return null;

                return foc.Data.shapeInfo;
            }
        }
      

        /// <summary>
        /// retrieve the active FormatOutputContext on the stack
        /// by walking up to the top of the stack
        /// </summary>
        private FormatOutputContext FormatContext
        {
            get
            {
                for (FormatMessagesContextManager.OutputContext oc = ctxManager.ActiveOutputContext; oc != null; oc = oc.ParentContext)
                {
                    FormatOutputContext foc = oc as FormatOutputContext;

                    if (foc != null)
                        return foc;
                }

                return null;
            }
        }


        /// <summary>
        /// context manager instance to guide the message traversal
        /// </summary>
        private FormatMessagesContextManager ctxManager = new FormatMessagesContextManager();

        private FormattedObjectsCache cache = null;

        /// <summary>
        /// handler for processing the caching notification and responsible for 
        /// setting the value of the formatting hint
        /// </summary>
        /// <param name="formatStartData"></param>
        /// <param name="objects"></param>
        private void ProcessCachedGroup (FormatStartData formatStartData, List<PacketInfoData> objects)
        {
            this.formattingHint = null;

            TableHeaderInfo thi = formatStartData.shapeInfo as TableHeaderInfo;

            if (thi != null)
            {
                ProcessCachedGroupOnTable (thi, objects);
                return;
            }

            WideViewHeaderInfo wvhi = formatStartData.shapeInfo as WideViewHeaderInfo;

            if (wvhi != null)
            {
                ProcessCachedGroupOnWide (wvhi, objects);
                return;
            }
        }

        private void ProcessCachedGroupOnTable (TableHeaderInfo thi, List<PacketInfoData> objects)
        {
            if (thi.tableColumnInfoList.Count == 0)
                return;

            int[] widths = new int[thi.tableColumnInfoList.Count];

            for (int k = 0; k < thi.tableColumnInfoList.Count; k++)
            {
                string label = thi.tableColumnInfoList[k].label;

                if (string.IsNullOrEmpty (label))
                    label = thi.tableColumnInfoList[k].propertyName;

                if (string.IsNullOrEmpty(label))
                    widths[k] = 0;
                else
                    widths[k] = this.lo.DisplayCells.Length(label);
            }

            int cellCount; // scratch variable
            foreach (PacketInfoData o in objects)
            {
                FormatEntryData fed = o as FormatEntryData;

                if (fed == null)
                    continue;

                TableRowEntry tre = fed.formatEntryInfo as TableRowEntry;
                int kk = 0;

                foreach (FormatPropertyField fpf in tre.formatPropertyFieldList)
                {
                    cellCount = this.lo.DisplayCells.Length(fpf.propertyValue);
                    if (widths[kk] < cellCount)
                        widths[kk] = cellCount;

                    kk++;
                }
            }

            TableFormattingHint hint = new TableFormattingHint ();

            hint.columnWidths = widths;
            this.formattingHint = hint;
        }

        private void ProcessCachedGroupOnWide (WideViewHeaderInfo wvhi, List<PacketInfoData> objects)
        {
            if (wvhi.columns != 0)
            {
                // columns forced on the client
                return;
            }

            int maxLen = 0;
            int cellCount; // scratch variable

            foreach (PacketInfoData o in objects)
            {
                FormatEntryData fed = o as FormatEntryData;

                if (fed == null)
                    continue;

                WideViewEntry wve = fed.formatEntryInfo as WideViewEntry;
                FormatPropertyField fpf = wve.formatPropertyField as FormatPropertyField;

                if (!string.IsNullOrEmpty (fpf.propertyValue))
                {
                    cellCount = this.lo.DisplayCells.Length(fpf.propertyValue);
                    if (cellCount > maxLen)
                        maxLen = cellCount;
                }
            }

            WideFormattingHint hint = new WideFormattingHint ();

            hint.maxWidth = maxLen;
            this.formattingHint = hint;
        }

        /// <summary>
        /// base class for all the formatting hints
        /// </summary>
        private abstract class FormattingHint 
        {
        }

        /// <summary>
        /// hint for format-table
        /// </summary>
        private sealed class TableFormattingHint : FormattingHint
        {
            internal int [] columnWidths = null;
        }

        /// <summary>
        /// hint for format-wide
        /// </summary>
        private sealed class WideFormattingHint : FormattingHint
        {
            internal int maxWidth = 0;
        }

        /// <summary>
        /// variable holding the autosize hint (set by the caching code and reset by the hint consumer
        /// </summary>
        private FormattingHint formattingHint = null;

        /// <summary>
        /// helper for consuming the formatting hint
        /// </summary>
        /// <returns></returns>
        private FormattingHint RetrieveFormattingHint()
        {
            FormattingHint fh = this.formattingHint;
            this.formattingHint = null;
            return fh;
        }

        private FormatObjectDeserializer formatObjectDeserializer;


        /// <summary>
        /// context for the outer scope of the format sequence
        /// </summary>
        private class FormatOutputContext : FormatMessagesContextManager.OutputContext
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal FormatOutputContext (FormatMessagesContextManager.OutputContext parentContext, FormatStartData formatData)
                : base(parentContext)
            {
                this.formatData = formatData;
            }

            /// <summary>
            /// retrieve the format data in the context
            /// </summary>
            internal FormatStartData Data { get { return this.formatData; } }

            /// <summary>
            /// the active formatting message, as obtained from the stream
            /// </summary>
            private FormatStartData formatData = null;
        }

        /// <summary>
        /// context for the currently active group
        /// </summary>
        private abstract class GroupOutputContext : FormatMessagesContextManager.OutputContext
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            internal GroupOutputContext(OutCommandInner cmd,
                                    FormatMessagesContextManager.OutputContext parentContext, 
                                    GroupStartData formatData) 
                : base(parentContext)
            {
                this.cmd = cmd;
                this.formatData = formatData;
            }

            /// <summary>
            /// called at creation time, overrides will initialize here, e.g.
            /// column widths, etc.
            /// </summary>
            internal virtual void Initialize () { }

            /// <summary>
            /// called when a group of data is started, overrided will do
            /// things such as headers, etc...
            /// </summary>
            internal virtual void GroupStart () { }

            /// <summary>
            /// called when the end of a group is reached, overrides will do
            /// things such as group footers
            /// </summary>
            internal virtual void GroupEnd () { }

            /// <summary>
            /// called when there is an entry to process, overrides will do
            /// things such as writing a row in a table
            /// </summary>
            /// <param name="fed">FormatEntryData to process</param>
            internal virtual void ProcessPayload (FormatEntryData fed) { }

            /// <summary>
            /// retrieve the format data in the context
            /// </summary>
            internal GroupStartData Data { get { return this.formatData; } }


            protected OutCommandInner InnerCommand
            {
                get { return this.cmd; } 
            }

            /// <summary>
            /// OutCommandInner reference, for accessing context data
            /// </summary>
            private OutCommandInner cmd;

            /// <summary>
            /// the active formatting message, as obtained from the stream
            /// </summary>
            private GroupStartData formatData = null;

        }

        private class TableOutputContextBase : GroupOutputContext
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="cmd">reference to the OutCommandInner instance who owns this instance</param>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal TableOutputContextBase(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext, 
                GroupStartData formatData) 
                : base(cmd, parentContext, formatData)
            {
            }

            /// <summary>
            /// Get the table writer for this context
            /// </summary>
            protected TableWriter Writer { get { return this.tableWriter; } }

            /// <summary>
            /// helper class to properly write a table using text output
            /// </summary>
            private TableWriter tableWriter = new TableWriter();


        }

        private sealed class TableOutputContext : TableOutputContextBase
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="cmd">reference to the OutCommandInner instance who owns this instance</param>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal TableOutputContext (OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext, 
                GroupStartData formatData) 
                : base(cmd, parentContext, formatData)
            {
            }

            /// <summary>
            /// initialize column widths
            /// </summary>
            internal override void Initialize ()
            {
                TableFormattingHint tableHint = this.InnerCommand.RetrieveFormattingHint () as TableFormattingHint;
                int[] columnWidthsHint = null;

                if (tableHint != null)
                {
                    columnWidthsHint = tableHint.columnWidths;
                }

                int columsOnTheScreen = this.InnerCommand.lo.ColumnNumber;

                int columns = this.CurrentTableHeaderInfo.tableColumnInfoList.Count;
                if (columns == 0)
                {
                    return;
                }
                
                // create arrays for widths and alignment
                int[] columnWidths = new int[columns];
                int[] alignment = new int[columns];
                int k = 0;

                foreach (TableColumnInfo tci in this.CurrentTableHeaderInfo.tableColumnInfoList)
                {
                    columnWidths[k] = (columnWidthsHint != null) ? columnWidthsHint[k] : tci.width;
                    alignment[k] = tci.alignment;
                    k++;
                }
                this.Writer.Initialize (0, columsOnTheScreen, columnWidths, alignment, this.CurrentTableHeaderInfo.hideHeader);
            }

            /// <summary>
            /// write the headers
            /// </summary>
            internal override void GroupStart ()
            {
                int columns = this.CurrentTableHeaderInfo.tableColumnInfoList.Count;

                if (columns == 0)
                {
                    return;
                }

                string[] properties = new string[columns];
                int k = 0;
                foreach (TableColumnInfo tci in this.CurrentTableHeaderInfo.tableColumnInfoList)
                {
                    properties[k++] = (tci.label != null) ? tci.label : tci.propertyName;
                }
                this.Writer.GenerateHeader(properties ,this.InnerCommand.lo);
            }

            /// <summary>
            /// write a row into the table
            /// </summary>
            /// <param name="fed">FormatEntryData to process</param>
            internal override void ProcessPayload (FormatEntryData fed)
            {
                int headerColumns = this.CurrentTableHeaderInfo.tableColumnInfoList.Count;

                if (headerColumns == 0)
                {
                    return;
                }

                TableRowEntry tre = fed.formatEntryInfo as TableRowEntry;

                // need to make sure we have matching counts: the header count will have to prevail
                string[] values = new string[headerColumns];
                int[] alignment = new int[headerColumns];

                int fieldCount = tre.formatPropertyFieldList.Count;
                
                for (int k = 0; k < headerColumns; k++)
                {
                    if (k < fieldCount)
                    {
                        values[k] = tre.formatPropertyFieldList[k].propertyValue;
                        alignment[k] = tre.formatPropertyFieldList[k].alignment;
                    }
                    else
                    {
                        values[k] = "";
                        alignment[k] = TextAlignment.Left; // hard coded default
                    }
                }
                this.Writer.GenerateRow (values, this.InnerCommand.lo, tre.multiLine, alignment, InnerCommand.lo.DisplayCells);
           }

            private TableHeaderInfo CurrentTableHeaderInfo
            {
                get
                {
                    return (TableHeaderInfo)this.InnerCommand.ShapeInfoOnFormatContext;
                }
            }

        }

        private sealed class ListOutputContext : GroupOutputContext
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="cmd">reference to the OutCommandInner instance who owns this instance</param>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal ListOutputContext (OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext, 
                GroupStartData formatData) 
                : base(cmd, parentContext, formatData)
            {
            }

            /// <summary>
            /// initialize column widths
            /// </summary>
            internal override void Initialize ()
            {
            }

            private void InternalInitialize (ListViewEntry lve)
            {
                this.properties = GetProperties (lve);
                this.listWriter.Initialize (this.properties, this.InnerCommand.lo.ColumnNumber, InnerCommand.lo.DisplayCells);
            }

            internal static string[] GetProperties (ListViewEntry lve)
            {
                StringCollection props = new StringCollection ();
                foreach (ListViewField lvf in lve.listViewFieldList)
                {
                    props.Add ((lvf.label != null) ? lvf.label : lvf.propertyName);
                }
                if (props.Count == 0)
                    return null;
                string[] retVal = new string[props.Count];
                props.CopyTo (retVal, 0);
                return retVal;
            }

            internal static string[] GetValues (ListViewEntry lve)
            {
                StringCollection vals = new StringCollection ();

                foreach (ListViewField lvf in lve.listViewFieldList)
                {
                    vals.Add (lvf.formatPropertyField.propertyValue);
                }
                if (vals.Count == 0)
                    return null;
                string[] retVal = new string[vals.Count];
                vals.CopyTo (retVal, 0);
                return retVal;
            }


            /// <summary>
            /// write the headers
            /// </summary>
            internal override void GroupStart ()
            {
                this.InnerCommand.lo.WriteLine("");
            }

            /// <summary>
            /// write a row into the list
            /// </summary>
            /// <param name="fed">FormatEntryData to process</param>
            internal override void ProcessPayload (FormatEntryData fed)
            {
                ListViewEntry lve = fed.formatEntryInfo as ListViewEntry;
                InternalInitialize (lve);
                string[] values = GetValues (lve);
                this.listWriter.WriteProperties(values, this.InnerCommand.lo);
                this.InnerCommand.lo.WriteLine("");
            }

            /// <summary>
            /// property list currently active
            /// </summary>
            private string[] properties = null;

            /// <summary>
            /// writer to do the actual formatting
            /// </summary>
            private ListWriter listWriter = new ListWriter();
        }

        private sealed class WideOutputContext : TableOutputContextBase
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="cmd">reference to the OutCommandInner instance who owns this instance</param>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal WideOutputContext (OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext, 
                GroupStartData formatData) 
                : base(cmd, parentContext, formatData)
            {
            }

            private StringValuesBuffer buffer = null;

            /// <summary>
            /// initialize column widths
            /// </summary>
            internal override void Initialize ()
            {
                // set the hard wider default, to be used if no other info is available
                int itemsPerRow = 2;

                // get the header info and the wiew hint
                WideFormattingHint hint = this.InnerCommand.RetrieveFormattingHint () as WideFormattingHint;

                // give a preference to the hint, if there
                if (hint != null && hint.maxWidth > 0)
                {
                    itemsPerRow = TableWriter.ComputeWideViewBestItemsPerRowFit(hint.maxWidth, this.InnerCommand.lo.ColumnNumber);
                }
                else if (this.CurrentWideHeaderInfo.columns > 0)
                {
                    itemsPerRow = this.CurrentWideHeaderInfo.columns;
                }

                // create a buffer object to hold partial rows
                buffer = new StringValuesBuffer(itemsPerRow);

                // initialize the writer
                int[] columnWidths = new int[itemsPerRow];
                int[] alignment = new int[itemsPerRow];

                for (int k = 0; k < itemsPerRow; k++)
                {
                    columnWidths[k] = 0; // autosize
                    alignment[k] = TextAlignment.Left;
                }

                this.Writer.Initialize (0, this.InnerCommand.lo.ColumnNumber, columnWidths, alignment, false);
            }

            /// <summary>
            /// write the headers
            /// </summary>
            internal override void GroupStart ()
            {
                this.InnerCommand.lo.WriteLine("");
            }

            /// <summary>
            /// called when the end of a group is reached, flush the
            /// write buffer
            /// </summary>
            internal override void GroupEnd () 
            {
                WriteStringBuffer();
            }

            /// <summary>
            /// write a row into the table
            /// </summary>
            /// <param name="fed">FormatEntryData to process</param>
            internal override void ProcessPayload (FormatEntryData fed)
            {
                WideViewEntry wve = fed.formatEntryInfo as WideViewEntry;
                FormatPropertyField fpf = wve.formatPropertyField as FormatPropertyField;
                buffer.Add (fpf.propertyValue);
                if (buffer.IsFull)
                {
                    WriteStringBuffer ();
                }
            }

            private WideViewHeaderInfo CurrentWideHeaderInfo
            {
                get
                {
                    return (WideViewHeaderInfo)this.InnerCommand.ShapeInfoOnFormatContext;
                }
            }

            private void WriteStringBuffer()
            {
                if (buffer.IsEmpty)
                {
                    return;
                }
            
                string[] values = new string[buffer.Lenght];
                for (int k=0; k< values.Length; k++)
                {
                    if (k < buffer.CurrentCount)
                        values[k] = buffer[k];
                    else
                        values[k] = "";
                }
                this.Writer.GenerateRow(values, this.InnerCommand.lo, false, null, InnerCommand.lo.DisplayCells);
                buffer.Reset();
            }


            /// <summary>
            /// helper class to accumulate the display values so that when the end
            /// of a line is reached, a full line can be composed
            /// </summary>
            private class StringValuesBuffer
            {
                /// <summary>
                /// construct the buffer
                /// </summary>
                /// <param name="size">number of entries to cache</param>
                internal StringValuesBuffer (int size)
                {
                    arr = new string[size];
                    Reset();
                }
                /// <summary>
                /// get the size of the buffer
                /// </summary>
                internal int Lenght { get { return arr.Length; } }

                /// <summary>
                /// get the current number of entries in the buffer
                /// </summary>
                internal int CurrentCount { get { return lastEmptySpot; } }

                /// <summary>
                /// check if the buffer is full
                /// </summary>
                internal bool IsFull
                {
                    get { return lastEmptySpot == arr.Length; }
                }

                /// <summary>
                /// check if the buffer is empty
                /// </summary>
                internal bool IsEmpty
                {
                    get { return lastEmptySpot == 0; }
                }

                /// <summary>
                /// indexer to access the k-th item in the buffer
                /// </summary>
                internal string this[int k] { get { return arr[k]; } }

                /// <summary>
                /// add an item to the buffer
                /// </summary>
                /// <param name="s">string to add</param>
                internal void Add (string s)
                {
                    arr[lastEmptySpot++] = s;
                }

                /// <summary>
                /// reset the buffer
                /// </summary>
                internal void Reset ()
                {
                    lastEmptySpot = 0;
                    for(int k=0; k<arr.Length; k++)
                        arr[k]  = null;
                }

                private string[] arr;
                private int lastEmptySpot;
            }
        }

        private sealed class ComplexOutputContext : GroupOutputContext
        {
            /// <summary>
            /// construct a context to push on the stack
            /// </summary>
            /// <param name="cmd">reference to the OutCommandInner instance who owns this instance</param>
            /// <param name="parentContext">parent context in the stack</param>
            /// <param name="formatData">format data to put in the context</param>
            internal ComplexOutputContext (OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext, 
                GroupStartData formatData) 
                : base(cmd, parentContext, formatData)
            {
            }

            internal override void Initialize ()
            {
                this.writer.Initialize (this.InnerCommand.lo, 
                                    this.InnerCommand.lo.ColumnNumber);
            }


            /// <summary>
            /// write a row into the list
            /// </summary>
            /// <param name="fed">FormatEntryData to process</param>
            internal override void ProcessPayload (FormatEntryData fed)
            {
                ComplexViewEntry cve = fed.formatEntryInfo as ComplexViewEntry;
                if (cve == null || cve.formatValueList == null) 
                    return;
                this.writer.WriteObject (cve.formatValueList);
            }

            ComplexWriter writer = new ComplexWriter();

        }
    
    }       

}

