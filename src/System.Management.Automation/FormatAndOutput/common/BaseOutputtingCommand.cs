// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// OutCommand base implementation
    /// it manages the formatting protocol and it writes to a generic
    /// screen host.
    /// </summary>
    internal class OutCommandInner : ImplementationCommandBase
    {
        #region tracer
        [TraceSource("format_out_OutCommandInner", "OutCommandInner")]
        internal static readonly PSTraceSource tracer = PSTraceSource.GetTracer("format_out_OutCommandInner", "OutCommandInner");
        #endregion tracer

        internal override void BeginProcessing()
        {
            base.BeginProcessing();

            _formatObjectDeserializer = new FormatObjectDeserializer(this.TerminatingErrorContext);

            // hook up the event handlers for the context manager object
            _ctxManager.contextCreation = new FormatMessagesContextManager.FormatContextCreationCallback(this.CreateOutputContext);
            _ctxManager.fs = new FormatMessagesContextManager.FormatStartCallback(this.ProcessFormatStart);
            _ctxManager.fe = new FormatMessagesContextManager.FormatEndCallback(this.ProcessFormatEnd);
            _ctxManager.gs = new FormatMessagesContextManager.GroupStartCallback(this.ProcessGroupStart);
            _ctxManager.ge = new FormatMessagesContextManager.GroupEndCallback(this.ProcessGroupEnd);
            _ctxManager.payload = new FormatMessagesContextManager.PayloadCallback(this.ProcessPayload);
        }

        /// <summary>
        /// Execution entry point override
        /// we assume that a LineOutput interface instance already has been acquired
        ///
        /// IMPORTANT: it assumes the presence of a pre-processing formatting command.
        /// </summary>
        internal override void ProcessRecord()
        {
            PSObject so = this.ReadObject();

            if (so == null || so == AutomationNull.Value)
                return;

            // try to process the object
            if (ProcessObject(so))
                return;

            // send to the formatter for preprocessing
            Array results = ApplyFormatting(so);

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

        internal override void EndProcessing()
        {
            base.EndProcessing();
            if (_command != null)
            {
                // shut down the command processor, if we ever used it
                Array results = _command.ShutDown();

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
                LineOutput.DoPlayBackCall playBackCall = new LineOutput.DoPlayBackCall(this.DrainCache);

                this.LineOutput.ExecuteBufferPlayBack(playBackCall);
            }
            else
            {
                // we drain the cache ourselves
                DrainCache();
            }
        }

        private void DrainCache()
        {
            if (_cache != null)
            {
                // drain the cache, since we are shutting down
                List<PacketInfoData> unprocessedObjects = _cache.Drain();

                if (unprocessedObjects != null)
                {
                    foreach (object obj in unprocessedObjects)
                    {
                        _ctxManager.Process(obj);
                    }
                }
            }
        }

        private bool ProcessObject(PSObject so)
        {
            object o = _formatObjectDeserializer.Deserialize(so);

            // Console.WriteLine("OutCommandInner.Execute() retrieved object {0}, of type {1}", o.ToString(), o.GetType());
            if (NeedsPreprocessing(o))
            {
                return false;
            }

            // instantiate the cache if not done yet
            _cache ??= new FormattedObjectsCache(this.LineOutput.RequiresBuffering);

            // no need for formatting, just process the object
            FormatStartData formatStart = o as FormatStartData;

            if (formatStart != null)
            {
                // get autosize flag from object
                // turn on group caching
                if (formatStart.autosizeInfo != null)
                {
                    FormattedObjectsCache.ProcessCachedGroupNotification callBack = new FormattedObjectsCache.ProcessCachedGroupNotification(ProcessCachedGroup);
                    _cache.EnableGroupCaching(callBack, formatStart.autosizeInfo.objectCount);
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
                        _cache.EnableGroupCaching(callBack, TimeSpan.FromMilliseconds(300));
                    }
                }
            }

            // Console.WriteLine("OutCommandInner.Execute() calling ctxManager.Process({0})",o.ToString());
            List<PacketInfoData> info = _cache.Add((PacketInfoData)o);

            if (info != null)
            {
                for (int k = 0; k < info.Count; k++)
                    _ctxManager.Process(info[k]);
            }

            return true;
        }

        /// <summary>
        /// Helper to return what shape we have to use to format the output.
        /// </summary>
        private FormatShape ActiveFormattingShape
        {
            get
            {
                // we assume that the format context
                // contains the information
                const FormatShape shape = FormatShape.Table; // default
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

        protected override void InternalDispose()
        {
            base.InternalDispose();
            if (_command != null)
            {
                _command.Dispose();
                _command = null;
            }
        }

        /// <summary>
        /// Enum describing the state for the output finite state machine.
        /// </summary>
        private enum FormattingState
        {
            /// <summary>
            /// We are in the clear state: no formatting in process.
            /// </summary>
            Reset,

            /// <summary>
            /// We received a Format Start message, but we are not inside a group.
            /// </summary>
            Formatting,

            /// <summary>
            /// We are inside a group because we received a Group Start.
            /// </summary>
            InsideGroup
        }

        /// <summary>
        /// Toggle to signal if we are in a formatting sequence.
        /// </summary>
        private FormattingState _currentFormattingState = FormattingState.Reset;

        /// <summary>
        /// Instance of a command wrapper to execute the
        /// default formatter when needed.
        /// </summary>
        private CommandWrapper _command;

        /// <summary>
        /// Enumeration to drive the preprocessing stage.
        /// </summary>
        private enum PreprocessingState { raw, processed, error }

        private const int DefaultConsoleWidth = 120;
        private const int DefaultConsoleHeight = int.MaxValue;
        internal const int StackAllocThreshold = 120;

        /// <summary>
        /// Test if an object coming from the pipeline needs to be
        /// preprocessed by the default formatter.
        /// </summary>
        /// <param name="o">Object to examine for formatting.</param>
        /// <returns>Whether the object needs to be shunted to preprocessing.</returns>
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
                // when encountering FormatStartDate out of sequence,
                // pretend that the previous formatting directives were properly closed
                if (_currentFormattingState == FormattingState.InsideGroup)
                {
                    this.EndProcessing();
                    this.BeginProcessing();
                }

                // we got a Fs message, we enter a sequence
                ValidateCurrentFormattingState(FormattingState.Reset, o);
                _currentFormattingState = FormattingState.Formatting;

                return false;
            }
            else if (o is FormatEndData)
            {
                // we got a Fe message, we exit a sequence
                ValidateCurrentFormattingState(FormattingState.Formatting, o);
                _currentFormattingState = FormattingState.Reset;

                return false;
            }
            else if (o is GroupStartData)
            {
                ValidateCurrentFormattingState(FormattingState.Formatting, o);
                _currentFormattingState = FormattingState.InsideGroup;

                return false;
            }
            else if (o is GroupEndData)
            {
                ValidateCurrentFormattingState(FormattingState.InsideGroup, o);
                _currentFormattingState = FormattingState.Formatting;

                return false;
            }

            // this is a raw object
            return true;
        }

        private void ValidateCurrentFormattingState(FormattingState expectedFormattingState, object obj)
        {
            // check if we are in the expected formatting state
            if (_currentFormattingState != expectedFormattingState)
            {
                // we are not in the expected state, some message is out of sequence,
                // need to abort the command

                string violatingCommand = "format-*";
                StartData sdObj = obj as StartData;
                if (sdObj != null)
                {
                    if (sdObj.shapeInfo is WideViewHeaderInfo)
                    {
                        violatingCommand = "format-wide";
                    }
                    else if (sdObj.shapeInfo is TableHeaderInfo)
                    {
                        violatingCommand = "format-table";
                    }
                    else if (sdObj.shapeInfo is ListViewHeaderInfo)
                    {
                        violatingCommand = "format-list";
                    }
                    else if (sdObj.shapeInfo is ComplexViewHeaderInfo)
                    {
                        violatingCommand = "format-complex";
                    }
                }

                string msg = StringUtil.Format(FormatAndOut_out_xxx.OutLineOutput_OutOfSequencePacket,
                    obj.GetType().FullName, violatingCommand);

                ErrorRecord errorRecord = new ErrorRecord(
                                                new InvalidOperationException(),
                                                "ConsoleLineOutputOutOfSequencePacket",
                                                ErrorCategory.InvalidData,
                                                null);

                errorRecord.ErrorDetails = new ErrorDetails(msg);
                this.TerminatingErrorContext.ThrowTerminatingError(errorRecord);
            }
        }

        /// <summary>
        /// Shunt object to the formatting pipeline for preprocessing.
        /// </summary>
        /// <param name="o">Object to be preprocessed.</param>
        /// <returns>Array of objects returned by the preprocessing step.</returns>
        private Array ApplyFormatting(object o)
        {
            if (_command == null)
            {
                _command = new CommandWrapper();
                _command.Initialize(this.OuterCmdlet().Context, "format-default", typeof(FormatDefaultCommand));
            }

            return _command.Process(o);
        }

        /// <summary>
        /// Class factory for output context.
        /// </summary>
        /// <param name="parentContext">Parent context in the stack.</param>
        /// <param name="formatInfoData">Fromat info data received from the pipeline.</param>
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
                            goc = new TableOutputContext(this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.List:
                        {
                            goc = new ListOutputContext(this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.Wide:
                        {
                            goc = new WideOutputContext(this, parentContext, gsd);
                            break;
                        }

                    case FormatShape.Complex:
                        {
                            goc = new ComplexOutputContext(this, parentContext, gsd);
                            break;
                        }

                    default:
                        {
                            Diagnostics.Assert(false, "Invalid shape. This should never happen");
                        }

                        break;
                }

                goc.Initialize();
                return goc;
            }

            return null;
        }

        /// <summary>
        /// Callback for Fs processing.
        /// </summary>
        /// <param name="c">The context containing the Fs entry.</param>
        private void ProcessFormatStart(FormatMessagesContextManager.OutputContext c)
        {
            // we just add an empty line to the display
            this.LineOutput.WriteLine(string.Empty);
        }

        /// <summary>
        /// Callback for Fe processing.
        /// </summary>
        /// <param name="fe">Fe notification message.</param>
        /// <param name="c">Current context, with Fs in it.</param>
        private void ProcessFormatEnd(FormatEndData fe, FormatMessagesContextManager.OutputContext c)
        {
            if (c is FormatOutputContext foContext
                && foContext.Data.shapeInfo is ListViewHeaderInfo)
            {
                // Skip writing out a new line for List view, because we already wrote out
                // an extra new line after displaying the last list entry.
                return;
            }

            // We just add an empty line to the display.
            LineOutput.WriteLine(string.Empty);
        }

        /// <summary>
        /// Callback for Gs processing.
        /// </summary>
        /// <param name="c">The context containing the Gs entry.</param>
        private void ProcessGroupStart(FormatMessagesContextManager.OutputContext c)
        {
            // Console.WriteLine("ProcessGroupStart");
            GroupOutputContext goc = (GroupOutputContext)c;

            if (goc.Data.groupingEntry != null)
            {
                ComplexWriter writer = new ComplexWriter();
                writer.Initialize(_lo, _lo.ColumnNumber);
                writer.WriteObject(goc.Data.groupingEntry.formatValueList);
                _lo.WriteLine(string.Empty);
            }

            goc.GroupStart();
        }

        /// <summary>
        /// Callback for Ge processing.
        /// </summary>
        /// <param name="ge">Ge notification message.</param>
        /// <param name="c">Current context, with Gs in it.</param>
        private void ProcessGroupEnd(GroupEndData ge, FormatMessagesContextManager.OutputContext c)
        {
            // Console.WriteLine("ProcessGroupEnd");
            GroupOutputContext goc = (GroupOutputContext)c;

            goc.GroupEnd();
        }

        /// <summary>
        /// Process the current payload object.
        /// </summary>
        /// <param name="fed">FormatEntryData to process.</param>
        /// <param name="c">Currently active context.</param>
        private void ProcessPayload(FormatEntryData fed, FormatMessagesContextManager.OutputContext c)
        {
            // we assume FormatEntryData as a standard wrapper
            if (fed == null)
            {
                PSTraceSource.NewArgumentNullException(nameof(fed));
            }

            if (fed.formatEntryInfo == null)
            {
                PSTraceSource.NewArgumentNullException("fed.formatEntryInfo");
            }

            WriteStreamType oldWSState = _lo.WriteStream;
            try
            {
                _lo.WriteStream = fed.writeStream;

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
                _lo.WriteStream = oldWSState;
            }
        }

        private void ProcessOutOfBandPayload(FormatEntryData fed)
        {
            // try if it is raw text
            RawTextFormatEntry rte = fed.formatEntryInfo as RawTextFormatEntry;
            if (rte != null)
            {
                if (fed.isHelpObject)
                {
                    ComplexWriter complexWriter = new ComplexWriter();

                    complexWriter.Initialize(_lo, _lo.ColumnNumber);
                    complexWriter.WriteString(rte.text);
                }
                else
                {
                    // Write out raw text without any changes to it.
                    _lo.WriteRawText(rte.text);
                }

                return;
            }

            // try if it is a complex entry
            ComplexViewEntry cve = fed.formatEntryInfo as ComplexViewEntry;
            if (cve != null && cve.formatValueList != null)
            {
                ComplexWriter complexWriter = new ComplexWriter();

                complexWriter.Initialize(_lo, int.MaxValue);
                complexWriter.WriteObject(cve.formatValueList);

                return;
            }
            // try if it is a list view
            ListViewEntry lve = fed.formatEntryInfo as ListViewEntry;
            if (lve != null && lve.listViewFieldList != null)
            {
                ListWriter listWriter = new ListWriter();

                _lo.WriteLine(string.Empty);

                string[] properties = ListOutputContext.GetProperties(lve);
                listWriter.Initialize(properties, _lo.ColumnNumber, _lo.DisplayCells);
                string[] values = ListOutputContext.GetValues(lve);
                listWriter.WriteProperties(values, _lo);

                _lo.WriteLine(string.Empty);

                return;
            }
        }

        /// <summary>
        /// The screen host associated with this outputter.
        /// </summary>
        private LineOutput _lo = null;

        internal LineOutput LineOutput
        {
            get { return _lo; }

            set { _lo = value; }
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
        /// Retrieve the active FormatOutputContext on the stack
        /// by walking up to the top of the stack.
        /// </summary>
        private FormatOutputContext FormatContext
        {
            get
            {
                for (FormatMessagesContextManager.OutputContext oc = _ctxManager.ActiveOutputContext; oc != null; oc = oc.ParentContext)
                {
                    FormatOutputContext foc = oc as FormatOutputContext;

                    if (foc != null)
                        return foc;
                }

                return null;
            }
        }

        /// <summary>
        /// Context manager instance to guide the message traversal.
        /// </summary>
        private readonly FormatMessagesContextManager _ctxManager = new FormatMessagesContextManager();

        private FormattedObjectsCache _cache = null;

        /// <summary>
        /// Handler for processing the caching notification and responsible for
        /// setting the value of the formatting hint.
        /// </summary>
        /// <param name="formatStartData"></param>
        /// <param name="objects"></param>
        private void ProcessCachedGroup(FormatStartData formatStartData, List<PacketInfoData> objects)
        {
            _formattingHint = null;

            TableHeaderInfo thi = formatStartData.shapeInfo as TableHeaderInfo;

            if (thi != null)
            {
                ProcessCachedGroupOnTable(thi, objects);
                return;
            }

            WideViewHeaderInfo wvhi = formatStartData.shapeInfo as WideViewHeaderInfo;

            if (wvhi != null)
            {
                ProcessCachedGroupOnWide(wvhi, objects);
                return;
            }
        }

        private void ProcessCachedGroupOnTable(TableHeaderInfo thi, List<PacketInfoData> objects)
        {
            if (thi.tableColumnInfoList.Count == 0)
                return;

            int[] widths = new int[thi.tableColumnInfoList.Count];

            for (int k = 0; k < thi.tableColumnInfoList.Count; k++)
            {
                string label = thi.tableColumnInfoList[k].label;

                if (string.IsNullOrEmpty(label))
                    label = thi.tableColumnInfoList[k].propertyName;

                if (string.IsNullOrEmpty(label))
                    widths[k] = 0;
                else
                    widths[k] = _lo.DisplayCells.Length(label);
            }

            int cellCount; // scratch variable
            foreach (PacketInfoData o in objects)
            {
                if (o is FormatEntryData fed)
                {
                    TableRowEntry tre = fed.formatEntryInfo as TableRowEntry;
                    int kk = 0;

                    foreach (FormatPropertyField fpf in tre.formatPropertyFieldList)
                    {
                        cellCount = _lo.DisplayCells.Length(fpf.propertyValue);
                        if (widths[kk] < cellCount)
                            widths[kk] = cellCount;

                        kk++;
                    }
                }
            }

            TableFormattingHint hint = new TableFormattingHint();

            hint.columnWidths = widths;
            _formattingHint = hint;
        }

        private void ProcessCachedGroupOnWide(WideViewHeaderInfo wvhi, List<PacketInfoData> objects)
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
                if (o is FormatEntryData fed)
                {
                    WideViewEntry wve = fed.formatEntryInfo as WideViewEntry;
                    FormatPropertyField fpf = wve.formatPropertyField as FormatPropertyField;

                    if (!string.IsNullOrEmpty(fpf.propertyValue))
                    {
                        cellCount = _lo.DisplayCells.Length(fpf.propertyValue);
                        if (cellCount > maxLen)
                            maxLen = cellCount;
                    }
                }
            }

            WideFormattingHint hint = new WideFormattingHint();

            hint.maxWidth = maxLen;
            _formattingHint = hint;
        }

        /// <summary>
        /// Tables and Wides need to use spaces for padding to maintain table look even if console window is resized.
        /// For all other output, we use int.MaxValue if the user didn't explicitly specify a width.
        /// If we detect that int.MaxValue is used, first we try to get the current console window width.
        /// However, if we can't read that (for example, implicit remoting has no console window), we default
        /// to something reasonable: 120 columns.
        /// </summary>
        private static int GetConsoleWindowWidth(int columnNumber)
        {
            if (InternalTestHooks.SetConsoleWidthToZero)
            {
                return DefaultConsoleWidth;
            }

            if (columnNumber == int.MaxValue)
            {
                try
                {
                    // if Console width is set to 0, the default width is returned so that the output string is not null.
                    // This can happen in environments where TERM is not set.
                    return (Console.WindowWidth != 0) ? Console.WindowWidth : DefaultConsoleWidth;
                }
                catch
                {
                    return DefaultConsoleWidth;
                }
            }

            return columnNumber;
        }

        /// <summary>
        /// Return the console height.null  If not available (like when remoting), treat as Int.MaxValue.
        /// </summary>
        private static int GetConsoleWindowHeight(int rowNumber)
        {
            if (InternalTestHooks.SetConsoleHeightToZero)
            {
                return DefaultConsoleHeight;
            }

            if (rowNumber <= 0)
            {
                try
                {
                    // if Console height is set to 0, the default height is returned.
                    // This can happen in environments where TERM is not set.
                    return (Console.WindowHeight > 0) ? Console.WindowHeight : DefaultConsoleHeight;
                }
                catch
                {
                    return DefaultConsoleHeight;
                }
            }

            return rowNumber;
        }

        /// <summary>
        /// Base class for all the formatting hints.
        /// </summary>
        private abstract class FormattingHint
        {
        }

        /// <summary>
        /// Hint for format-table.
        /// </summary>
        private sealed class TableFormattingHint : FormattingHint
        {
            internal int[] columnWidths = null;
        }

        /// <summary>
        /// Hint for format-wide.
        /// </summary>
        private sealed class WideFormattingHint : FormattingHint
        {
            internal int maxWidth = 0;
        }

        /// <summary>
        /// Variable holding the autosize hint (set by the caching code and reset by the hint consumer.
        /// </summary>
        private FormattingHint _formattingHint = null;

        /// <summary>
        /// Helper for consuming the formatting hint.
        /// </summary>
        /// <returns></returns>
        private FormattingHint RetrieveFormattingHint()
        {
            FormattingHint fh = _formattingHint;
            _formattingHint = null;
            return fh;
        }

        private FormatObjectDeserializer _formatObjectDeserializer;

        /// <summary>
        /// Context for the outer scope of the format sequence.
        /// </summary>
        private sealed class FormatOutputContext : FormatMessagesContextManager.OutputContext
        {
            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal FormatOutputContext(FormatMessagesContextManager.OutputContext parentContext, FormatStartData formatData)
                : base(parentContext)
            {
                Data = formatData;
            }

            /// <summary>
            /// Retrieve the format data in the context.
            /// </summary>
            internal FormatStartData Data { get; } = null;
        }

        /// <summary>
        /// Context for the currently active group.
        /// </summary>
        private abstract class GroupOutputContext : FormatMessagesContextManager.OutputContext
        {
            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            internal GroupOutputContext(OutCommandInner cmd,
                                    FormatMessagesContextManager.OutputContext parentContext,
                                    GroupStartData formatData)
                : base(parentContext)
            {
                InnerCommand = cmd;
                Data = formatData;
            }

            /// <summary>
            /// Called at creation time, overrides will initialize here, e.g.
            /// column widths, etc.
            /// </summary>
            internal virtual void Initialize() { }

            /// <summary>
            /// Called when a group of data is started, overridden will do
            /// things such as headers, etc...
            /// </summary>
            internal virtual void GroupStart() { }

            /// <summary>
            /// Called when the end of a group is reached, overrides will do
            /// things such as group footers.
            /// </summary>
            internal virtual void GroupEnd() { }

            /// <summary>
            /// Called when there is an entry to process, overrides will do
            /// things such as writing a row in a table.
            /// </summary>
            /// <param name="fed">FormatEntryData to process.</param>
            internal virtual void ProcessPayload(FormatEntryData fed) { }

            /// <summary>
            /// Retrieve the format data in the context.
            /// </summary>
            internal GroupStartData Data { get; } = null;

            protected OutCommandInner InnerCommand { get; }
        }

        private class TableOutputContextBase : GroupOutputContext
        {
            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="cmd">Reference to the OutCommandInner instance who owns this instance.</param>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal TableOutputContextBase(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext,
                GroupStartData formatData)
                : base(cmd, parentContext, formatData)
            {
            }

            /// <summary>
            /// Get the table writer for this context.
            /// </summary>
            protected TableWriter Writer { get { return _tableWriter; } }

            /// <summary>
            /// Helper class to properly write a table using text output.
            /// </summary>
            private readonly TableWriter _tableWriter = new TableWriter();
        }

        private sealed class TableOutputContext : TableOutputContextBase
        {
            private int _rowCount = 0;
            private int _consoleHeight = -1;
            private int _consoleWidth = -1;

            private const int WhitespaceAndPagerLineCount = 2;

            private readonly bool _repeatHeader = false;

            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="cmd">Reference to the OutCommandInner instance who owns this instance.</param>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal TableOutputContext(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext,
                GroupStartData formatData)
                : base(cmd, parentContext, formatData)
            {
                if (parentContext is FormatOutputContext foc)
                {
                    if (foc.Data.shapeInfo is TableHeaderInfo thi)
                    {
                        _repeatHeader = thi.repeatHeader;
                    }
                }
            }

            /// <summary>
            /// Initialize column widths.
            /// </summary>
            internal override void Initialize()
            {
                TableFormattingHint tableHint = this.InnerCommand.RetrieveFormattingHint() as TableFormattingHint;
                int[] columnWidthsHint = null;
                // We expect that console width is less then 120.

                if (tableHint != null)
                {
                    columnWidthsHint = tableHint.columnWidths;
                }

                _consoleHeight = GetConsoleWindowHeight(this.InnerCommand._lo.RowNumber);
                _consoleWidth = GetConsoleWindowWidth(this.InnerCommand._lo.ColumnNumber);

                int columns = this.CurrentTableHeaderInfo.tableColumnInfoList.Count;
                if (columns == 0)
                {
                    return;
                }

                // create arrays for widths and alignment
                Span<int> columnWidths = columns <= StackAllocThreshold ? stackalloc int[columns] : new int[columns];
                Span<int> alignment = columns <= StackAllocThreshold ? stackalloc int[columns] : new int[columns];
                Span<bool> headerMatchesProperty = columns <= StackAllocThreshold ? stackalloc bool[columns] : new bool[columns];

                int k = 0;
                foreach (TableColumnInfo tci in this.CurrentTableHeaderInfo.tableColumnInfoList)
                {
                    columnWidths[k] = (columnWidthsHint != null) ? columnWidthsHint[k] : tci.width;
                    alignment[k] = tci.alignment;
                    headerMatchesProperty[k] = tci.HeaderMatchesProperty;
                    k++;
                }

                this.Writer.Initialize(0, _consoleWidth, columnWidths, alignment, headerMatchesProperty, this.CurrentTableHeaderInfo.hideHeader);
            }

            /// <summary>
            /// Write the headers.
            /// </summary>
            internal override void GroupStart()
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
                    properties[k++] = tci.label ?? tci.propertyName;
                }

                _rowCount += this.Writer.GenerateHeader(properties, this.InnerCommand._lo);
            }

            /// <summary>
            /// Write a row into the table.
            /// </summary>
            /// <param name="fed">FormatEntryData to process.</param>
            internal override void ProcessPayload(FormatEntryData fed)
            {
                int headerColumns = this.CurrentTableHeaderInfo.tableColumnInfoList.Count;

                if (headerColumns == 0)
                {
                    return;
                }

                if (_repeatHeader && _rowCount >= _consoleHeight - WhitespaceAndPagerLineCount)
                {
                    this.InnerCommand._lo.WriteLine(string.Empty);
                    _rowCount = this.Writer.GenerateHeader(null, this.InnerCommand._lo);
                }

                TableRowEntry tre = fed.formatEntryInfo as TableRowEntry;

                // need to make sure we have matching counts: the header count will have to prevail
                string[] values = new string[headerColumns];
                Span<int> alignment = headerColumns <= StackAllocThreshold ? stackalloc int[headerColumns] : new int[headerColumns];

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
                        values[k] = string.Empty;
                        alignment[k] = TextAlignment.Left; // hard coded default
                    }
                }

                this.Writer.GenerateRow(values, this.InnerCommand._lo, tre.multiLine, alignment, InnerCommand._lo.DisplayCells, generatedRows: null);
                _rowCount++;
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
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="cmd">Reference to the OutCommandInner instance who owns this instance.</param>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal ListOutputContext(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext,
                GroupStartData formatData)
                : base(cmd, parentContext, formatData)
            {
            }

            /// <summary>
            /// Initialize column widths.
            /// </summary>
            internal override void Initialize()
            {
            }

            private void InternalInitialize(ListViewEntry lve)
            {
                _properties = GetProperties(lve);
                _listWriter.Initialize(_properties, this.InnerCommand._lo.ColumnNumber, InnerCommand._lo.DisplayCells);
            }

            internal static string[] GetProperties(ListViewEntry lve)
            {
                int count = lve.listViewFieldList.Count;

                if (count == 0)
                {
                    return null;
                }

                string[] result = new string[count];
                for (int index = 0; index < result.Length; ++index)
                {
                    ListViewField lvf = lve.listViewFieldList[index];
                    result[index] = lvf.label ?? lvf.propertyName;
                }

                return result;
            }

            internal static string[] GetValues(ListViewEntry lve)
            {
                int count = lve.listViewFieldList.Count;

                if (count == 0)
                {
                    return null;
                }

                string[] result = new string[count];
                for (int index = 0; index < result.Length; ++index)
                {
                    ListViewField lvf = lve.listViewFieldList[index];
                    result[index] = lvf.formatPropertyField.propertyValue;
                }

                return result;
            }

            /// <summary>
            /// Write the headers.
            /// </summary>
            internal override void GroupStart()
            {
            }

            /// <summary>
            /// Write a row into the list.
            /// </summary>
            /// <param name="fed">FormatEntryData to process.</param>
            internal override void ProcessPayload(FormatEntryData fed)
            {
                ListViewEntry lve = fed.formatEntryInfo as ListViewEntry;
                InternalInitialize(lve);
                string[] values = GetValues(lve);
                _listWriter.WriteProperties(values, this.InnerCommand._lo);
                this.InnerCommand._lo.WriteLine(string.Empty);
            }

            /// <summary>
            /// Property list currently active.
            /// </summary>
            private string[] _properties = null;

            /// <summary>
            /// Writer to do the actual formatting.
            /// </summary>
            private readonly ListWriter _listWriter = new ListWriter();
        }

        private sealed class WideOutputContext : TableOutputContextBase
        {
            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="cmd">Reference to the OutCommandInner instance who owns this instance.</param>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal WideOutputContext(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext,
                GroupStartData formatData)
                : base(cmd, parentContext, formatData)
            {
            }

            private StringValuesBuffer _buffer = null;

            /// <summary>
            /// Initialize column widths.
            /// </summary>
            internal override void Initialize()
            {
                // set the hard wider default, to be used if no other info is available
                int itemsPerRow = 2;

                // get the header info and the view hint
                WideFormattingHint hint = this.InnerCommand.RetrieveFormattingHint() as WideFormattingHint;

                int columnsOnTheScreen = GetConsoleWindowWidth(this.InnerCommand._lo.ColumnNumber);

                // give a preference to the hint, if there
                if (hint != null && hint.maxWidth > 0)
                {
                    itemsPerRow = TableWriter.ComputeWideViewBestItemsPerRowFit(hint.maxWidth, columnsOnTheScreen);
                }
                else if (this.CurrentWideHeaderInfo.columns > 0)
                {
                    itemsPerRow = this.CurrentWideHeaderInfo.columns;
                }

                // create a buffer object to hold partial rows
                _buffer = new StringValuesBuffer(itemsPerRow);

                // initialize the writer
                Span<int> columnWidths = itemsPerRow <= StackAllocThreshold ? stackalloc int[itemsPerRow] : new int[itemsPerRow];
                Span<int> alignment = itemsPerRow <= StackAllocThreshold ? stackalloc int[itemsPerRow] : new int[itemsPerRow];

                for (int k = 0; k < itemsPerRow; k++)
                {
                    columnWidths[k] = 0; // autosize
                    alignment[k] = TextAlignment.Left;
                }

                this.Writer.Initialize(leftMarginIndent: 0, columnsOnTheScreen, columnWidths, alignment, headerMatchesProperty: null, suppressHeader: false, screenRows: GetConsoleWindowHeight(this.InnerCommand._lo.RowNumber));
            }

            /// <summary>
            /// Write the headers.
            /// </summary>
            internal override void GroupStart()
            {
            }

            /// <summary>
            /// Called when the end of a group is reached, flush the
            /// write buffer.
            /// </summary>
            internal override void GroupEnd()
            {
                WriteStringBuffer();
            }

            /// <summary>
            /// Write a row into the table.
            /// </summary>
            /// <param name="fed">FormatEntryData to process.</param>
            internal override void ProcessPayload(FormatEntryData fed)
            {
                WideViewEntry wve = fed.formatEntryInfo as WideViewEntry;
                FormatPropertyField fpf = wve.formatPropertyField as FormatPropertyField;
                _buffer.Add(fpf.propertyValue);
                if (_buffer.IsFull)
                {
                    WriteStringBuffer();
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
                if (_buffer.IsEmpty)
                {
                    return;
                }

                string[] values = new string[_buffer.Length];
                for (int k = 0; k < values.Length; k++)
                {
                    if (k < _buffer.CurrentCount)
                        values[k] = _buffer[k];
                    else
                        values[k] = string.Empty;
                }

                this.Writer.GenerateRow(values, this.InnerCommand._lo, false, null, InnerCommand._lo.DisplayCells, generatedRows: null);
                _buffer.Reset();
            }

            /// <summary>
            /// Helper class to accumulate the display values so that when the end
            /// of a line is reached, a full line can be composed.
            /// </summary>
            private sealed class StringValuesBuffer
            {
                /// <summary>
                /// Construct the buffer.
                /// </summary>
                /// <param name="size">Number of entries to cache.</param>
                internal StringValuesBuffer(int size)
                {
                    _arr = new string[size];
                    Reset();
                }
                /// <summary>
                /// Get the size of the buffer.
                /// </summary>
                internal int Length { get { return _arr.Length; } }

                /// <summary>
                /// Get the current number of entries in the buffer.
                /// </summary>
                internal int CurrentCount { get { return _lastEmptySpot; } }

                /// <summary>
                /// Check if the buffer is full.
                /// </summary>
                internal bool IsFull
                {
                    get { return _lastEmptySpot == _arr.Length; }
                }

                /// <summary>
                /// Check if the buffer is empty.
                /// </summary>
                internal bool IsEmpty
                {
                    get { return _lastEmptySpot == 0; }
                }

                /// <summary>
                /// Indexer to access the k-th item in the buffer.
                /// </summary>
                internal string this[int k] { get { return _arr[k]; } }

                /// <summary>
                /// Add an item to the buffer.
                /// </summary>
                /// <param name="s">String to add.</param>
                internal void Add(string s)
                {
                    _arr[_lastEmptySpot++] = s;
                }

                /// <summary>
                /// Reset the buffer.
                /// </summary>
                internal void Reset()
                {
                    _lastEmptySpot = 0;
                    for (int k = 0; k < _arr.Length; k++)
                        _arr[k] = null;
                }

                private readonly string[] _arr;
                private int _lastEmptySpot;
            }
        }

        private sealed class ComplexOutputContext : GroupOutputContext
        {
            /// <summary>
            /// Construct a context to push on the stack.
            /// </summary>
            /// <param name="cmd">Reference to the OutCommandInner instance who owns this instance.</param>
            /// <param name="parentContext">Parent context in the stack.</param>
            /// <param name="formatData">Format data to put in the context.</param>
            internal ComplexOutputContext(OutCommandInner cmd,
                FormatMessagesContextManager.OutputContext parentContext,
                GroupStartData formatData)
                : base(cmd, parentContext, formatData)
            {
            }

            internal override void Initialize()
            {
                _writer.Initialize(this.InnerCommand._lo,
                                    this.InnerCommand._lo.ColumnNumber);
            }

            /// <summary>
            /// Write a row into the list.
            /// </summary>
            /// <param name="fed">FormatEntryData to process.</param>
            internal override void ProcessPayload(FormatEntryData fed)
            {
                ComplexViewEntry cve = fed.formatEntryInfo as ComplexViewEntry;
                if (cve == null || cve.formatValueList == null)
                    return;
                _writer.WriteObject(cve.formatValueList);
            }

            private readonly ComplexWriter _writer = new ComplexWriter();
        }
    }
}
