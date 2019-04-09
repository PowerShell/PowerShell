// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class allows accessing static channel information and
** configures channel publishing and logging properties.  An instance
** of this class is obtained from EventLogManagement class.
**
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Log Type.
    /// </summary>
    public enum EventLogType
    {
        Administrative = 0,
        Operational,
        Analytical,
        Debug
    }

    /// <summary>
    /// Log Isolation.
    /// </summary>
    public enum EventLogIsolation
    {
        Application = 0,
        System,
        Custom
    }

    /// <summary>
    /// Log Mode.
    /// </summary>
    public enum EventLogMode
    {
        Circular = 0,
        AutoBackup,
        Retain
    }

    /// <summary>
    /// Provides access to static log information and configures
    /// log publishing and log file properties.
    /// </summary>
    public class EventLogConfiguration : IDisposable
    {
        //
        // access to the data member reference is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        private EventLogHandle _handle = EventLogHandle.Zero;

        private EventLogSession _session = null;
        private string _channelName;

        public EventLogConfiguration(string logName) : this(logName, null) { }

        // marked as SecurityCritical because allocates SafeHandles.
        // marked as Safe because performs Demand check.
        [System.Security.SecurityCritical]
        public EventLogConfiguration(string logName, EventLogSession session)
        {
            if (session == null)
                session = EventLogSession.GlobalSession;

            _session = session;
            _channelName = logName;

            _handle = NativeWrapper.EvtOpenChannelConfig(_session.Handle, _channelName, 0);
        }

        public string LogName
        {
            get
            {
                return _channelName;
            }
        }

        public EventLogType LogType
        {
            get
            {
                return (EventLogType)((uint)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigType));
            }
        }

        public EventLogIsolation LogIsolation
        {
            get
            {
                return (EventLogIsolation)((uint)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigIsolation));
            }
        }

        public bool IsEnabled
        {
            get
            {
                return (bool)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigEnabled);
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigEnabled, (object)value);
            }
        }

        public bool IsClassicLog
        {
            get
            {
                return (bool)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigClassicEventlog);
            }
        }

        public string SecurityDescriptor
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigAccess);
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigAccess, (object)value);
            }
        }

        public string LogFilePath
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigLogFilePath);
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigLogFilePath, (object)value);
            }
        }

        public long MaximumSizeInBytes
        {
            get
            {
                return (long)((ulong)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigMaxSize));
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigMaxSize, (object)value);
            }
        }

        public EventLogMode LogMode
        {
            get
            {
                object nativeRetentionObject = NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention);
                object nativeAutoBackupObject = NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup);

                bool nativeRetention = nativeRetentionObject == null ? false : (bool)nativeRetentionObject;
                bool nativeAutoBackup = nativeAutoBackupObject == null ? false : (bool)nativeAutoBackupObject;

                if (nativeAutoBackup)
                    return EventLogMode.AutoBackup;

                if (nativeRetention)
                    return EventLogMode.Retain;

                return EventLogMode.Circular;
            }

            set
            {
                switch (value)
                {
                    case EventLogMode.Circular:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)false);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)false);
                        break;
                    case EventLogMode.AutoBackup:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)true);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)true);
                        break;
                    case EventLogMode.Retain:
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup, (object)false);
                        NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention, (object)true);
                        break;
                }
            }
        }

        public string OwningProviderName
        {
            get
            {
                return (string)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigOwningPublisher);
            }
        }

        public IEnumerable<string> ProviderNames
        {
            get
            {
                return (string[])NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublisherList);
            }
        }

        public int? ProviderLevel
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLevel));
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLevel, (object)value);
            }
        }

        public long? ProviderKeywords
        {
            get
            {
                return (long?)((ulong?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigKeywords));
            }

            set
            {
                NativeWrapper.EvtSetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigKeywords, (object)value);
            }
        }

        public int? ProviderBufferSize
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigBufferSize));
            }
        }

        public int? ProviderMinimumNumberOfBuffers
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigMinBuffers));
            }
        }

        public int? ProviderMaximumNumberOfBuffers
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigMaxBuffers));
            }
        }

        public int? ProviderLatency
        {
            get
            {
                return (int?)((uint?)NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLatency));
            }
        }

        public Guid? ProviderControlGuid
        {
            get
            {
                return (Guid?)(NativeWrapper.EvtGetChannelConfigProperty(_handle, UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigControlGuid));
            }
        }

        public void SaveChanges()
        {
            NativeWrapper.EvtSaveChannelConfig(_handle, 0);
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
