// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLogInformation
**
** Purpose: 
** The objects of this class allow access to the run-time 
** properties of logs and external log files. An instance of this 
** class is obtained from EventLogSession.
** 
============================================================*/

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Describes the run-time properties of logs and external log files.  An instance
    /// of this class is obtained from EventLogSession.
    /// </summary>
    public sealed class EventLogInformation {
        DateTime? creationTime;
        DateTime? lastAccessTime;
        DateTime? lastWriteTime;
        long? fileSize;
        int? fileAttributes;
        long? recordCount;
        long? oldestRecordNumber;
        bool? isLogFull;


        [System.Security.SecuritySafeCritical]
        internal EventLogInformation(EventLogSession session, string channelName, PathType pathType) {
            
            EventLogHandle logHandle = NativeWrapper.EvtOpenLog(session.Handle, channelName, pathType);

            using (logHandle) {
                creationTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogCreationTime);
                lastAccessTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogLastAccessTime);
                lastWriteTime = (DateTime?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogLastWriteTime);
                fileSize = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogFileSize));
                fileAttributes = (int?)((uint?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogAttributes));
                recordCount = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogNumberOfLogRecords));
                oldestRecordNumber = (long?)((ulong?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogOldestRecordNumber));
                isLogFull = (bool?)NativeWrapper.EvtGetLogInfo(logHandle, UnsafeNativeMethods.EvtLogPropertyId.EvtLogFull);
            }
        }

        public DateTime? CreationTime { get { return creationTime; } }
        public DateTime? LastAccessTime { get { return lastAccessTime; } }
        public DateTime? LastWriteTime { get { return lastWriteTime; } }
        public long? FileSize { get { return fileSize; } }
        public int? Attributes { get { return fileAttributes; } }
        public long? RecordCount { get { return recordCount; } }
        public long? OldestRecordNumber { get { return oldestRecordNumber; } }
        public bool? IsLogFull { get { return isLogFull; } }
    }
}
