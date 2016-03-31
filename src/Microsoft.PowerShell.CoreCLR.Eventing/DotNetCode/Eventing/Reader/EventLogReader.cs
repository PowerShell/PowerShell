// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLogReader
**
** Purpose: 
** This public class is used for reading event records from event log. 
**
============================================================*/

using System.IO;
using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// This public class is used for reading event records from event log.  
    /// </summary>
    public class EventLogReader : IDisposable {

        private EventLogQuery eventQuery;

        private int batchSize;

        //
        // access to the data member reference is safe, while 
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle handle;

        /// <summary>
        /// events buffer holds batched event (handles).
        /// </summary>
        private IntPtr[] eventsBuffer;
        /// <summary>
        /// The current index where the function GetNextEvent is (inside the eventsBuffer).
        /// </summary>
        private int currentIndex;
        /// <summary>
        /// The number of events read from the batch into the eventsBuffer
        /// </summary>
        private int eventCount;

        /// <summary>
        /// When the reader finishes (will always return only ERROR_NO_MORE_ITEMS).
        /// For subscription, this means we need to wait for next event.
        /// </summary>
        bool isEof;

        /// <summary>
        /// Maintains cached display / metadata information returned from 
        /// EventRecords that were obtained from this reader.
        /// </summary>
        ProviderMetadataCachedInformation cachedMetadataInformation;  

        public EventLogReader(string path)
            : this(new EventLogQuery(path, PathType.LogName), null) {
        }

        public EventLogReader(string path, PathType pathType)
            : this(new EventLogQuery(path, pathType), null) {
        }

        public EventLogReader(EventLogQuery eventQuery)
            : this(eventQuery, null) {
        }

        [System.Security.SecurityCritical]
        public EventLogReader(EventLogQuery eventQuery, EventBookmark bookmark) {

            if (eventQuery == null)
                throw new ArgumentNullException("eventQuery");

            string logfile = null;
            if (eventQuery.ThePathType == PathType.FilePath)
                logfile = eventQuery.Path;

            this.cachedMetadataInformation = new ProviderMetadataCachedInformation(eventQuery.Session, logfile, 50 );

            //explicit data
            this.eventQuery = eventQuery;

            //implicit
            this.batchSize = 64;
            this.eventsBuffer = new IntPtr[batchSize];

            //
            // compute the flag.
            //
            int flag = 0;

            if (this.eventQuery.ThePathType == PathType.LogName)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryChannelPath;
            else
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryFilePath;

            if (this.eventQuery.ReverseDirection)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryReverseDirection;

            if (this.eventQuery.TolerateQueryErrors)
                flag |= (int)UnsafeNativeMethods.EvtQueryFlags.EvtQueryTolerateQueryErrors;
            
            handle = NativeWrapper.EvtQuery(this.eventQuery.Session.Handle,
                this.eventQuery.Path, this.eventQuery.Query,
                flag);

            EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark);

            if (!bookmarkHandle.IsInvalid) {
                using (bookmarkHandle) {
                    NativeWrapper.EvtSeek(handle, 1, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
                }
            }
        }

        public int BatchSize {
            get {
                return batchSize;
            }
            set {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value");
                batchSize = value;
            }
        }

        [System.Security.SecurityCritical]
        private bool GetNextBatch(TimeSpan ts) {

            int timeout;
            if (ts == TimeSpan.MaxValue)
                timeout = -1;
            else
                timeout = (int)ts.TotalMilliseconds;

            // batchSize was changed by user, reallocate buffer.
            if (batchSize != eventsBuffer.Length) eventsBuffer = new IntPtr[batchSize];

            int newEventCount = 0;
            bool results = NativeWrapper.EvtNext(handle, batchSize, eventsBuffer, timeout, 0, ref newEventCount);

            if (!results) {
                this.eventCount = 0;
                this.currentIndex = 0;
                return false; //no more events in the result set
            }

            this.currentIndex = 0;
            this.eventCount = newEventCount;
            return true;
        }

        public EventRecord ReadEvent() {
            return ReadEvent(TimeSpan.MaxValue);
        }

        // security critical because allocates SafeHandle.
        // marked as safe because performs Demand check.
        [System.Security.SecurityCritical]
        public EventRecord ReadEvent(TimeSpan timeout) {

            if (this.isEof)
                throw new InvalidOperationException();

            if (this.currentIndex >= this.eventCount) {
                // buffer is empty, get next batch.
                GetNextBatch(timeout);

                if (this.currentIndex >= this.eventCount) {
                    this.isEof = true;
                    return null;
                }
            }

            EventLogRecord eventInstance = new EventLogRecord(new EventLogHandle(this.eventsBuffer[currentIndex], true), this.eventQuery.Session, this.cachedMetadataInformation);
            currentIndex++;
            return eventInstance;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing) {
            
            while (this.currentIndex < this.eventCount) {
                NativeWrapper.EvtClose(eventsBuffer[this.currentIndex]);
                this.currentIndex++;
            }

            if (handle != null && !handle.IsInvalid)   
                handle.Dispose();
        }

        [System.Security.SecurityCritical]
        internal void SeekReset() {
            //
            //close all unread event handles in the buffer
            //
            while (this.currentIndex < this.eventCount) {
                NativeWrapper.EvtClose(eventsBuffer[this.currentIndex]);
                this.currentIndex++;
            }

            //reset the indexes used by Next
            this.currentIndex = 0;
            this.eventCount = 0;
            this.isEof = false;
        }

        // marked as SecurityCritical because it allocates SafeHandle.
        [System.Security.SecurityCritical]
        internal void SeekCommon(long offset) {

            //
            // modify offset that we're going to send to service to account for the
            // fact that we've already read some events in our buffer that the user
            // hasn't seen yet.
            //  
            offset = offset - (this.eventCount - this.currentIndex);

            SeekReset();

            NativeWrapper.EvtSeek(this.handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToCurrent);
        }

        public void Seek(EventBookmark bookmark) {
            Seek(bookmark, 0);
        }

        [System.Security.SecurityCritical]
        public void Seek(EventBookmark bookmark, long offset) {
            if (bookmark == null)
                throw new ArgumentNullException("bookmark");
            
            SeekReset();
            using (EventLogHandle bookmarkHandle = EventLogRecord.GetBookmarkHandleFromBookmark(bookmark)) {
                NativeWrapper.EvtSeek(this.handle, offset, bookmarkHandle, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToBookmark);
            }
        }

        [System.Security.SecurityCritical]
        public void Seek(SeekOrigin origin, long offset) {
            
            switch (origin) {
                case SeekOrigin.Begin:

                    SeekReset();
                    NativeWrapper.EvtSeek(this.handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToFirst);
                    return;

                case SeekOrigin.End:

                    SeekReset();
                    NativeWrapper.EvtSeek(this.handle, offset, EventLogHandle.Zero, 0, UnsafeNativeMethods.EvtSeekFlags.EvtSeekRelativeToLast);
                    return;

                case SeekOrigin.Current:
                    if (offset >= 0) {
                        //we can reuse elements in the batch.
                        if (this.currentIndex + offset < this.eventCount) {
                            // 
                            // We don't call Seek here, we can reposition within the batch.
                            //

                            // close all event handles between [currentIndex, currentIndex + offset)
                            int index = this.currentIndex;
                            while (index < this.currentIndex + offset) {
                                NativeWrapper.EvtClose(eventsBuffer[index]);
                                index++;
                            }

                            this.currentIndex = (int)(this.currentIndex + offset);
                            //leave the eventCount unchanged
                            //leave the same Eof
                        }
                        else {
                            SeekCommon(offset);
                        }
                    }
                    else {
                        //if inside the current buffer, we still cannot read the events, as the handles.
                        //may have already been closed.
                        if (currentIndex + offset >= 0) {
                            SeekCommon(offset);
                        }
                        else  //outside the current buffer
                        {
                            SeekCommon(offset);
                        }
                    }
                    return;
            }
        }

        public void CancelReading() {

            NativeWrapper.EvtCancel(handle);
        }
    
        public IList<EventLogStatus> LogStatus {
            [System.Security.SecurityCritical]
            get {
                List<EventLogStatus> list = null;
                string[] channelNames = null;
                int[] errorStatuses = null;
                EventLogHandle queryHandle = this.handle;

                if (queryHandle.IsInvalid)
                    throw new InvalidOperationException();

                channelNames = (string[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryNames);
                errorStatuses = (int[])NativeWrapper.EvtGetQueryInfo(queryHandle, UnsafeNativeMethods.EvtQueryPropertyId.EvtQueryStatuses);

                if (channelNames.Length != errorStatuses.Length)
                    throw new InvalidOperationException();

                list = new List<EventLogStatus>(channelNames.Length);
                for (int i = 0; i < channelNames.Length; i++) {
                    EventLogStatus cs = new EventLogStatus(channelNames[i], errorStatuses[i]);
                    list.Add(cs);
                }
                return list.AsReadOnly();
            }
        } 
    }
}
