// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class is used for reading event records from event log.
**
============================================================*/

using System.IO;
using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// This public class is used for reading event records from event log.
    /// </summary>
    public class EventLogReader : IDisposable
    {
        private EventLogQuery _eventQuery;

        private int _batchSize;

        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle _handle;

        /// <summary>
        /// Events buffer holds batched event (handles).
        /// </summary>
        private IntPtr[] _eventsBuffer;
        /// <summary>
        /// The current index where the function GetNextEvent is (inside the eventsBuffer).
        /// </summary>
        private int _currentIndex;
        /// <summary>
        /// The number of events read from the batch into the eventsBuffer.
        /// </summary>
        private int _eventCount;

        /// <summary>
        /// When the reader finishes (will always return only ERROR_NO_MORE_ITEMS).
        /// For subscription, this means we need to wait for next event.
        /// </summary>
        private bool _isEof;

        /// <summary>
        /// Maintains cached display / metadata information returned from
        /// EventRecords that were obtained from this reader.
        /// </summary>
        private ProviderMetadataCachedInformation _cachedMetadataInformation;

        public EventLogReader(string path)
            : this(new EventLogQuery(path, PathType.LogName), null)
        {
        }

        public EventLogReader(string path, PathType pathType)
            : this(new EventLogQuery(path, pathType), null)
        {
        }

        public EventLogReader(EventLogQuery eventQuery)
            : this(eventQuery, null)
        {
        }

        [System.Security.SecurityCritical]
        public EventLogReader(EventLogQuery eventQuery, EventBookmark bookmark)
        {
            if (eventQuery == null)
                throw new ArgumentNullException("eventQuery");

            string logfile = null;
            if (eventQuery.ThePathType == PathType.FilePath)
                logfile = eventQuery.Path;

            _cachedMetadataInformation = new ProviderMetadataCachedInformation(eventQuery.Session, logfile, 50);

            // explicit data
            _eventQuery = eventQuery;

            // implicit
            _batchSize = 64;
            _eventsBuffer = new IntPtr[_batchSize];

            //
            // compute the flag.
            //
            int flag = 0;

            if (_eventQuery.ThePathType == PathType.LogName)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryChannelPath;
            else
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryFilePath;

            if (_eventQuery.ReverseDirection)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryReverseDirection;

            if (_eventQuery.TolerateQueryErrors)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryTolerateQueryErrors;

            _handle = NativeWrapper.EvtQuery(_eventQuery.Session.Handle,
                _eventQuery.Path, _eventQuery.Query,
                flag);

            EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark);

            if (!bookmarkHandle.IsInvalid)
            {
                using (bookmarkHandle)
                {
                    NativeWrapper.EvtSeek(_handle, 1, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
                }
            }
        }

        public int BatchSize
        {
            get
            {
                return _batchSize;
            }

            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value");
                _batchSize = value;
            }
        }

        [System.Security.SecurityCritical]
        private bool GetNextBatch(TimeSpan ts)
        {
            int timeout;
            if (ts == TimeSpan.MaxValue)
                timeout = -1;
            else
                timeout = (int)ts.TotalMilliseconds;

            // batchSize was changed by user, reallocate buffer.
            if (_batchSize != _eventsBuffer.Length) _eventsBuffer = new IntPtr[_batchSize];

            int newEventCount = 0;
            bool results = NativeWrapper.EvtNext(_handle, _batchSize, _eventsBuffer, timeout, 0, ref newEventCount);

            if (!results)
            {
                _eventCount = 0;
                _currentIndex = 0;
                return false; // no more events in the result set
            }

            _currentIndex = 0;
            _eventCount = newEventCount;
            return true;
        }

        public EventRecord ReadEvent()
        {
            return ReadEvent(TimeSpan.MaxValue);
        }

        // security critical because allocates SafeHandle.
        // marked as safe because performs Demand check.
        [System.Security.SecurityCritical]
        public EventRecord ReadEvent(TimeSpan timeout)
        {
            if (_isEof)
                throw new InvalidOperationException();

            if (_currentIndex >= _eventCount)
            {
                // buffer is empty, get next batch.
                GetNextBatch(timeout);

                if (_currentIndex >= _eventCount)
                {
                    _isEof = true;
                    return null;
                }
            }

            EventLogRecord eventInstance = new EventLogRecord(new EventLogHandle(_eventsBuffer[_currentIndex], true), _eventQuery.Session, _cachedMetadataInformation);
            _currentIndex++;
            return eventInstance;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            while (_currentIndex < _eventCount)
            {
                NativeWrapper.EvtClose(_eventsBuffer[_currentIndex]);
                _currentIndex++;
            }

            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }

        [System.Security.SecurityCritical]
        internal void SeekReset()
        {
            //
            // close all unread event handles in the buffer
            //
            while (_currentIndex < _eventCount)
            {
                NativeWrapper.EvtClose(_eventsBuffer[_currentIndex]);
                _currentIndex++;
            }

            // reset the indexes used by Next
            _currentIndex = 0;
            _eventCount = 0;
            _isEof = false;
        }

        // marked as SecurityCritical because it allocates SafeHandle.
        [System.Security.SecurityCritical]
        internal void SeekCommon(long offset)
        {
            //
            // modify offset that we're going to send to service to account for the
            // fact that we've already read some events in our buffer that the user
            // hasn't seen yet.
            //
            offset = offset - (_eventCount - _currentIndex);

            SeekReset();

            NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToCurrent);
        }

        public void Seek(EventBookmark bookmark)
        {
            Seek(bookmark, 0);
        }

        [System.Security.SecurityCritical]
        public void Seek(EventBookmark bookmark, long offset)
        {
            if (bookmark == null)
                throw new ArgumentNullException("bookmark");

            SeekReset();
            using (EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark))
            {
                NativeWrapper.EvtSeek(_handle, offset, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
            }
        }

        [System.Security.SecurityCritical]
        public void Seek(SeekOrigin origin, long offset)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:

                    SeekReset();
                    NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToFirst);
                    return;

                case SeekOrigin.End:

                    SeekReset();
                    NativeWrapper.EvtSeek(_handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToLast);
                    return;

                case SeekOrigin.Current:
                    if (offset >= 0)
                    {
                        // we can reuse elements in the batch.
                        if (_currentIndex + offset < _eventCount)
                        {
                            //
                            // We don't call Seek here, we can reposition within the batch.
                            //

                            // close all event handles between [currentIndex, currentIndex + offset)
                            int index = _currentIndex;
                            while (index < _currentIndex + offset)
                            {
                                NativeWrapper.EvtClose(_eventsBuffer[index]);
                                index++;
                            }

                            _currentIndex = (int)(_currentIndex + offset);
                            // leave the eventCount unchanged
                            // leave the same Eof
                        }
                        else
                        {
                            SeekCommon(offset);
                        }
                    }
                    else
                    {
                        SeekCommon(offset);
                    }

                    return;
            }
        }

        public void CancelReading()
        {
            NativeWrapper.EvtCancel(_handle);
        }

        public IList<EventLogStatus> LogStatus
        {
            [System.Security.SecurityCritical]
            get
            {
                List<EventLogStatus> list = null;
                string[] channelNames = null;
                int[] errorStatuses = null;
                EventLogHandle queryHandle = _handle;

                if (queryHandle.IsInvalid)
                    throw new InvalidOperationException();

                channelNames = (string[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryNames);
                errorStatuses = (int[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryStatuses);

                if (channelNames.Length != errorStatuses.Length)
                    throw new InvalidOperationException();

                list = new List<EventLogStatus>(channelNames.Length);
                for (int i = 0; i < channelNames.Length; i++)
                {
                    EventLogStatus cs = new EventLogStatus(channelNames[i], errorStatuses[i]);
                    list.Add(cs);
                }

                return list.AsReadOnly();
            }
        }
    }
}
