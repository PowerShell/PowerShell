// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Contains information about a single history entry.
    /// </summary>
    public class HistoryInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pipelineId">Id of pipeline in which command associated
        /// with this history entry is executed</param>
        /// <param name="cmdline">Command string.</param>
        /// <param name="status">Status of pipeline execution.</param>
        /// <param name="startTime">StartTime of execution.</param>
        /// <param name="endTime">EndTime of execution.</param>
        internal HistoryInfo(long pipelineId, string cmdline, PipelineState status, DateTime startTime, DateTime endTime)
        {
            Dbg.Assert(cmdline != null, "caller should validate the parameter");
            _pipelineId = pipelineId;
            CommandLine = cmdline;
            ExecutionStatus = status;
            StartExecutionTime = startTime;
            EndExecutionTime = endTime;
            Cleared = false;
        }

        /// <summary>
        /// Copy constructor to support cloning.
        /// </summary>
        /// <param name="history"></param>
        private HistoryInfo(HistoryInfo history)
        {
            Id = history.Id;
            _pipelineId = history._pipelineId;
            CommandLine = history.CommandLine;
            ExecutionStatus = history.ExecutionStatus;
            StartExecutionTime = history.StartExecutionTime;
            EndExecutionTime = history.EndExecutionTime;
            Cleared = history.Cleared;
        }

        /// <summary>
        /// Id of this history entry.
        /// </summary>
        /// <value></value>
        public long Id { get; private set; }

        /// <summary>
        /// CommandLine string.
        /// </summary>
        /// <value></value>
        public string CommandLine { get; private set; }

        /// <summary>
        /// Execution status of associated pipeline.
        /// </summary>
        /// <value></value>
        public PipelineState ExecutionStatus { get; private set; }

        /// <summary>
        /// Start time of execution of associated pipeline.
        /// </summary>
        /// <value></value>
        public DateTime StartExecutionTime { get; }

        /// <summary>
        /// End time of execution of associated pipeline.
        /// </summary>
        /// <value></value>
        public DateTime EndExecutionTime { get; private set; }

        /// <summary>
        /// The time it took to execute the associeated pipeline.
        /// </summary>
        public TimeSpan Duration => EndExecutionTime - StartExecutionTime;

        /// <summary>
        /// Override for ToString() method.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(CommandLine))
            {
                return base.ToString();
            }
            else
            {
                return CommandLine;
            }
        }

        /// <summary>
        /// Cleared status of an entry.
        /// </summary>
        internal bool Cleared { get; set; } = false;

        /// <summary>
        /// Sets Id.
        /// </summary>
        /// <param name="id"></param>
        internal void SetId(long id) => Id = id;

        /// <summary>
        /// Set status.
        /// </summary>
        /// <param name="status"></param>
        internal void SetStatus(PipelineState status) => ExecutionStatus = status;

        /// <summary>
        /// Set endtime.
        /// </summary>
        /// <param name="endTime"></param>
        internal void SetEndTime(DateTime endTime) => EndExecutionTime = endTime;

        /// <summary>
        /// Sets command.
        /// </summary>
        /// <param name="command"></param>
        internal void SetCommand(string command) => CommandLine = command;

        /// <summary>
        /// Id of the pipeline corresponding to this history entry.
        /// </summary>
        private readonly long _pipelineId;

        /// <summary>
        /// Returns a clone of this object.
        /// </summary>
        /// <returns></returns>
        public HistoryInfo Clone()
        {
            return new HistoryInfo(this);
        }
    }

    /// <summary>
    /// This class implements history and provides APIs for adding and fetching
    /// entries from history.
    /// </summary>
    internal class History
    {
        /// <summary>
        /// Default history size.
        /// </summary>
        internal const int DefaultHistorySize = 4096;

        #region constructors

        /// <summary>
        /// Constructs history store.
        /// </summary>
        internal History(ExecutionContext context)
        {
            // Create history size variable. Add ValidateRangeAttribute to
            // validate the range.
            Collection<Attribute> attrs = new Collection<Attribute>();
            attrs.Add(new ValidateRangeAttribute(1, (int)short.MaxValue));
            PSVariable historySizeVar = new PSVariable(SpecialVariables.HistorySize, DefaultHistorySize, ScopedItemOptions.None, attrs);
            historySizeVar.Description = SessionStateStrings.MaxHistoryCountDescription;

            context.EngineSessionState.SetVariable(historySizeVar, false, CommandOrigin.Internal);

            _capacity = DefaultHistorySize;
            _buffer = new HistoryInfo[_capacity];
        }

        #endregion constructors

        #region internal

        /// <summary>
        /// Create a new history entry.
        /// </summary>
        /// <param name="pipelineId"></param>
        /// <param name="cmdline"></param>
        /// <param name="status"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="skipIfLocked">If true, the entry will not be added when the history is locked.</param>
        /// <returns>Id for the new created entry. Use this id to fetch the
        /// entry. Returns -1 if the entry is not added.</returns>
        /// <remarks>This function is thread safe</remarks>
        internal long AddEntry(long pipelineId, string cmdline, PipelineState status, DateTime startTime, DateTime endTime, bool skipIfLocked)
        {
            if (!System.Threading.Monitor.TryEnter(_syncRoot, skipIfLocked ? 0 : System.Threading.Timeout.Infinite))
            {
                return -1;
            }

            try
            {
                ReallocateBufferIfNeeded();

                HistoryInfo entry = new HistoryInfo(pipelineId, cmdline, status, startTime, endTime);
                return Add(entry);
            }
            finally
            {
                System.Threading.Monitor.Exit(_syncRoot);
            }
        }

        /// <summary>
        /// Update the history entry corresponding to id.
        /// </summary>
        /// <param name="id">Id of history entry to be updated.</param>
        /// <param name="status">Status to be updated.</param>
        /// <param name="endTime">EndTime to be updated.</param>
        /// <param name="skipIfLocked">If true, the entry will not be added when the history is locked.</param>
        /// <returns></returns>
        internal void UpdateEntry(long id, PipelineState status, DateTime endTime, bool skipIfLocked)
        {
            if (!System.Threading.Monitor.TryEnter(_syncRoot, skipIfLocked ? 0 : System.Threading.Timeout.Infinite))
            {
                return;
            }

            try
            {
                HistoryInfo entry = CoreGetEntry(id);
                if (entry != null)
                {
                    entry.SetStatus(status);
                    entry.SetEndTime(endTime);
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(_syncRoot);
            }
        }

        /// <summary>
        /// Gets entry from buffer for given id. This id should be the
        /// id returned by Add method.
        /// </summary>
        /// <param name="id">Id of the entry to be fetched.</param>
        /// <returns>Entry corresponding to id if it is present else null
        /// </returns>
        internal HistoryInfo GetEntry(long id)
        {
            lock (_syncRoot)
            {
                ReallocateBufferIfNeeded();

                HistoryInfo entry = CoreGetEntry(id);
                if (entry != null)
                    if (!entry.Cleared)
                        return entry.Clone();

                return null;
            }
        }

        /// <summary>
        /// Get count HistoryEntries.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        /// <param name="newest"></param>
        /// <returns>History entries.</returns>
        internal HistoryInfo[] GetEntries(long id, long count, SwitchParameter newest)
        {
            ReallocateBufferIfNeeded();

            if (count < -1)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(count), count);
            }

            if (newest.ToString() == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(newest));
            }

            if (count == -1 || count > _countEntriesAdded || count > _countEntriesInBuffer)
                count = _countEntriesInBuffer;

            if (count == 0 || _countEntriesInBuffer == 0)
            {
                return Array.Empty<HistoryInfo>();
            }

            lock (_syncRoot)
            {
                // Using list instead of an array to store the entries.With array we are getting null values
                // when the historybuffer size is changed
                List<HistoryInfo> entriesList = new List<HistoryInfo>();
                if (id > 0)
                {
                    long firstId, baseId;
                    baseId = id;

                    // get id,count,newest values
                    if (!newest.IsPresent)
                    {
                        // get older entries

                        // Calculate the first id (i.e lowest id to fetch)
                        firstId = baseId - count + 1;

                        // If first id is less than the lowest id in history store,
                        // assign lowest id as first ID
                        if (firstId < 1)
                        {
                            firstId = 1;
                        }

                        for (long i = baseId; i >= firstId; --i)
                        {
                            if (firstId <= 1) break;
                            // if entry is null , continue the loop with the next entry
                            if (_buffer[GetIndexFromId(i)] == null) continue;
                            if (_buffer[GetIndexFromId(i)].Cleared)
                            {
                                // we have to clear count entries before an id, so if an entry is null,decrement
                                // first id as long as its is greater than the lowest entry in the buffer.
                                firstId--;
                                continue;
                            }
                        }

                        for (long i = firstId; i <= baseId; ++i)
                        {
                            // if an entry is null after being cleared by clear-history cmdlet,
                            // continue with the next entry
                            if (_buffer[GetIndexFromId(i)] == null || _buffer[GetIndexFromId(i)].Cleared)
                                continue;
                            entriesList.Add(_buffer[GetIndexFromId(i)].Clone());
                        }
                    }
                    else
                    { // get latest entries
                        // first id becomes the id +count no of entries from the end of the buffer
                        firstId = baseId + count - 1;
                        // if first id is more than the no of entries in the buffer, first id will be the last entry in the buffer
                        if (firstId >= _countEntriesAdded)
                        {
                            firstId = _countEntriesAdded;
                        }

                        for (long i = baseId; i <= firstId; i++)
                        {
                            if (firstId >= _countEntriesAdded) break;
                            // if entry is null , continue the loop with the next entry
                            if (_buffer[GetIndexFromId(i)] == null) continue;
                            if (_buffer[GetIndexFromId(i)].Cleared)
                            {
                                // we have to clear count entries before an id, so if an entry is null,increment first id
                                firstId++;
                                continue;
                            }
                        }

                        for (long i = firstId; i >= baseId; --i)
                        {
                            // if an entry is null after being cleared by clear-history cmdlet,
                            // continue with the next entry
                            if (_buffer[GetIndexFromId(i)] == null || _buffer[GetIndexFromId(i)].Cleared)
                                continue;
                            entriesList.Add(_buffer[GetIndexFromId(i)].Clone());
                        }
                    }
                }
                else
                {
                    // get entries for count,newest

                    long index, SmallestID = 0;
                    // if we change the defaulthistory size and when no of entries exceed the size, then
                    // we need to get the smallest entry in the buffer when we want to clear the oldest entry
                    // eg if size is 5 and then the entries can be 7,6,1,2,3
                    if (_capacity != DefaultHistorySize)
                        SmallestID = SmallestIDinBuffer();
                    if (!newest.IsPresent)
                    {
                        // get oldest count entries
                        index = 1;
                        if (_capacity != DefaultHistorySize)
                        {
                            if (_countEntriesAdded > _capacity)
                                index = SmallestID;
                        }

                        for (long i = count - 1; i >= 0;)
                        {
                            if (index > _countEntriesAdded) break;
                            if ((index <= 0 || GetIndexFromId(index) >= _buffer.Length) ||
                                (_buffer[GetIndexFromId(index)].Cleared))
                            {
                                index++; continue;
                            }
                            else
                            {
                                entriesList.Add(_buffer[GetIndexFromId(index)].Clone());
                                i--; index++;
                            }
                        }
                    }
                    else
                    {
                        index = _countEntriesAdded; //SmallestIDinBuffer

                        for (long i = count - 1; i >= 0;)
                        {
                            // if an entry is cleared continue to the next entry
                            if (_capacity != DefaultHistorySize)
                            {
                                if (_countEntriesAdded > _capacity)
                                {
                                    if (index < SmallestID)
                                        break;
                                }
                            }

                            if (index < 1) break;
                            if ((index <= 0 || GetIndexFromId(index) >= _buffer.Length) ||
                                (_buffer[GetIndexFromId(index)].Cleared))
                            { index--; continue; }
                            else
                            {
                                // clone the entry from the history buffer
                                entriesList.Add(_buffer[GetIndexFromId(index)].Clone());
                                i--; index--;
                            }
                        }
                    }
                }

                HistoryInfo[] entries = new HistoryInfo[entriesList.Count];
                entriesList.CopyTo(entries);
                return entries;
            }
        }

        /// <summary>
        /// Get History Entries based on the WildCard Pattern value.
        /// If passed 0, returns all the values, else return on the basis of count.
        /// </summary>
        /// <param name="wildcardpattern"></param>
        /// <param name="count"></param>
        /// <param name="newest"></param>
        /// <returns></returns>
        internal HistoryInfo[] GetEntries(WildcardPattern wildcardpattern, long count, SwitchParameter newest)
        {
            lock (_syncRoot)
            {
                if (count < -1)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException(nameof(count), count);
                }

                if (newest.ToString() == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(newest));
                }

                if (count > _countEntriesAdded || count == -1)
                {
                    count = _countEntriesInBuffer;
                }

                List<HistoryInfo> cmdlist = new List<HistoryInfo>();
                long SmallestID = 1;
                // if buffersize is changes,Get the smallest entry that's not cleared in the buffer
                if (_capacity != DefaultHistorySize)
                    SmallestID = SmallestIDinBuffer();
                if (count != 0)
                {
                    if (!newest.IsPresent)
                    {
                        long id = 1;
                        if (_capacity != DefaultHistorySize)
                        {
                            if (_countEntriesAdded > _capacity)
                                id = SmallestID;
                        }

                        for (long i = 0; i <= count - 1;)
                        {
                            if (id > _countEntriesAdded) break;
                            if (!_buffer[GetIndexFromId(id)].Cleared && wildcardpattern.IsMatch(_buffer[GetIndexFromId(id)].CommandLine.Trim()))
                            {
                                cmdlist.Add(_buffer[GetIndexFromId(id)].Clone()); i++;
                            }

                            id++;
                        }
                    }
                    else
                    {
                        long id = _countEntriesAdded;
                        for (long i = 0; i <= count - 1;)
                        {
                            // if buffersize is changed,we have to loop from max entry to min entry that's not cleared
                            if (_capacity != DefaultHistorySize)
                            {
                                if (_countEntriesAdded > _capacity)
                                {
                                    if (id < SmallestID)
                                        break;
                                }
                            }

                            if (id < 1) break;
                            if (!_buffer[GetIndexFromId(id)].Cleared && wildcardpattern.IsMatch(_buffer[GetIndexFromId(id)].CommandLine.Trim()))
                            {
                                cmdlist.Add(_buffer[GetIndexFromId(id)].Clone()); i++;
                            }

                            id--;
                        }
                    }
                }
                else
                {
                    for (long i = 1; i <= _countEntriesAdded; i++)
                    {
                        if (!_buffer[GetIndexFromId(i)].Cleared && wildcardpattern.IsMatch(_buffer[GetIndexFromId(i)].CommandLine.Trim()))
                        {
                            cmdlist.Add(_buffer[GetIndexFromId(i)].Clone());
                        }
                    }
                }

                HistoryInfo[] entries = new HistoryInfo[cmdlist.Count];
                cmdlist.CopyTo(entries);
                return entries;
            }
        }

        /// <summary>
        /// Clears the history entry from buffer for a given id.
        /// </summary>
        /// <param name="id">Id of the entry to be Cleared.</param>
        /// <returns>Nothing.</returns>
        internal void ClearEntry(long id)
        {
            lock (_syncRoot)
            {
                if (id < 0)
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException(nameof(id), id);
                }
                // no entries are present to clear
                if (_countEntriesInBuffer == 0)
                    return;

                // throw an exception if id is out of range
                if (id > _countEntriesAdded)
                {
                    return;
                }

                HistoryInfo entry = CoreGetEntry(id);
                if (entry != null)
                {
                    entry.Cleared = true;
                    _countEntriesInBuffer--;
                }

                return;
            }
        }

        /// <summary>
        /// gets the total number of entries added
        /// </summary>
        /// <returns>count of total entries added.</returns>
        internal int Buffercapacity()
        {
            return _capacity;
        }

        #endregion internal

        #region private

        /// <summary>
        /// Adds an entry to the buffer. If buffer is full, overwrites
        /// oldest entry in the buffer.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Returns id for the entry. This id should be used to fetch
        /// the entry from the buffer.</returns>
        /// <remarks>Id starts from 1 and is incremented by 1 for each new entry</remarks>
        private long Add(HistoryInfo entry)
        {
            if (entry == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(entry));
            }

            _buffer[GetIndexForNewEntry()] = entry;

            // Increment count of entries added so far
            _countEntriesAdded++;

            // Id of an entry in history is same as its number in history store.
            entry.SetId(_countEntriesAdded);

            // Increment count of entries in buffer by 1
            IncrementCountOfEntriesInBuffer();

            return _countEntriesAdded;
        }

        /// <summary>
        /// Gets entry from buffer for given id. This id should be the
        /// id returned by Add method.
        /// </summary>
        /// <param name="id">Id of the entry to be fetched.</param>
        /// <returns>Entry corresponding to id if it is present else null
        /// </returns>
        private HistoryInfo CoreGetEntry(long id)
        {
            if (id <= 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(id), id);
            }

            if (_countEntriesInBuffer == 0)
                return null;
            if (id > _countEntriesAdded)
            {
                return null;
            }
            // if (_buffer[GetIndexFromId(id)].Cleared == false )
            return _buffer[GetIndexFromId(id)];
            // else
            //    return null;
        }

        /// <summary>
        /// Gets the smallest id in the buffer.
        /// </summary>
        /// <returns></returns>
        private long SmallestIDinBuffer()
        {
            long minID = 0;
            if (_buffer == null)
                return minID;
            for (int i = 0; i < _buffer.Length; i++)
            {
                // assign the first entry in the buffer as min.
                if (_buffer[i] != null && !_buffer[i].Cleared)
                {
                    minID = _buffer[i].Id;
                    break;
                }
            }
            // check for the minimum id that is not cleared
            for (int i = 0; i < _buffer.Length; i++)
            {
                if (_buffer[i] != null && !_buffer[i].Cleared)
                    if (minID > _buffer[i].Id)
                        minID = _buffer[i].Id;
            }

            return minID;
        }

        /// <summary>
        /// Reallocates the buffer if history size changed.
        /// </summary>
        private void ReallocateBufferIfNeeded()
        {
            // Get current value of histoysize variable
            int historySize = GetHistorySize();

            if (historySize == _capacity)
                return;

            HistoryInfo[] tempBuffer = new HistoryInfo[historySize];

            // Calculate number of entries to copy in new buffer.
            int numberOfEntries = _countEntriesInBuffer;

            // when buffer size is changed,we have to consider the totalnumber of entries added
            if (numberOfEntries < _countEntriesAdded)
                numberOfEntries = (int)_countEntriesAdded;

            if (_countEntriesInBuffer > historySize)
                numberOfEntries = historySize;

            for (int i = numberOfEntries; i > 0; --i)
            {
                long nextId = _countEntriesAdded - i + 1;

                tempBuffer[GetIndexFromId(nextId, historySize)] = _buffer[GetIndexFromId(nextId)];
            }

            _countEntriesInBuffer = numberOfEntries;
            _capacity = historySize;
            _buffer = tempBuffer;
        }

        /// <summary>
        /// Get the index for new entry.
        /// </summary>
        /// <returns>Index for new entry.</returns>
        private int GetIndexForNewEntry()
        {
            return (int)(_countEntriesAdded % _capacity);
        }

        /// <summary>
        /// Gets index in buffer for an entry with given Id.
        /// </summary>
        /// <returns></returns>
        private int GetIndexFromId(long id)
        {
            return (int)((id - 1) % _capacity);
        }

        /// <summary>
        /// Gets index in buffer for an entry with given Id using passed in
        /// capacity.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        private static int GetIndexFromId(long id, int capacity)
        {
            return (int)((id - 1) % capacity);
        }

        /// <summary>
        /// Increment number of entries in buffer by 1.
        /// </summary>
        private void IncrementCountOfEntriesInBuffer()
        {
            if (_countEntriesInBuffer < _capacity)
                _countEntriesInBuffer++;
        }

        /// <summary>
        /// Get the current history size.
        /// </summary>
        /// <returns></returns>
        private static int GetHistorySize()
        {
            int historySize = 0;
            var executionContext = LocalPipeline.GetExecutionContextFromTLS();
            object obj = executionContext?.GetVariableValue(SpecialVariables.HistorySizeVarPath);
            if (obj != null)
            {
                try
                {
                    historySize = (int)LanguagePrimitives.ConvertTo(obj, typeof(int), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException)
                { }
            }

            if (historySize <= 0)
            {
                historySize = DefaultHistorySize;
            }

            return historySize;
        }

        /// <summary>
        /// Buffer.
        /// </summary>
        private HistoryInfo[] _buffer;

        /// <summary>
        /// Capacity of circular buffer.
        /// </summary>
        private int _capacity;

        /// <summary>
        /// Number of entries in buffer currently.
        /// </summary>
        private int _countEntriesInBuffer;

        /// <summary>
        /// Total number of entries added till now including those which have
        /// been overwritten after buffer got full. This is also number of
        /// last entry added.
        /// </summary>
        private long _countEntriesAdded;

        /// <summary>
        /// Private object for synchronization.
        /// </summary>
        private readonly object _syncRoot = new object();

        #endregion private

        /// <summary>
        /// Return the ID of the next history item to be added.
        /// </summary>
        internal long GetNextHistoryId()
        {
            return _countEntriesAdded + 1;
        }

        #region invoke_loop_detection

        /// <summary>
        /// This is a set of HistoryInfo ids which are currently being executed in the
        /// pipelines of the Runspace that is holding this 'History' instance.
        /// </summary>
        private readonly HashSet<long> _invokeHistoryIds = new HashSet<long>();

        internal bool PresentInInvokeHistoryEntrySet(HistoryInfo entry)
        {
            return _invokeHistoryIds.Contains(entry.Id);
        }

        internal void AddToInvokeHistoryEntrySet(HistoryInfo entry)
        {
            _invokeHistoryIds.Add(entry.Id);
        }

        internal void RemoveFromInvokeHistoryEntrySet(HistoryInfo entry)
        {
            _invokeHistoryIds.Remove(entry.Id);
        }

        #endregion invoke_loop_detection
    }

    /// <summary>
    /// This class Implements the get-history command.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "History", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096788")]
    [OutputType(typeof(HistoryInfo))]
    public class GetHistoryCommand : PSCmdlet
    {
        /// <summary>
        /// Ids of entries to display.
        /// </summary>
        private long[] _id;

        /// <summary>
        /// Ids of entries to display.
        /// </summary>
        /// <value></value>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [ValidateRange((long)1, long.MaxValue)]
        public long[] Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Is Count parameter specified.
        /// </summary>
        private bool _countParameterSpecified;
        /// <summary>
        /// Count of entries to display. By default, count is the length of the history buffer.
        /// So "Get-History" returns all history entries.
        /// </summary>
        private int _count;

        /// <summary>
        /// No of History Entries (starting from last) that are to be displayed.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateRange(0, (int)short.MaxValue)]
        public int Count
        {
            get
            {
                return _count;
            }

            set
            {
                _countParameterSpecified = true;
                _count = value;
            }
        }

        /// <summary>
        /// Implements the Processing() method for show/History command.
        /// </summary>
        protected override void ProcessRecord()
        {
            History history = ((LocalRunspace)Context.CurrentRunspace).History;

            if (_id != null)
            {
                if (!_countParameterSpecified)
                {
                    // If Id parameter is specified and count is not specified,
                    // get history
                    foreach (long id in _id)
                    {
                        Dbg.Assert(id > 0, "ValidateRangeAttribute should not allow this");

                        HistoryInfo entry = history.GetEntry(id);

                        if (entry != null && entry.Id == id)
                        {
                            WriteObject(entry);
                        }
                        else
                        {
                            Exception ex =
                                new ArgumentException
                                (
                                    StringUtil.Format(HistoryStrings.NoHistoryForId, id)
                                );

                            WriteError
                            (
                                new ErrorRecord
                                (
                                    ex,
                                    "GetHistoryNoHistoryForId",
                                    ErrorCategory.ObjectNotFound,
                                    id
                                )
                            );
                        }
                    }
                }
                else if (_id.Length > 1)
                {
                    Exception ex =
                        new ArgumentException
                        (
                            StringUtil.Format(HistoryStrings.NoCountWithMultipleIds)
                        );

                    ThrowTerminatingError
                    (
                        new ErrorRecord
                        (
                            ex,
                            "GetHistoryNoCountWithMultipleIds",
                            ErrorCategory.InvalidArgument,
                            _count
                        )
                    );
                }
                else
                {
                    long id = _id[0];

                    Dbg.Assert(id > 0, "ValidateRangeAttribute should not allow this");
                    WriteObject(history.GetEntries(id, _count, false), true);
                }
            }
            else
            {
                // The default value for _count is the size of the history buffer.
                if (!_countParameterSpecified)
                {
                    _count = history.Buffercapacity();
                }

                HistoryInfo[] entries = history.GetEntries(0, _count, true);
                for (long i = entries.Length - 1; i >= 0; i--)
                    WriteObject(entries[i]);
            }
        }
    }

    /// <summary>
    /// This class implements the Invoke-History command.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "History", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096586")]
    public class InvokeHistoryCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Invoke cmd can execute only one history entry. If multiple
        /// ids are provided, we throw error.
        /// </summary>
        private bool _multipleIdProvided;
        private string _id;
        /// <summary>
        /// Accepts a string value indicating a previously executed command to
        /// re-execute.
        /// If string can be parsed to long,
        /// it will be used as HistoryId
        /// else
        /// as a string value indicating a previously executed command to
        /// re-execute. This string is the first n characters of the command
        /// that is to be re-executed.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (_id != null)
                {
                    // Id has been set already.
                    _multipleIdProvided = true;
                }

                _id = value;
            }
        }

        #endregion

        /// <summary>
        /// Implements the BeginProcessing() method for eval/History command.
        /// </summary>
        protected override void EndProcessing()
        {
            // Invoke-history can execute only one command. If multiple
            // ids were provided, throw exception
            if (_multipleIdProvided)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException(HistoryStrings.InvokeHistoryMultipleCommandsError),
                        "InvokeHistoryMultipleCommandsError",
                        ErrorCategory.InvalidArgument,
                        targetObject: null));
            }

            var ctxRunspace = (LocalRunspace)Context.CurrentRunspace;
            History history = ctxRunspace.History;
            Dbg.Assert(history != null, "History should be non null");

            // Get the history entry to invoke
            HistoryInfo entry = GetHistoryEntryToInvoke(history);
            string commandToInvoke = entry.CommandLine;

            if (!ShouldProcess(commandToInvoke))
            {
                return;
            }

            // Check if there is a loop in invoke-history
            if (history.PresentInInvokeHistoryEntrySet(entry))
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                        new InvalidOperationException(HistoryStrings.InvokeHistoryLoopDetected),
                        "InvokeHistoryLoopDetected",
                        ErrorCategory.InvalidOperation,
                        targetObject: null));
            }
            else
            {
                history.AddToInvokeHistoryEntrySet(entry);
            }

            // Replace Invoke-History with string which is getting invoked
            ReplaceHistoryString(entry, ctxRunspace);

            try
            {
                // Echo command
                Host.UI.WriteLine(commandToInvoke);
            }
            catch (HostException)
            {
                // when the host is not interactive, HostException is thrown
                // do nothing
            }

            // Items invoked as History should act as though they were submitted by the user - so should still come from
            // the runspace itself. For this reason, it is insufficient to just use the InvokeScript method on the Cmdlet class.
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                ps.AddScript(commandToInvoke);

                EventHandler<DataAddedEventArgs> debugAdded = (object sender, DataAddedEventArgs e) => { DebugRecord record = (DebugRecord)((PSDataCollection<DebugRecord>)sender)[e.Index]; WriteDebug(record.Message); };
                EventHandler<DataAddedEventArgs> errorAdded = (object sender, DataAddedEventArgs e) => { ErrorRecord record = (ErrorRecord)((PSDataCollection<ErrorRecord>)sender)[e.Index]; WriteError(record); };
                EventHandler<DataAddedEventArgs> informationAdded = (object sender, DataAddedEventArgs e) => { InformationRecord record = (InformationRecord)((PSDataCollection<InformationRecord>)sender)[e.Index]; WriteInformation(record); };
                EventHandler<DataAddedEventArgs> progressAdded = (object sender, DataAddedEventArgs e) => { ProgressRecord record = (ProgressRecord)((PSDataCollection<ProgressRecord>)sender)[e.Index]; WriteProgress(record); };
                EventHandler<DataAddedEventArgs> verboseAdded = (object sender, DataAddedEventArgs e) => { VerboseRecord record = (VerboseRecord)((PSDataCollection<VerboseRecord>)sender)[e.Index]; WriteVerbose(record.Message); };
                EventHandler<DataAddedEventArgs> warningAdded = (object sender, DataAddedEventArgs e) => { WarningRecord record = (WarningRecord)((PSDataCollection<WarningRecord>)sender)[e.Index]; WriteWarning(record.Message); };

                ps.Streams.Debug.DataAdded += debugAdded;
                ps.Streams.Error.DataAdded += errorAdded;
                ps.Streams.Information.DataAdded += informationAdded;
                ps.Streams.Progress.DataAdded += progressAdded;
                ps.Streams.Verbose.DataAdded += verboseAdded;
                ps.Streams.Warning.DataAdded += warningAdded;

                LocalRunspace localRunspace = ps.Runspace as LocalRunspace;

                try
                {
                    // Indicate to the system that we are in nested prompt mode, since we are emulating running the command at the prompt.
                    // This ensures that the command being run as nested runs in the correct language mode, because CreatePipelineProcessor()
                    // always forces CommandOrigin to Internal for nested running commands, and Command.CreateCommandProcessor() forces Internal
                    // commands to always run in FullLanguage mode unless in a nested prompt.
                    if (localRunspace != null)
                    {
                        localRunspace.InInternalNestedPrompt = ps.IsNested;
                    }

                    Collection<PSObject> results = ps.Invoke();
                    if (results.Count > 0)
                    {
                        WriteObject(results, true);
                    }
                }
                finally
                {
                    history.RemoveFromInvokeHistoryEntrySet(entry);

                    if (localRunspace != null)
                    {
                        localRunspace.InInternalNestedPrompt = false;
                    }

                    ps.Streams.Debug.DataAdded -= debugAdded;
                    ps.Streams.Error.DataAdded -= errorAdded;
                    ps.Streams.Information.DataAdded -= informationAdded;
                    ps.Streams.Progress.DataAdded -= progressAdded;
                    ps.Streams.Verbose.DataAdded -= verboseAdded;
                    ps.Streams.Warning.DataAdded -= warningAdded;
                }
            }
        }

        /// <summary>
        /// Helper function which gets history entry to invoke.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "It's ok to use ID in the ArgumentException")]
        private HistoryInfo GetHistoryEntryToInvoke(History history)
        {
            HistoryInfo entry = null;

            // User didn't specify any input parameter. Invoke the last
            // entry
            if (_id == null)
            {
                HistoryInfo[] entries = history.GetEntries(0, 1, true);

                if (entries.Length == 1)
                {
                    entry = entries[0];
                }
                else
                {
                    Exception ex =
                        new InvalidOperationException
                        (
                            StringUtil.Format(HistoryStrings.NoLastHistoryEntryFound)
                        );

                    ThrowTerminatingError
                    (
                        new ErrorRecord
                        (
                            ex,
                            "InvokeHistoryNoLastHistoryEntryFound",
                            ErrorCategory.InvalidOperation,
                            null
                        )
                    );
                }
            }
            else
            {
                // Parse input
                PopulateIdAndCommandLine();
                // User specified a commandline. Get list of all history entries
                // and find latest match
                if (_commandLine != null)
                {
                    HistoryInfo[] entries = history.GetEntries(0, -1, false);

                    // and search backwards through the entries
                    for (int i = entries.Length - 1; i >= 0; i--)
                    {
                        if (entries[i].CommandLine.StartsWith(_commandLine, StringComparison.Ordinal))
                        {
                            entry = entries[i];
                            break;
                        }
                    }

                    if (entry == null)
                    {
                        Exception ex =
                            new ArgumentException
                            (
                                StringUtil.Format(HistoryStrings.NoHistoryForCommandline, _commandLine)
                            );

                        ThrowTerminatingError
                        (
                            new ErrorRecord
                            (
                                ex,
                                "InvokeHistoryNoHistoryForCommandline",
                                ErrorCategory.ObjectNotFound,
                                _commandLine
                            )
                        );
                    }
                }
                else
                {
                    if (_historyId <= 0)
                    {
                        Exception ex =
                            new ArgumentOutOfRangeException
                            (
                                "Id",
                                StringUtil.Format(HistoryStrings.InvalidIdGetHistory, _historyId)
                            );

                        ThrowTerminatingError
                        (
                            new ErrorRecord
                            (
                                ex,
                                "InvokeHistoryInvalidIdGetHistory",
                                ErrorCategory.InvalidArgument,
                                _historyId
                            )
                        );
                    }
                    else
                    {
                        // Retrieve the command at the index we've specified
                        entry = history.GetEntry(_historyId);
                        if (entry == null || entry.Id != _historyId)
                        {
                            Exception ex =
                                new ArgumentException
                                (
                                    StringUtil.Format(HistoryStrings.NoHistoryForId, _historyId)
                                );

                            ThrowTerminatingError
                            (
                                new ErrorRecord
                                (
                                    ex,
                                    "InvokeHistoryNoHistoryForId",
                                    ErrorCategory.ObjectNotFound,
                                    _historyId
                                )
                            );
                        }
                    }
                }
            }

            return entry;
        }

        /// <summary>
        /// Id of history entry to execute.
        /// </summary>
        private long _historyId = -1;

        /// <summary>
        /// Commandline to execute.
        /// </summary>
        private string _commandLine;

        /// <summary>
        /// Parse Id parameter to populate _historyId and _commandLine.
        /// </summary>
        private void PopulateIdAndCommandLine()
        {
            if (_id == null)
                return;

            try
            {
                _historyId = (long)LanguagePrimitives.ConvertTo(_id, typeof(long), System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (PSInvalidCastException)
            {
                _commandLine = _id;
                return;
            }
        }

        /// <summary>
        /// Invoke-history is replaced in history by the command it executed.
        /// This replacement happens only if Invoke-History is single element
        /// in the pipeline. If there are more than one element in pipeline
        /// (ex A | Invoke-History 2 | B) then we cannot do this replacement.
        /// </summary>
        private static void ReplaceHistoryString(HistoryInfo entry, LocalRunspace localRunspace)
        {
            var pipeline = (LocalPipeline)localRunspace.GetCurrentlyRunningPipeline();
            if (pipeline.AddToHistory)
            {
                pipeline.HistoryString = entry.CommandLine;
            }
        }
    }

    /// <summary>
    /// This class Implements the add-history command.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "History", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096479")]
    [OutputType(typeof(HistoryInfo))]
    public class AddHistoryCommand : PSCmdlet
    {
        #region parameters

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public PSObject[] InputObject { get; set; }

        private bool _passthru;
        /// <summary>
        /// A Boolean that indicates whether history objects should be
        /// passed to the next element in the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter Passthru
        {
            get { return _passthru; }

            set { _passthru = value; }
        }

        #endregion parameters

        /// <summary>
        /// Override for BeginProcessing.
        /// </summary>
        protected
        override
        void BeginProcessing()
        {
            // Get currently running pipeline and add history entry for
            // this pipeline.
            // Note:Generally History entry for current pipeline is added
            // on completion of pipeline (See LocalPipeline implementation).
            // However Add-history adds additional entries in to history and
            // additional entries must be added after history for current pipeline.
            // This is done by adding the history entry for current pipeline below.
            LocalPipeline lpl = (LocalPipeline)((RunspaceBase)Context.CurrentRunspace).GetCurrentlyRunningPipeline();
            lpl.AddHistoryEntryFromAddHistoryCmdlet();
        }

        /// <summary>
        /// Override for ProcessRecord.
        /// </summary>
        protected
        override
        void ProcessRecord()
        {
            History history = ((LocalRunspace)Context.CurrentRunspace).History;
            Dbg.Assert(history != null, "History should be non null");

            if (InputObject != null)
            {
                foreach (PSObject input in InputObject)
                {
                    // Wrap the inputobject in PSObject and convert it to
                    // HistoryInfo object.
                    HistoryInfo infoToAdd = GetHistoryInfoObject(input);
                    if (infoToAdd != null)
                    {
                        long id = history.AddEntry
                                  (
                                    0,
                                    infoToAdd.CommandLine,
                                    infoToAdd.ExecutionStatus,
                                    infoToAdd.StartExecutionTime,
                                    infoToAdd.EndExecutionTime,
                                    false
                                  );

                        if (Passthru)
                        {
                            HistoryInfo infoAdded = history.GetEntry(id);
                            WriteObject(infoAdded);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert mshObject that has the properties of an HistoryInfo
        /// object in to HistoryInfo object.
        /// </summary>
        /// <param name="mshObject">
        /// mshObject to be converted to HistoryInfo.
        /// </param>
        /// <returns>
        /// HistoryInfo object if conversion is successful else null.
        /// </returns>
#pragma warning disable 0162
        private
        HistoryInfo
        GetHistoryInfoObject(PSObject mshObject)
        {
            do
            {
                if (mshObject == null)
                {
                    break;
                }

                // Read CommandLine property
                if (GetPropertyValue(mshObject, "CommandLine") is not string commandLine)
                {
                    break;
                }

                // Read ExecutionStatus property
                object pipelineState = GetPropertyValue(mshObject, "ExecutionStatus");
                if (pipelineState == null || !LanguagePrimitives.TryConvertTo<PipelineState>(pipelineState, out PipelineState executionStatus))
                {
                    break;
                }

                // Read StartExecutionTime property
                object temp = GetPropertyValue(mshObject, "StartExecutionTime");
                if (temp == null || !LanguagePrimitives.TryConvertTo<DateTime>(temp, CultureInfo.CurrentCulture, out DateTime startExecutionTime))
                {
                    break;
                }

                // Read EndExecutionTime property
                temp = GetPropertyValue(mshObject, "EndExecutionTime");
                if (temp == null || !LanguagePrimitives.TryConvertTo<DateTime>(temp, CultureInfo.CurrentCulture, out DateTime endExecutionTime))
                {
                    break;
                }

                return new HistoryInfo(
                    pipelineId: 0,
                    commandLine,
                    executionStatus,
                    startExecutionTime,
                    endExecutionTime
                );
            } while (false);

            // If we are here, an error has occurred.
            Exception ex =
                new InvalidDataException
                (
                    StringUtil.Format(HistoryStrings.AddHistoryInvalidInput)
                );

            WriteError
            (
                new ErrorRecord
                (
                    ex,
                    "AddHistoryInvalidInput",
                    ErrorCategory.InvalidData,
                    mshObject
                )
            );

            return null;
        }
#pragma warning restore 0162

        private static
        object
        GetPropertyValue(PSObject mshObject, string propertyName)
        {
            PSMemberInfo propertyInfo = mshObject.Properties[propertyName];
            if (propertyInfo == null)
                return null;
            return propertyInfo.Value;
        }
    }

    /// <summary>
    /// This Class implements the Clear History cmdlet
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "History", SupportsShouldProcess = true, DefaultParameterSetName = "IDParameter", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096691")]
    public class ClearHistoryCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Specifies the ID of a command in the session history.Clear history clears the entries
        /// wit the specified ID(s)
        /// </summary>
        [Parameter(ParameterSetName = "IDParameter", Position = 0,
           HelpMessage = "Specifies the ID of a command in the session history.Clear history clears only the specified command")]
        [ValidateRange((int)1, int.MaxValue)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public int[] Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Id of a history entry.
        /// </summary>
        private int[] _id;

        /// <summary>
        /// Command line name of an entry in the session history.
        /// </summary>
        [Parameter(ParameterSetName = "CommandLineParameter", HelpMessage = "Specifies the name of a command in the session history")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CommandLine
        {
            get
            {
                return _commandline;
            }

            set
            {
                _commandline = value;
            }
        }

        /// <summary>
        /// Commandline parameter.
        /// </summary>
        private string[] _commandline = null;

        /// <summary>
        /// Clears the specified number of history entries
        /// </summary>
        [Parameter(Mandatory = false, Position = 1, HelpMessage = "Clears the specified number of history entries")]
        [ValidateRange((int)1, int.MaxValue)]
        public int Count
        {
            get
            {
                return _count;
            }

            set
            {
                _countParameterSpecified = true;
                _count = value;
            }
        }

        /// <summary>
        /// Count of the history entries.
        /// </summary>
        private int _count = 32;

        /// <summary>
        /// A boolean variable to indicate if the count parameter specified.
        /// </summary>
        private bool _countParameterSpecified = false;

        /// <summary>
        /// Specifies whether new entries to be cleared or the default old ones.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Specifies whether new entries to be cleared or the default old ones.")]
        public SwitchParameter Newest
        {
            get
            {
                return _newest;
            }

            set
            {
                _newest = value;
            }
        }

        /// <summary>
        /// Switch parameter on the history entries.
        /// </summary>
        private SwitchParameter _newest;

        #endregion Command Line Parameters

        /// <summary>
        /// Overriding Begin Processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            _history = ((LocalRunspace)Context.CurrentRunspace).History;
        }

        /// <summary>
        /// Overriding Process Record.
        /// </summary>
        protected override void ProcessRecord()
        {
            // case statement to identify the parameter set
            switch (ParameterSetName)
            {
                case "IDParameter":
                    ClearHistoryByID();
                    break;
                case "CommandLineParameter":
                    ClearHistoryByCmdLine();
                    break;
                default:
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new ArgumentException("Invalid ParameterSet Name"),
                            "Unable to access the session history", ErrorCategory.InvalidOperation, null));
                    return;
            }
        }

        #region Private

        /// <summary>
        /// Clears the session history based on the id parameter
        /// takes no parameters
        /// </summary>
        /// <returns>Nothing.</returns>
        private void ClearHistoryByID()
        {
            if (_countParameterSpecified && Count < 0)
            {
                Exception ex =
                   new ArgumentException
                   (
                       StringUtil.Format("HistoryStrings", "InvalidCountValue")
                   );
                ThrowTerminatingError
                (
                    new ErrorRecord
                    (
                        ex,
                        "ClearHistoryInvalidCountValue",
                        ErrorCategory.InvalidArgument,
                        _count
                    )
                );
            }
            // if id parameter is not present
            if (_id != null)
            {
                // if count parameter is not present
                if (!_countParameterSpecified)
                {
                    // clearing the entry for each id in the id[] parameter.
                    foreach (long id in _id)
                    {
                        Dbg.Assert(id > 0, "ValidateRangeAttribute should not allow this");
                        HistoryInfo entry = _history.GetEntry(id);
                        if (entry != null && entry.Id == id)
                        {
                            _history.ClearEntry(entry.Id);
                        }
                        else
                        {// throw an exception if an entry for an id is not found
                            Exception ex =
                                new ArgumentException
                                (
                                    StringUtil.Format(HistoryStrings.NoHistoryForId, id)
                                );
                            WriteError
                            (
                                new ErrorRecord
                                (
                                    ex,
                                    "GetHistoryNoHistoryForId",
                                    ErrorCategory.ObjectNotFound,
                                    id
                                )
                            );
                        }
                    }
                }
                else if (_id.Length > 1)
                {// throwing an exception for invalid parameter combinations
                    Exception ex =
                        new ArgumentException
                        (
                            StringUtil.Format(HistoryStrings.NoCountWithMultipleIds)
                        );

                    ThrowTerminatingError
                    (
                        new ErrorRecord
                        (
                            ex,
                            "GetHistoryNoCountWithMultipleIds",
                            ErrorCategory.InvalidArgument,
                            _count
                        )
                    );
                }
                else
                {// if id,count and newest parameters are present
                    // throw an exception for invalid count values

                    long id = _id[0];
                    Dbg.Assert(id > 0, "ValidateRangeAttribute should not allow this");
                    ClearHistoryEntries(id, _count, null, _newest);
                }
            }
            else
            {
                // confirmation message if all the clearhistory cmdlet is used without any parameters
                if (!_countParameterSpecified)
                {
                    string message = StringUtil.Format(HistoryStrings.ClearHistoryWarning, "Warning"); // "The command would clear all the entry(s) from the session history,Are you sure you want to continue ?";
                    if (!ShouldProcess(message))
                    {
                        return;
                    }

                    ClearHistoryEntries(0, -1, null, _newest);
                }
                else
                {
                    ClearHistoryEntries(0, _count, null, _newest);
                }
            }
        }

        /// <summary>
        /// Clears the session history based on the Commandline parameter
        /// takes no parameters
        /// </summary>
        /// <returns>Nothing.</returns>
        private void ClearHistoryByCmdLine()
        {
            // throw an exception for invalid count values
            if (_countParameterSpecified && Count < 0)
            {
                Exception ex =
                   new ArgumentException
                   (
                       StringUtil.Format(HistoryStrings.InvalidCountValue)
                   );

                ThrowTerminatingError
                (
                    new ErrorRecord
                    (
                        ex,
                        "ClearHistoryInvalidCountValue",
                        ErrorCategory.InvalidArgument,
                        _count
                    )
                );
            }
            // if command line is not present
            if (_commandline != null)
            {
                // if count parameter is not present
                if (!_countParameterSpecified)
                {
                    foreach (string cmd in _commandline)
                    {
                        ClearHistoryEntries(0, 1, cmd, _newest);
                    }
                }
                else if (_commandline.Length > 1)
                {// throwing exceptions for invalid parameter combinations
                    Exception ex =
                        new ArgumentException
                        (
                            StringUtil.Format(HistoryStrings.NoCountWithMultipleCmdLine)
                        );

                    ThrowTerminatingError
                    (
                        new ErrorRecord
                        (
                            ex,
                            "NoCountWithMultipleCmdLine ",
                            ErrorCategory.InvalidArgument,
                            _commandline
                        )
                    );
                }
                else
                {   // if commandline,count and newest parameters are present.
                    ClearHistoryEntries(0, _count, _commandline[0], _newest);
                }
            }
        }

        /// <summary>
        /// Clears the session history based on the input parameter
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <param name="id">Id of the entry to be cleared.</param>
        /// <param name="count">Count of entries to be cleared.</param>
        /// <param name="cmdline">Cmdline string to be cleared.</param>
        /// <param name="newest">Order of the entries.</param>
        private void ClearHistoryEntries(long id, int count, string cmdline, SwitchParameter newest)
        {
            // if cmdline is null,use default parameter set notion.
            if (cmdline == null)
            {
                // if id is present,clears count entries from id
                if (id > 0)
                {
                    HistoryInfo entry = _history.GetEntry(id);
                    if (entry == null || entry.Id != id)
                    {
                        Exception ex =
                                   new ArgumentException
                                   (
                                       StringUtil.Format(HistoryStrings.NoHistoryForId, id)
                                   );
                        WriteError
                        (
                            new ErrorRecord
                            (
                                ex,
                                "GetHistoryNoHistoryForId",
                                ErrorCategory.ObjectNotFound,
                                id
                            )
                        );
                    }

                    _entries = _history.GetEntries(id, count, newest);
                }
                else
                {// if only count is present
                    _entries = _history.GetEntries(0, count, newest);
                }
            }
            else
            {
                // creates a wild card pattern
                WildcardPattern wildcardpattern = WildcardPattern.Get(cmdline, WildcardOptions.IgnoreCase);
                // count set to zero if not specified.
                if (!_countParameterSpecified && WildcardPattern.ContainsWildcardCharacters(cmdline))
                {
                    count = 0;
                }
                // Return the matching history entries for the command line parameter
                // if newest id false...gets the oldest entry
                _entries = _history.GetEntries(wildcardpattern, count, newest);
            }

            // Clear the History value.
            foreach (HistoryInfo entry in _entries)
            {
                if (entry != null && !entry.Cleared)
                    _history.ClearEntry(entry.Id);
            }

            return;
        }

        /// <summary>
        /// History obj.
        /// </summary>
        private History _history;

        /// <summary>
        /// Array of historyinfo objects.
        /// </summary>
        private HistoryInfo[] _entries;

        #endregion Private
    }
}
