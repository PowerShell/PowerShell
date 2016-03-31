// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLogRecord
**
** Purpose: 
** This public class is an EventLog implementation of EventRecord.  An
** instance of this is obtained from an EventLogReader.
**
============================================================*/

using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics.Eventing.Reader {
    
    public class EventLogRecord : EventRecord {

        private const int SYSTEM_PROPERTY_COUNT = 18;

        //
        // access to the data member reference is safe, while 
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        [System.Security.SecuritySafeCritical]
        private EventLogHandle handle;

        private EventLogSession session;

        private NativeWrapper.SystemProperties systemProperties;
        private string containerChannel;
        private int[] matchedQueryIds;

        //a dummy object which is used only for the locking.
        object syncObject;

        //cached DisplayNames for each instance
        private string levelName = null;
        private string taskName = null;
        private string opcodeName = null;
        private IEnumerable<string> keywordsNames = null;

        //cached DisplayNames for each instance
        private bool levelNameReady;
        private bool taskNameReady;
        private bool opcodeNameReady;

        private ProviderMetadataCachedInformation cachedMetadataInformation;

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        internal EventLogRecord(EventLogHandle handle, EventLogSession session, ProviderMetadataCachedInformation cachedMetadataInfo) {
            this.cachedMetadataInformation = cachedMetadataInfo;
            this.handle = handle;
            this.session = session;
            systemProperties = new NativeWrapper.SystemProperties();
            syncObject = new object();
        }

        internal EventLogHandle Handle {
            // just returning reference to security critical type, the methods
            // of that type are protected by SecurityCritical as appropriate.
            [System.Security.SecuritySafeCritical]
            get {
                return handle;
            }
        }


        internal void PrepareSystemData() {

            if (this.systemProperties.filled)
                return;

            //prepare the System Context, if it is not already initialized.
            this.session.SetupSystemContext();

            lock (this.syncObject) {
                if (this.systemProperties.filled == false) {
                    NativeWrapper.EvtRenderBufferWithContextSystem(this.session.renderContextHandleSystem, this.handle, UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventValues, this.systemProperties, SYSTEM_PROPERTY_COUNT);
                    this.systemProperties.filled = true;
                }
            }
        }

        public override int Id {
            get {
                PrepareSystemData();
                if (this.systemProperties.Id == null)
                    return 0;
                return (int)this.systemProperties.Id;
            }
        }

        public override byte? Version {
            get {
                PrepareSystemData();
                return this.systemProperties.Version;
            }
        }

        public override int? Qualifiers {
            get {
                PrepareSystemData();
                return (int?)(uint?)this.systemProperties.Qualifiers;
            }
        }

        public override byte? Level {
            get {
                PrepareSystemData();
                return this.systemProperties.Level;
            }
        }

        public override int? Task {
            get {
                PrepareSystemData();
                return (int?)(uint?)this.systemProperties.Task;
            }
        }

        public override short? Opcode {
            get {
                PrepareSystemData();
                return (short?)(ushort?)this.systemProperties.Opcode;
            }
        }

        public override long? Keywords {
            get {
                PrepareSystemData();
                return (long?)this.systemProperties.Keywords;
            }
        }

        public override long? RecordId {
            get {
                PrepareSystemData();
                return (long?)this.systemProperties.RecordId;
            }
        }

        public override string ProviderName {
            get {
                PrepareSystemData();
                return this.systemProperties.ProviderName;
            }
        }

        public override Guid? ProviderId {
            get {
                PrepareSystemData();
                return this.systemProperties.ProviderId;
            }
        }

        public override string LogName {
            get {
                PrepareSystemData();
                return this.systemProperties.ChannelName;
            }
        }

        public override int? ProcessId {
            get {
                PrepareSystemData();
                return (int?)this.systemProperties.ProcessId;
            }
        }

        public override int? ThreadId {
            get {
                PrepareSystemData();
                return (int?)this.systemProperties.ThreadId;
            }
        }

        public override string MachineName {
            get {
                PrepareSystemData();
                return this.systemProperties.ComputerName;
            }
        }

        public override System.Security.Principal.SecurityIdentifier UserId {
            get {
                PrepareSystemData();
                return this.systemProperties.UserId;
            }
        }

        public override DateTime? TimeCreated {
            get {
                PrepareSystemData();
                return this.systemProperties.TimeCreated;
            }
        }

        public override Guid? ActivityId {
            get {
                PrepareSystemData();
                return this.systemProperties.ActivityId;
            }
        }

        public override Guid? RelatedActivityId {
            get {
                PrepareSystemData();
                return this.systemProperties.RelatedActivityId;
            }
        }

        public string ContainerLog {
            get {
                if (this.containerChannel != null)
                    return this.containerChannel;
                lock (this.syncObject) {
                    if (this.containerChannel == null) {
                        this.containerChannel = (string)NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventPath);
                    }
                    return this.containerChannel;
                }
            }
        }

        public IEnumerable<int> MatchedQueryIds {
            get {
                if (this.matchedQueryIds != null)
                    return this.matchedQueryIds;
                lock (this.syncObject) {
                    if (this.matchedQueryIds == null) {
                        this.matchedQueryIds = (int[])NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventQueryIDs);
                    }
                    return this.matchedQueryIds;
                }
            }
        }

        
        public override EventBookmark Bookmark {
            [System.Security.SecuritySafeCritical]
            get {

                EventLogHandle bookmarkHandle = NativeWrapper.EvtCreateBookmark(null);
                NativeWrapper.EvtUpdateBookmark(bookmarkHandle, this.handle);
                string bookmarkText = NativeWrapper.EvtRenderBookmark(bookmarkHandle);

                return new EventBookmark(bookmarkText);
            }
        }

        public override string FormatDescription() {

            return this.cachedMetadataInformation.GetFormatDescription(this.ProviderName, this.handle);
        }

        public override string FormatDescription(IEnumerable<object> values) {
            if (values == null) return this.FormatDescription();

            //copy the value IEnumerable to an array.
            string[] theValues = new string[0];
            int i = 0;
            foreach (object o in values) {
                if ( theValues.Length == i )
                    Array.Resize( ref theValues, i+1 );
                theValues[i] = o.ToString();
                i++;
            } 

            return this.cachedMetadataInformation.GetFormatDescription(this.ProviderName, this.handle, theValues);
        }

        public override string LevelDisplayName {
            get {
                if ( this.levelNameReady )
                    return this.levelName;
                lock (this.syncObject) {
                    if (this.levelNameReady == false) {
                        this.levelNameReady = true;
                        this.levelName = this.cachedMetadataInformation.GetLevelDisplayName(this.ProviderName, this.handle);
                    }
                    return this.levelName;
                }
            }
        }


        public override string OpcodeDisplayName {
            get {
                lock (this.syncObject) {
                    if (this.opcodeNameReady == false) {
                        this.opcodeNameReady = true;
                        this.opcodeName = this.cachedMetadataInformation.GetOpcodeDisplayName(this.ProviderName, this.handle);

                    }
                    return this.opcodeName;
                }
            }
        }

        public override string TaskDisplayName {
            get {
                if (this.taskNameReady == true)
                    return this.taskName;
                lock (this.syncObject) {
                    if (this.taskNameReady == false) {
                        this.taskNameReady = true;
                        this.taskName = this.cachedMetadataInformation.GetTaskDisplayName(this.ProviderName, this.handle);
                    }
                    return this.taskName;
                }
            }
        }

        public override IEnumerable<string> KeywordsDisplayNames {
            get {
                if (this.keywordsNames != null)
                    return this.keywordsNames;
                lock (this.syncObject) {
                    if (this.keywordsNames == null) {
                        this.keywordsNames = this.cachedMetadataInformation.GetKeywordDisplayNames(this.ProviderName, this.handle);
                    }
                    return this.keywordsNames;
                }
            }
        }


        public override IList<EventProperty> Properties {
            get {
                this.session.SetupUserContext();
                IList<object> properties = NativeWrapper.EvtRenderBufferWithContextUserOrValues(this.session.renderContextHandleUser, this.handle);
                List<EventProperty> list = new List<EventProperty>();
                foreach (object value in properties) {
                    list.Add(new EventProperty(value));
                }
                return list;
            }
        }

        public IList<object> GetPropertyValues(EventLogPropertySelector propertySelector) {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");
            return NativeWrapper.EvtRenderBufferWithContextUserOrValues(propertySelector.Handle, this.handle);
        }

        // marked as SecurityCritical because it allocates SafeHandle
        // marked as TreatAsSafe because it performs Demand.
        [System.Security.SecuritySafeCritical]
        public override string ToXml() {
            StringBuilder renderBuffer = new StringBuilder(2000);
            NativeWrapper.EvtRender(EventLogHandle.Zero, this.handle, UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventXml, renderBuffer);
            return renderBuffer.ToString();
        }

        [System.Security.SecuritySafeCritical]
        protected override void Dispose(bool disposing) {
            try {

                if ( this.handle != null && !this.handle.IsInvalid )
                    this.handle.Dispose();
            }
            finally {
                base.Dispose(disposing);
            }
        }

        // marked as SecurityCritical because allocates SafeHandle.
        [System.Security.SecurityCritical]
        internal static EventLogHandle GetBookmarkHandleFromBookmark(EventBookmark bookmark) {
            if (bookmark == null)
                return EventLogHandle.Zero;
            EventLogHandle handle = NativeWrapper.EvtCreateBookmark(bookmark.BookmarkText);
            return handle;
        }
    }

}
