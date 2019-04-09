// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class is an EventLog implementation of EventRecord.  An
** instance of this is obtained from an EventLogReader.
**
============================================================*/

using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics.Eventing.Reader
{
    public class EventLogRecord : EventRecord
    {
        private const int SYSTEM_PROPERTY_COUNT = 18;

        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        [System.Security.SecuritySafeCritical]
        private EventLogHandle _handle;

        private EventLogSession _session;

        private NativeWrapper.SystemProperties _systemProperties;
        private string _containerChannel;
        private int[] _matchedQueryIds;

        // a dummy object which is used only for the locking.
        private object _syncObject;

        // cached DisplayNames for each instance
        private string _levelName = null;
        private string _taskName = null;
        private string _opcodeName = null;
        private IEnumerable<string> _keywordsNames = null;

        // cached DisplayNames for each instance
        private bool _levelNameReady;
        private bool _taskNameReady;
        private bool _opcodeNameReady;

        private ProviderMetadataCachedInformation _cachedMetadataInformation;

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        internal EventLogRecord(EventLogHandle handle, EventLogSession session, ProviderMetadataCachedInformation cachedMetadataInfo)
        {
            _cachedMetadataInformation = cachedMetadataInfo;
            _handle = handle;
            _session = session;
            _systemProperties = new NativeWrapper.SystemProperties();
            _syncObject = new object();
        }

        internal EventLogHandle Handle
        {
            // just returning reference to security critical type, the methods
            // of that type are protected by SecurityCritical as appropriate.
            [System.Security.SecuritySafeCritical]
            get
            {
                return _handle;
            }
        }

        internal void PrepareSystemData()
        {
            if (_systemProperties.filled)
                return;

            // prepare the System Context, if it is not already initialized.
            _session.SetupSystemContext();

            lock (_syncObject)
            {
                if (_systemProperties.filled == false)
                {
                    NativeWrapper.EvtRenderBufferWithContextSystem(_session.renderContextHandleSystem, _handle, UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventValues, _systemProperties, SYSTEM_PROPERTY_COUNT);
                    _systemProperties.filled = true;
                }
            }
        }

        public override int Id
        {
            get
            {
                PrepareSystemData();
                if (_systemProperties.Id == null)
                    return 0;
                return (int)_systemProperties.Id;
            }
        }

        public override byte? Version
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.Version;
            }
        }

        public override int? Qualifiers
        {
            get
            {
                PrepareSystemData();
                return (int?)(uint?)_systemProperties.Qualifiers;
            }
        }

        public override byte? Level
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.Level;
            }
        }

        public override int? Task
        {
            get
            {
                PrepareSystemData();
                return (int?)(uint?)_systemProperties.Task;
            }
        }

        public override short? Opcode
        {
            get
            {
                PrepareSystemData();
                return (short?)(ushort?)_systemProperties.Opcode;
            }
        }

        public override long? Keywords
        {
            get
            {
                PrepareSystemData();
                return (long?)_systemProperties.Keywords;
            }
        }

        public override long? RecordId
        {
            get
            {
                PrepareSystemData();
                return (long?)_systemProperties.RecordId;
            }
        }

        public override string ProviderName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ProviderName;
            }
        }

        public override Guid? ProviderId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ProviderId;
            }
        }

        public override string LogName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ChannelName;
            }
        }

        public override int? ProcessId
        {
            get
            {
                PrepareSystemData();
                return (int?)_systemProperties.ProcessId;
            }
        }

        public override int? ThreadId
        {
            get
            {
                PrepareSystemData();
                return (int?)_systemProperties.ThreadId;
            }
        }

        public override string MachineName
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ComputerName;
            }
        }

        public override System.Security.Principal.SecurityIdentifier UserId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.UserId;
            }
        }

        public override DateTime? TimeCreated
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.TimeCreated;
            }
        }

        public override Guid? ActivityId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.ActivityId;
            }
        }

        public override Guid? RelatedActivityId
        {
            get
            {
                PrepareSystemData();
                return _systemProperties.RelatedActivityId;
            }
        }

        public string ContainerLog
        {
            get
            {
                if (_containerChannel != null)
                    return _containerChannel;
                lock (_syncObject)
                {
                    if (_containerChannel == null)
                    {
                        _containerChannel = (string)NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventPath);
                    }

                    return _containerChannel;
                }
            }
        }

        public IEnumerable<int> MatchedQueryIds
        {
            get
            {
                if (_matchedQueryIds != null)
                    return _matchedQueryIds;
                lock (_syncObject)
                {
                    if (_matchedQueryIds == null)
                    {
                        _matchedQueryIds = (int[])NativeWrapper.EvtGetEventInfo(this.Handle, UnsafeNativeMethods.EvtEventPropertyId.EvtEventQueryIDs);
                    }

                    return _matchedQueryIds;
                }
            }
        }

        public override EventBookmark Bookmark
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                EventLogHandle bookmarkHandle = NativeWrapper.EvtCreateBookmark(null);
                NativeWrapper.EvtUpdateBookmark(bookmarkHandle, _handle);
                string bookmarkText = NativeWrapper.EvtRenderBookmark(bookmarkHandle);

                return new EventBookmark(bookmarkText);
            }
        }

        public override string FormatDescription()
        {
            return _cachedMetadataInformation.GetFormatDescription(this.ProviderName, _handle);
        }

        public override string FormatDescription(IEnumerable<object> values)
        {
            if (values == null) return this.FormatDescription();

            // copy the value IEnumerable to an array.
            string[] theValues = Array.Empty<string>();
            int i = 0;
            foreach (object o in values)
            {
                if (theValues.Length == i)
                    Array.Resize(ref theValues, i + 1);
                theValues[i] = o.ToString();
                i++;
            }

            return _cachedMetadataInformation.GetFormatDescription(this.ProviderName, _handle, theValues);
        }

        public override string LevelDisplayName
        {
            get
            {
                if (_levelNameReady)
                    return _levelName;
                lock (_syncObject)
                {
                    if (_levelNameReady == false)
                    {
                        _levelNameReady = true;
                        _levelName = _cachedMetadataInformation.GetLevelDisplayName(this.ProviderName, _handle);
                    }

                    return _levelName;
                }
            }
        }

        public override string OpcodeDisplayName
        {
            get
            {
                lock (_syncObject)
                {
                    if (_opcodeNameReady == false)
                    {
                        _opcodeNameReady = true;
                        _opcodeName = _cachedMetadataInformation.GetOpcodeDisplayName(this.ProviderName, _handle);
                    }

                    return _opcodeName;
                }
            }
        }

        public override string TaskDisplayName
        {
            get
            {
                if (_taskNameReady == true)
                    return _taskName;
                lock (_syncObject)
                {
                    if (_taskNameReady == false)
                    {
                        _taskNameReady = true;
                        _taskName = _cachedMetadataInformation.GetTaskDisplayName(this.ProviderName, _handle);
                    }

                    return _taskName;
                }
            }
        }

        public override IEnumerable<string> KeywordsDisplayNames
        {
            get
            {
                if (_keywordsNames != null)
                    return _keywordsNames;
                lock (_syncObject)
                {
                    if (_keywordsNames == null)
                    {
                        _keywordsNames = _cachedMetadataInformation.GetKeywordDisplayNames(this.ProviderName, _handle);
                    }

                    return _keywordsNames;
                }
            }
        }

        public override IList<EventProperty> Properties
        {
            get
            {
                _session.SetupUserContext();
                IList<object> properties = NativeWrapper.EvtRenderBufferWithContextUserOrValues(_session.renderContextHandleUser, _handle);
                List<EventProperty> list = new List<EventProperty>();
                foreach (object value in properties)
                {
                    list.Add(new EventProperty(value));
                }

                return list;
            }
        }

        public IList<object> GetPropertyValues(EventLogPropertySelector propertySelector)
        {
            if (propertySelector == null)
                throw new ArgumentNullException("propertySelector");
            return NativeWrapper.EvtRenderBufferWithContextUserOrValues(propertySelector.Handle, _handle);
        }

        // marked as SecurityCritical because it allocates SafeHandle
        // marked as TreatAsSafe because it performs Demand.
        [System.Security.SecuritySafeCritical]
        public override string ToXml()
        {
            StringBuilder renderBuffer = new StringBuilder(2000);
            NativeWrapper.EvtRender(EventLogHandle.Zero, _handle, UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventXml, renderBuffer);
            return renderBuffer.ToString();
        }

        [System.Security.SecuritySafeCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_handle != null && !_handle.IsInvalid)
                    _handle.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // marked as SecurityCritical because allocates SafeHandle.
        [System.Security.SecurityCritical]
        internal static EventLogHandle GetBookmarkHandleFromBookmark(EventBookmark bookmark)
        {
            if (bookmark == null)
                return EventLogHandle.Zero;
            EventLogHandle handle = NativeWrapper.EvtCreateBookmark(bookmark.BookmarkText);
            return handle;
        }
    }
}
