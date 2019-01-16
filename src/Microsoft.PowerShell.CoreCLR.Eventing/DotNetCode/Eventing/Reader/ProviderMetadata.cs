// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class exposes all the metadata for a specific
** Provider.  An instance of this class is obtained from
** EventLogManagement and is scoped to a single Locale.
**
============================================================*/

using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Exposes all the metadata for a specific event Provider.  An instance
    /// of this class is obtained from EventLogManagement and is scoped to a
    /// single Locale.
    /// </summary>
    public class ProviderMetadata : IDisposable
    {
        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle _handle = EventLogHandle.Zero;

        private EventLogHandle _defaultProviderHandle = EventLogHandle.Zero;

        private EventLogSession _session = null;

        private string _providerName;
        private CultureInfo _cultureInfo;
        private string _logFilePath;

        // caching of the IEnumerable<EventLevel>, <EventTask>, <EventKeyword>, <EventOpcode> on the ProviderMetadata
        // they do not change with every call.
        private IList<EventLevel> _levels = null;
        private IList<EventOpcode> _opcodes = null;
        private IList<EventTask> _tasks = null;
        private IList<EventKeyword> _keywords = null;
        private IList<EventLevel> _standardLevels = null;
        private IList<EventOpcode> _standardOpcodes = null;
        private IList<EventTask> _standardTasks = null;
        private IList<EventKeyword> _standardKeywords = null;
        private IList<EventLogLink> _channelReferences = null;

        private object _syncObject;

        public ProviderMetadata(string providerName)
            : this(providerName, null, null, null)
        {
        }

        public ProviderMetadata(string providerName, EventLogSession session, CultureInfo targetCultureInfo)
            : this(providerName, session, targetCultureInfo, null)
        {
        }

        // SecurityCritical since it allocates SafeHandles.
        // Marked TreatAsSafe since we perform the Demand check.
        [System.Security.SecuritySafeCritical]
        internal ProviderMetadata(string providerName, EventLogSession session, CultureInfo targetCultureInfo, string logFilePath)
        {
            if (targetCultureInfo == null)
                targetCultureInfo = CultureInfo.CurrentCulture;

            if (session == null)
                session = EventLogSession.GlobalSession;

            _session = session;
            _providerName = providerName;
            _cultureInfo = targetCultureInfo;
            _logFilePath = logFilePath;

            _handle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, _providerName, _logFilePath, 0, 0);

            _syncObject = new object();
        }

        internal EventLogHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public string Name
        {
            get { return _providerName; }
        }

        public Guid Id
        {
            get
            {
                return (Guid)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataPublisherGuid);
            }
        }

        public string MessageFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataMessageFilePath);
            }
        }

        public string ResourceFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataResourceFilePath);
            }
        }

        public string ParameterFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataParameterFilePath);
            }
        }

        public Uri HelpLink
        {
            get
            {
                string helpLinkStr = (string)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataHelpLink);
                if (helpLinkStr == null || helpLinkStr.Length == 0)
                    return null;
                return new Uri(helpLinkStr);
            }
        }

        private uint ProviderMessageID
        {
            get
            {
                return (uint)NativeWrapper.EvtGetPublisherMetadataProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataPublisherMessageID);
            }
        }

        public string DisplayName
        {
            [System.Security.SecurityCritical]
            get
            {
                uint msgId = (uint)this.ProviderMessageID;

                if (msgId == 0xffffffff)
                    return null;

                return NativeWrapper.EvtFormatMessage(_handle, msgId);
            }
        }

        public IList<EventLogLink> LogLinks
        {
            [System.Security.SecurityCritical]
            get
            {
                EventLogHandle elHandle = EventLogHandle.Zero;
                try
                {
                    lock (_syncObject)
                    {
                        if (_channelReferences != null)
                            return _channelReferences;

                        elHandle = NativeWrapper.EvtGetPublisherMetadataPropertyHandle(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataChannelReferences);

                        int arraySize = NativeWrapper.EvtGetObjectArraySize(elHandle);

                        List<EventLogLink> channelList = new List<EventLogLink>(arraySize);

                        for (int index = 0; index < arraySize; index++)
                        {
                            string channelName = (string)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataChannelReferencePath);

                            uint channelId = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataChannelReferenceID);

                            uint flag = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataChannelReferenceFlags);

                            bool isImported;
                            if (flag == (int)UnsafeNativeMethods.EvtChannelReferenceFlags.EvtChannelReferenceImported) isImported = true;
                            else isImported = false;

                            int channelRefMessageId = unchecked((int)((uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataChannelReferenceMessageID)));
                            string channelRefDisplayName;

                            // if channelRefMessageId == -1, we do not have anything in the message table.
                            if (channelRefMessageId == -1)
                            {
                                channelRefDisplayName = null;
                            }
                            else
                            {
                                channelRefDisplayName = NativeWrapper.EvtFormatMessage(_handle, unchecked((uint)channelRefMessageId));
                            }

                            if (channelRefDisplayName == null && isImported)
                            {
                                if (string.Compare(channelName, "Application", StringComparison.OrdinalIgnoreCase) == 0)
                                    channelRefMessageId = 256;
                                else if (string.Compare(channelName, "System", StringComparison.OrdinalIgnoreCase) == 0)
                                    channelRefMessageId = 258;
                                else if (string.Compare(channelName, "Security", StringComparison.OrdinalIgnoreCase) == 0)
                                    channelRefMessageId = 257;
                                else
                                    channelRefMessageId = -1;

                                if (channelRefMessageId != -1)
                                {
                                    if (_defaultProviderHandle.IsInvalid)
                                    {
                                        _defaultProviderHandle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, null, null, 0, 0);
                                    }

                                    channelRefDisplayName = NativeWrapper.EvtFormatMessage(_defaultProviderHandle, unchecked((uint)channelRefMessageId));
                                }
                            }

                            channelList.Add(new EventLogLink(channelName, isImported, channelRefDisplayName, channelId));
                        }

                        _channelReferences = channelList.AsReadOnly();
                    }

                    return _channelReferences;
                }
                finally
                {
                    elHandle.Dispose();
                }
            }
        }

        internal enum ObjectTypeName
        {
            Level = 0,
            Opcode = 1,
            Task = 2,
            Keyword = 3
        }

        internal string FindStandardLevelDisplayName(string name, uint value)
        {
            if (_standardLevels == null)
                _standardLevels = (List<EventLevel>)GetProviderListProperty(_defaultProviderHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevels);
            foreach (EventLevel standardLevel in _standardLevels)
            {
                if (standardLevel.Name == name && standardLevel.Value == value)
                    return standardLevel.DisplayName;
            }

            return null;
        }

        internal string FindStandardOpcodeDisplayName(string name, uint value)
        {
            if (_standardOpcodes == null)
                _standardOpcodes = (List<EventOpcode>)GetProviderListProperty(_defaultProviderHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodes);
            foreach (EventOpcode standardOpcode in _standardOpcodes)
            {
                if (standardOpcode.Name == name && standardOpcode.Value == value)
                    return standardOpcode.DisplayName;
            }

            return null;
        }

        internal string FindStandardKeywordDisplayName(string name, long value)
        {
            if (_standardKeywords == null)
                _standardKeywords = (List<EventKeyword>)GetProviderListProperty(_defaultProviderHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywords);
            foreach (EventKeyword standardKeyword in _standardKeywords)
            {
                if (standardKeyword.Name == name && standardKeyword.Value == value)
                    return standardKeyword.DisplayName;
            }

            return null;
        }

        internal string FindStandardTaskDisplayName(string name, uint value)
        {
            if (_standardTasks == null)
                _standardTasks = (List<EventTask>)GetProviderListProperty(_defaultProviderHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTasks);
            foreach (EventTask standardTask in _standardTasks)
            {
                if (standardTask.Name == name && standardTask.Value == value)
                    return standardTask.DisplayName;
            }

            return null;
        }

        [System.Security.SecuritySafeCritical]
        internal object GetProviderListProperty(EventLogHandle providerHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId metadataProperty)
        {
            EventLogHandle elHandle = EventLogHandle.Zero;

            try
            {
                UnsafeNativeMethods.EvtPublisherMetadataPropertyId propName;
                UnsafeNativeMethods.EvtPublisherMetadataPropertyId propValue;
                UnsafeNativeMethods.EvtPublisherMetadataPropertyId propMessageId;
                ObjectTypeName objectTypeName;

                List<EventLevel> levelList = null;
                List<EventOpcode> opcodeList = null;
                List<EventKeyword> keywordList = null;
                List<EventTask> taskList = null;

                elHandle = NativeWrapper.EvtGetPublisherMetadataPropertyHandle(providerHandle, metadataProperty);

                int arraySize = NativeWrapper.EvtGetObjectArraySize(elHandle);

                switch (metadataProperty)
                {
                    case UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevels:
                        propName = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevelName;
                        propValue = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevelValue;
                        propMessageId = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevelMessageID;
                        objectTypeName = ObjectTypeName.Level;
                        levelList = new List<EventLevel>(arraySize);
                        break;

                    case UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodes:
                        propName = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodeName;
                        propValue = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodeValue;
                        propMessageId = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodeMessageID;
                        objectTypeName = ObjectTypeName.Opcode;
                        opcodeList = new List<EventOpcode>(arraySize);
                        break;

                    case UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywords:
                        propName = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywordName;
                        propValue = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywordValue;
                        propMessageId = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywordMessageID;
                        objectTypeName = ObjectTypeName.Keyword;
                        keywordList = new List<EventKeyword>(arraySize);
                        break;

                    case UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTasks:
                        propName = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTaskName;
                        propValue = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTaskValue;
                        propMessageId = UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTaskMessageID;
                        objectTypeName = ObjectTypeName.Task;
                        taskList = new List<EventTask>(arraySize);
                        break;

                    default:
                        return null;
                }

                for (int index = 0; index < arraySize; index++)
                {
                    string generalName = (string)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propName);

                    uint generalValue = 0;
                    long generalValueKeyword = 0;
                    if (objectTypeName != ObjectTypeName.Keyword)
                    {
                        generalValue = (uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propValue);
                    }
                    else
                    {
                        generalValueKeyword = unchecked((long)((ulong)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propValue)));
                    }

                    int generalMessageId = unchecked((int)((uint)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)propMessageId)));

                    string generalDisplayName = null;

                    if (generalMessageId == -1)
                    {
                        if (providerHandle != _defaultProviderHandle)
                        {
                            if (_defaultProviderHandle.IsInvalid)
                            {
                                _defaultProviderHandle = NativeWrapper.EvtOpenProviderMetadata(_session.Handle, null, null, 0, 0);
                            }

                            switch (objectTypeName)
                            {
                                case ObjectTypeName.Level:
                                    generalDisplayName = FindStandardLevelDisplayName(generalName, generalValue);
                                    break;
                                case ObjectTypeName.Opcode:
                                    generalDisplayName = FindStandardOpcodeDisplayName(generalName, generalValue >> 16);
                                    break;
                                case ObjectTypeName.Keyword:
                                    generalDisplayName = FindStandardKeywordDisplayName(generalName, generalValueKeyword);
                                    break;
                                case ObjectTypeName.Task:
                                    generalDisplayName = FindStandardTaskDisplayName(generalName, generalValue);
                                    break;
                                default:
                                    generalDisplayName = null;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        generalDisplayName = NativeWrapper.EvtFormatMessage(providerHandle, unchecked((uint)generalMessageId));
                    }

                    switch (objectTypeName)
                    {
                        case ObjectTypeName.Level:
                            levelList.Add(new EventLevel(generalName, (int)generalValue, generalDisplayName));
                            break;
                        case ObjectTypeName.Opcode:
                            opcodeList.Add(new EventOpcode(generalName, (int)(generalValue >> 16), generalDisplayName));
                            break;
                        case ObjectTypeName.Keyword:
                            keywordList.Add(new EventKeyword(generalName, (long)generalValueKeyword, generalDisplayName));
                            break;
                        case ObjectTypeName.Task:
                            Guid taskGuid = (Guid)NativeWrapper.EvtGetObjectArrayProperty(elHandle, index, (int)UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTaskEventGuid);
                            taskList.Add(new EventTask(generalName, (int)generalValue, generalDisplayName, taskGuid));
                            break;
                        default:
                            return null;
                    }
                }

                switch (objectTypeName)
                {
                    case ObjectTypeName.Level:
                        return levelList;
                    case ObjectTypeName.Opcode:
                        return opcodeList;
                    case ObjectTypeName.Keyword:
                        return keywordList;
                    case ObjectTypeName.Task:
                        return taskList;
                }

                return null;
            }
            finally
            {
                elHandle.Dispose();
            }
        }

        public IList<EventLevel> Levels
        {
            get
            {
                List<EventLevel> el;
                lock (_syncObject)
                {
                    if (_levels != null)
                        return _levels;

                    el = (List<EventLevel>)this.GetProviderListProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataLevels);
                    _levels = el.AsReadOnly();
                }

                return _levels;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcodes", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public IList<EventOpcode> Opcodes
        {
            get
            {
                List<EventOpcode> eo;
                lock (_syncObject)
                {
                    if (_opcodes != null)
                        return _opcodes;

                    eo = (List<EventOpcode>)this.GetProviderListProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataOpcodes);
                    _opcodes = eo.AsReadOnly();
                }

                return _opcodes;
            }
        }

        public IList<EventKeyword> Keywords
        {
            get
            {
                List<EventKeyword> ek;
                lock (_syncObject)
                {
                    if (_keywords != null)
                        return _keywords;

                    ek = (List<EventKeyword>)this.GetProviderListProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataKeywords);
                    _keywords = ek.AsReadOnly();
                }

                return _keywords;
            }
        }

        public IList<EventTask> Tasks
        {
            get
            {
                List<EventTask> et;
                lock (_syncObject)
                {
                    if (_tasks != null)
                        return _tasks;

                    et = (List<EventTask>)this.GetProviderListProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTasks);
                    _tasks = et.AsReadOnly();
                }

                return _tasks;
            }
        }

        public IEnumerable<EventMetadata> Events
        {
            [System.Security.SecurityCritical]
            get
            {
                List<EventMetadata> emList = new List<EventMetadata>();

                EventLogHandle emEnumHandle = NativeWrapper.EvtOpenEventMetadataEnum(_handle, 0);

                using (emEnumHandle)
                {
                    while (true)
                    {
                        EventLogHandle emHandle = emHandle = NativeWrapper.EvtNextEventMetadata(emEnumHandle, 0);
                        if (emHandle == null)
                            break;

                        using (emHandle)
                        {
                            unchecked
                            {
                                uint emId = (uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventID);
                                byte emVersion = (byte)((uint)(NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventVersion)));
                                byte emChannelId = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventChannel));
                                byte emLevel = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventLevel));
                                byte emOpcode = (byte)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventOpcode));
                                short emTask = (short)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventTask));
                                long emKeywords = (long)(ulong)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventKeyword);
                                string emTemplate = (string)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventTemplate);
                                int messageId = (int)((uint)NativeWrapper.EvtGetEventMetadataProperty(emHandle, UnsafeNativeMethods.EvtEventMetadataPropertyId.EventMetadataEventMessageID));

                                string emMessage = (messageId == -1)
                                    ? null
                                    : NativeWrapper.EvtFormatMessage(_handle, (uint)messageId);

                                EventMetadata em = new EventMetadata(emId, emVersion, emChannelId, emLevel, emOpcode, emTask, emKeywords, emTemplate, emMessage, this);
                                emList.Add(em);
                            }
                        }
                    }

                    return emList.AsReadOnly();
                }
            }
        }

        // throws if Provider metadata has been uninstalled since this object was created.

        internal void CheckReleased()
        {
            lock (_syncObject)
            {
                this.GetProviderListProperty(_handle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId.EvtPublisherMetadataTasks);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (_handle != null && !_handle.IsInvalid)
                _handle.Dispose();
        }
    }
}
