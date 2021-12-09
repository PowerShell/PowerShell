// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Diagnostics.Eventing
{
    internal static class UnsafeNativeMethods
    {
        private const string FormatMessageDllName = "api-ms-win-core-localization-l1-2-0.dll";
        private const string EventProviderDllName = "api-ms-win-eventing-provider-l1-1-0.dll";
        private const string WEVTAPI = "wevtapi.dll";

        private static readonly IntPtr s_NULL = IntPtr.Zero;

        // WinError.h codes:
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6;

        // Can occurs when filled buffers are trying to flush to disk, but disk IOs are not fast enough.
        // This happens when the disk is slow and event traffic is heavy.
        // Eventually, there are no more free (empty) buffers and the event is dropped.
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;

        internal const int ERROR_INVALID_DRIVE = 0xF;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
        internal const int ERROR_LOCK_VIOLATION = 0x21;  // 33
        internal const int ERROR_HANDLE_EOF = 0x26;  // 38
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;  // 87
        internal const int ERROR_BROKEN_PIPE = 0x6D;  // 109
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;  // 122
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long
        internal const int ERROR_PIPE_BUSY = 0xE7;  // 231
        internal const int ERROR_NO_DATA = 0xE8;  // 232
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;  // 233
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_NO_MORE_ITEMS = 0x103;  // 259
        internal const int ERROR_PIPE_CONNECTED = 0x217;  // 535
        internal const int ERROR_PIPE_LISTENING = 0x218;  // 536
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_IO_PENDING = 0x3E5;  // 997
        internal const int ERROR_NOT_FOUND = 0x490;  // 1168

        // The event size is larger than the allowed maximum (64k - header).
        internal const int ERROR_ARITHMETIC_OVERFLOW = 0x216;  // 534
        internal const int ERROR_RESOURCE_LANG_NOT_FOUND = 0x717;  // 1815

        // Event log specific codes:
        internal const int ERROR_EVT_MESSAGE_NOT_FOUND = 15027;
        internal const int ERROR_EVT_MESSAGE_ID_NOT_FOUND = 15028;
        internal const int ERROR_EVT_UNRESOLVED_VALUE_INSERT = 15029;
        internal const int ERROR_EVT_UNRESOLVED_PARAMETER_INSERT = 15030;
        internal const int ERROR_EVT_MAX_INSERTS_REACHED = 15031;
        internal const int ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND = 15033;
        internal const int ERROR_MUI_FILE_NOT_FOUND = 15100;

        // ErrorCode & format
        // for win32 error message formatting
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        [DllImport(FormatMessageDllName, CharSet = CharSet.Unicode, BestFitMapping = false)]
        [SecurityCritical]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
            int dwMessageId, int dwLanguageId, StringBuilder lpBuffer,
            int nSize, IntPtr va_list_arguments);

        // Gets an error message for a Win32 error code.
        [SecurityCritical]
        internal static string GetMessage(int errorCode)
        {
            StringBuilder sb = new(512);
            int result = UnsafeNativeMethods.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                UnsafeNativeMethods.s_NULL, errorCode, 0, sb, sb.Capacity, UnsafeNativeMethods.s_NULL);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder on NT,
                // but on Win9x, it appears to be the number of MBCS buffer.
                // Just give up and return the String as-is...
                string s = sb.ToString();
                return s;
            }
            else
            {
                return "UnknownError_Num " + errorCode;
            }
        }

        // ETW Methods
        // Callback
        [SecurityCritical]
        internal unsafe delegate void EtwEnableCallback(
            [In] ref Guid sourceId,
            [In] int isEnabled,
            [In] byte level,
            [In] long matchAnyKeywords,
            [In] long matchAllKeywords,
            [In] void* filterData,
            [In] void* callbackContext
            );

        // Registration APIs
        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventRegister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventRegister(
                    [In] in Guid providerId,
                    [In] EtwEnableCallback enableCallback,
                    [In] void* callbackContext,
                    [In][Out] ref long registrationHandle
                    );

        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventUnregister", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern int EventUnregister([In] long registrationHandle);

        // Control (Is Enabled) APIs
        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventEnabled", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern int EventEnabled([In] long registrationHandle, [In] in System.Diagnostics.Eventing.EventDescriptor eventDescriptor);

        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventProviderEnabled", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern int EventProviderEnabled([In] long registrationHandle, [In] byte level, [In] long keywords);

        // Writing (Publishing/Logging) APIs
        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventWrite", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventWrite(
                [In] long registrationHandle,
                [In] in EventDescriptor eventDescriptor,
                [In] uint userDataCount,
                [In] void* userData
                );

        // Writing (Publishing/Logging) APIs
        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventWrite", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventWrite(
                [In] long registrationHandle,
                [In] EventDescriptor* eventDescriptor,
                [In] uint userDataCount,
                [In] void* userData
                );

        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventWriteTransfer", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventWriteTransfer(
                [In] long registrationHandle,
                [In] in EventDescriptor eventDescriptor,
                [In] Guid* activityId,
                [In] Guid* relatedActivityId,
                [In] uint userDataCount,
                [In] void* userData
                );

        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventWriteString", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventWriteString(
                [In] long registrationHandle,
                [In] byte level,
                [In] long keywords,
                [In] char* message
                );

        // ActivityId Control APIs
        [DllImport(EventProviderDllName, ExactSpelling = true, EntryPoint = "EventActivityIdControl", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        [SecurityCritical]
        internal static extern unsafe uint EventActivityIdControl([In] int ControlCode, [In][Out] ref Guid ActivityId);

        // EventLog
        [Flags]
        internal enum EvtQueryFlags
        {
            EvtQueryChannelPath = 0x1,
            EvtQueryFilePath = 0x2,
            EvtQueryForwardDirection = 0x100,
            EvtQueryReverseDirection = 0x200,
            EvtQueryTolerateQueryErrors = 0x1000
        }

        /// <summary>
        /// Evt Variant types.
        /// </summary>
        internal enum EvtVariantType
        {
            EvtVarTypeNull = 0,
            EvtVarTypeString = 1,
            EvtVarTypeAnsiString = 2,
            EvtVarTypeSByte = 3,
            EvtVarTypeByte = 4,
            EvtVarTypeInt16 = 5,
            EvtVarTypeUInt16 = 6,
            EvtVarTypeInt32 = 7,
            EvtVarTypeUInt32 = 8,
            EvtVarTypeInt64 = 9,
            EvtVarTypeUInt64 = 10,
            EvtVarTypeSingle = 11,
            EvtVarTypeDouble = 12,
            EvtVarTypeBoolean = 13,
            EvtVarTypeBinary = 14,
            EvtVarTypeGuid = 15,
            EvtVarTypeSizeT = 16,
            EvtVarTypeFileTime = 17,
            EvtVarTypeSysTime = 18,
            EvtVarTypeSid = 19,
            EvtVarTypeHexInt32 = 20,
            EvtVarTypeHexInt64 = 21,
            // these types used internally
            EvtVarTypeEvtHandle = 32,
            EvtVarTypeEvtXml = 35,
            // Array = 128
            EvtVarTypeStringArray = 129,
            EvtVarTypeUInt32Array = 136
        }

        internal enum EvtMasks
        {
            EVT_VARIANT_TYPE_MASK = 0x7f,
            EVT_VARIANT_TYPE_ARRAY = 128
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            public short Year;

            [MarshalAs(UnmanagedType.U2)]
            public short Month;

            [MarshalAs(UnmanagedType.U2)]
            public short DayOfWeek;

            [MarshalAs(UnmanagedType.U2)]
            public short Day;

            [MarshalAs(UnmanagedType.U2)]
            public short Hour;

            [MarshalAs(UnmanagedType.U2)]
            public short Minute;

            [MarshalAs(UnmanagedType.U2)]
            public short Second;

            [MarshalAs(UnmanagedType.U2)]
            public short Milliseconds;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        internal struct EvtVariant
        {
            [FieldOffset(0)]
            public uint UInteger;

            [FieldOffset(0)]
            public int Integer;

            [FieldOffset(0)]
            public byte UInt8;

            [FieldOffset(0)]
            public short Short;

            [FieldOffset(0)]
            public ushort UShort;

            [FieldOffset(0)]
            public uint Bool;

            [FieldOffset(0)]
            public byte ByteVal;

            [FieldOffset(0)]
            public byte SByte;

            [FieldOffset(0)]
            public ulong ULong;

            [FieldOffset(0)]
            public long Long;

            [FieldOffset(0)]
            public float Single;

            [FieldOffset(0)]
            public double Double;

            [FieldOffset(0)]
            public IntPtr StringVal;

            [FieldOffset(0)]
            public IntPtr AnsiString;

            [FieldOffset(0)]
            public IntPtr SidVal;

            [FieldOffset(0)]
            public IntPtr Binary;

            [FieldOffset(0)]
            public IntPtr Reference;

            [FieldOffset(0)]
            public IntPtr Handle;

            [FieldOffset(0)]
            public IntPtr GuidReference;

            [FieldOffset(0)]
            public ulong FileTime;

            [FieldOffset(0)]
            public IntPtr SystemTime;

            [FieldOffset(0)]
            public IntPtr SizeT;

            [FieldOffset(8)]
            public uint Count;   // number of elements (not length) in bytes.

            [FieldOffset(12)]
            public uint Type;
        }

        internal enum EvtEventPropertyId
        {
            EvtEventQueryIDs = 0,
            EvtEventPath = 1
        }

        /// <summary>
        /// The query flags to get information about query.
        /// </summary>
        internal enum EvtQueryPropertyId
        {
            EvtQueryNames = 0,   // String;   // Variant will be array of EvtVarTypeString
            EvtQueryStatuses = 1 // UInt32;   // Variant will be Array of EvtVarTypeUInt32
        }

        /// <summary>
        /// Publisher Metadata properties.
        /// </summary>
        internal enum EvtPublisherMetadataPropertyId
        {
            EvtPublisherMetadataPublisherGuid = 0,      // EvtVarTypeGuid
            EvtPublisherMetadataResourceFilePath = 1,       // EvtVarTypeString
            EvtPublisherMetadataParameterFilePath = 2,      // EvtVarTypeString
            EvtPublisherMetadataMessageFilePath = 3,        // EvtVarTypeString
            EvtPublisherMetadataHelpLink = 4,               // EvtVarTypeString
            EvtPublisherMetadataPublisherMessageID = 5,     // EvtVarTypeUInt32

            EvtPublisherMetadataChannelReferences = 6,      // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataChannelReferencePath = 7,   // EvtVarTypeString
            EvtPublisherMetadataChannelReferenceIndex = 8,  // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceID = 9,     // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceFlags = 10,  // EvtVarTypeUInt32
            EvtPublisherMetadataChannelReferenceMessageID = 11, // EvtVarTypeUInt32

            EvtPublisherMetadataLevels = 12,                 // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataLevelName = 13,              // EvtVarTypeString
            EvtPublisherMetadataLevelValue = 14,             // EvtVarTypeUInt32
            EvtPublisherMetadataLevelMessageID = 15,         // EvtVarTypeUInt32

            EvtPublisherMetadataTasks = 16,                  // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataTaskName = 17,               // EvtVarTypeString
            EvtPublisherMetadataTaskEventGuid = 18,          // EvtVarTypeGuid
            EvtPublisherMetadataTaskValue = 19,              // EvtVarTypeUInt32
            EvtPublisherMetadataTaskMessageID = 20,          // EvtVarTypeUInt32

            EvtPublisherMetadataOpcodes = 21,                // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataOpcodeName = 22,             // EvtVarTypeString
            EvtPublisherMetadataOpcodeValue = 23,            // EvtVarTypeUInt32
            EvtPublisherMetadataOpcodeMessageID = 24,        // EvtVarTypeUInt32

            EvtPublisherMetadataKeywords = 25,               // EvtVarTypeEvtHandle, ObjectArray
            EvtPublisherMetadataKeywordName = 26,            // EvtVarTypeString
            EvtPublisherMetadataKeywordValue = 27,           // EvtVarTypeUInt64
            EvtPublisherMetadataKeywordMessageID = 28        // EvtVarTypeUInt32
            // EvtPublisherMetadataPropertyIdEND
        }

        internal enum EvtChannelReferenceFlags
        {
            EvtChannelReferenceImported = 1
        }

        internal enum EvtEventMetadataPropertyId
        {
            EventMetadataEventID,        // EvtVarTypeUInt32
            EventMetadataEventVersion,   // EvtVarTypeUInt32
            EventMetadataEventChannel,   // EvtVarTypeUInt32
            EventMetadataEventLevel,     // EvtVarTypeUInt32
            EventMetadataEventOpcode,    // EvtVarTypeUInt32
            EventMetadataEventTask,      // EvtVarTypeUInt32
            EventMetadataEventKeyword,   // EvtVarTypeUInt64
            EventMetadataEventMessageID, // EvtVarTypeUInt32
            EventMetadataEventTemplate   // EvtVarTypeString
            // EvtEventMetadataPropertyIdEND
        }

        // CHANNEL CONFIGURATION
        internal enum EvtChannelConfigPropertyId
        {
            EvtChannelConfigEnabled = 0,            // EvtVarTypeBoolean
            EvtChannelConfigIsolation,              // EvtVarTypeUInt32, EVT_CHANNEL_ISOLATION_TYPE
            EvtChannelConfigType,                   // EvtVarTypeUInt32, EVT_CHANNEL_TYPE
            EvtChannelConfigOwningPublisher,        // EvtVarTypeString
            EvtChannelConfigClassicEventlog,        // EvtVarTypeBoolean
            EvtChannelConfigAccess,                 // EvtVarTypeString
            EvtChannelLoggingConfigRetention,       // EvtVarTypeBoolean
            EvtChannelLoggingConfigAutoBackup,      // EvtVarTypeBoolean
            EvtChannelLoggingConfigMaxSize,         // EvtVarTypeUInt64
            EvtChannelLoggingConfigLogFilePath,     // EvtVarTypeString
            EvtChannelPublishingConfigLevel,        // EvtVarTypeUInt32
            EvtChannelPublishingConfigKeywords,     // EvtVarTypeUInt64
            EvtChannelPublishingConfigControlGuid,  // EvtVarTypeGuid
            EvtChannelPublishingConfigBufferSize,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigMinBuffers,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigMaxBuffers,   // EvtVarTypeUInt32
            EvtChannelPublishingConfigLatency,      // EvtVarTypeUInt32
            EvtChannelPublishingConfigClockType,    // EvtVarTypeUInt32, EVT_CHANNEL_CLOCK_TYPE
            EvtChannelPublishingConfigSidType,      // EvtVarTypeUInt32, EVT_CHANNEL_SID_TYPE
            EvtChannelPublisherList,                // EvtVarTypeString | EVT_VARIANT_TYPE_ARRAY
            EvtChannelConfigPropertyIdEND
        }

        // LOG INFORMATION
        internal enum EvtLogPropertyId
        {
            EvtLogCreationTime = 0,             // EvtVarTypeFileTime
            EvtLogLastAccessTime,               // EvtVarTypeFileTime
            EvtLogLastWriteTime,                // EvtVarTypeFileTime
            EvtLogFileSize,                     // EvtVarTypeUInt64
            EvtLogAttributes,                   // EvtVarTypeUInt32
            EvtLogNumberOfLogRecords,           // EvtVarTypeUInt64
            EvtLogOldestRecordNumber,           // EvtVarTypeUInt64
            EvtLogFull,                         // EvtVarTypeBoolean
        }

        internal enum EvtExportLogFlags
        {
            EvtExportLogChannelPath = 1,
            EvtExportLogFilePath = 2,
            EvtExportLogTolerateQueryErrors = 0x1000
        }

        // RENDERING
        internal enum EvtRenderContextFlags
        {
            EvtRenderContextValues = 0,      // Render specific properties
            EvtRenderContextSystem = 1,      // Render all system properties (System)
            EvtRenderContextUser = 2         // Render all user properties (User/EventData)
        }

        internal enum EvtRenderFlags
        {
            EvtRenderEventValues = 0,       // Variants
            EvtRenderEventXml = 1,          // XML
            EvtRenderBookmark = 2           // Bookmark
        }

        internal enum EvtFormatMessageFlags
        {
            EvtFormatMessageEvent = 1,
            EvtFormatMessageLevel = 2,
            EvtFormatMessageTask = 3,
            EvtFormatMessageOpcode = 4,
            EvtFormatMessageKeyword = 5,
            EvtFormatMessageChannel = 6,
            EvtFormatMessageProvider = 7,
            EvtFormatMessageId = 8,
            EvtFormatMessageXml = 9
        }

        internal enum EvtSystemPropertyId
        {
            EvtSystemProviderName = 0,          // EvtVarTypeString
            EvtSystemProviderGuid,              // EvtVarTypeGuid
            EvtSystemEventID,                   // EvtVarTypeUInt16
            EvtSystemQualifiers,                // EvtVarTypeUInt16
            EvtSystemLevel,                     // EvtVarTypeUInt8
            EvtSystemTask,                      // EvtVarTypeUInt16
            EvtSystemOpcode,                    // EvtVarTypeUInt8
            EvtSystemKeywords,                  // EvtVarTypeHexInt64
            EvtSystemTimeCreated,               // EvtVarTypeFileTime
            EvtSystemEventRecordId,             // EvtVarTypeUInt64
            EvtSystemActivityID,                // EvtVarTypeGuid
            EvtSystemRelatedActivityID,         // EvtVarTypeGuid
            EvtSystemProcessID,                 // EvtVarTypeUInt32
            EvtSystemThreadID,                  // EvtVarTypeUInt32
            EvtSystemChannel,                   // EvtVarTypeString
            EvtSystemComputer,                  // EvtVarTypeString
            EvtSystemUserID,                    // EvtVarTypeSid
            EvtSystemVersion,                   // EvtVarTypeUInt8
            EvtSystemPropertyIdEND
        }

        // SESSION
        internal enum EvtLoginClass
        {
            EvtRpcLogin = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct EvtRpcLogin
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Server;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string User;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Domain;

            [SecurityCritical]
            public CoTaskMemUnicodeSafeHandle Password;

            public int Flags;
        }

        // SEEK
        [Flags]
        internal enum EvtSeekFlags
        {
            EvtSeekRelativeToFirst = 1,
            EvtSeekRelativeToLast = 2,
            EvtSeekRelativeToCurrent = 3,
            EvtSeekRelativeToBookmark = 4,
            EvtSeekOriginMask = 7,
            EvtSeekStrict = 0x10000
        }

        [DllImport(WEVTAPI, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtQuery(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            int flags);

        // SEEK
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtSeek(
                            EventLogHandle resultSet,
                            long position,
                            EventLogHandle bookmark,
                            int timeout,
                            [MarshalAs(UnmanagedType.I4)] EvtSeekFlags flags
                                        );

        [DllImport(WEVTAPI, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtNext(
                            EventLogHandle queryHandle,
                            int eventSize,
                            [MarshalAs(UnmanagedType.LPArray)] IntPtr[] events,
                            int timeout,
                            int flags,
                            ref int returned);

        [DllImport(WEVTAPI, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtCancel(EventLogHandle handle);

        [DllImport(WEVTAPI)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtClose(IntPtr handle);

        /*
        [DllImport(WEVTAPI, EntryPoint = "EvtClose", SetLastError = true)]
        public static extern bool EvtClose(
                            IntPtr eventHandle
                                           );
         */

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetEventInfo(
                            EventLogHandle eventHandle,
                            // int propertyId
                            [MarshalAs(UnmanagedType.I4)] EvtEventPropertyId propertyId,
                            int bufferSize,
                            IntPtr bufferPtr,
                            out int bufferUsed
                                            );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetQueryInfo(
                            EventLogHandle queryHandle,
                            [MarshalAs(UnmanagedType.I4)] EvtQueryPropertyId propertyId,
                            int bufferSize,
                            IntPtr buffer,
                            ref int bufferRequired
                                            );

        // PUBLISHER METADATA
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenPublisherMetadata(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string publisherId,
                            [MarshalAs(UnmanagedType.LPWStr)] string logFilePath,
                            int locale,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetPublisherMetadataProperty(
                            EventLogHandle publisherMetadataHandle,
                            [MarshalAs(UnmanagedType.I4)] EvtPublisherMetadataPropertyId propertyId,
                            int flags,
                            int publisherMetadataPropertyBufferSize,
                            IntPtr publisherMetadataPropertyBuffer,
                            out int publisherMetadataPropertyBufferUsed
                                    );

        // NEW
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetObjectArraySize(
                            EventLogHandle objectArray,
                            out int objectArraySize
                                        );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetObjectArrayProperty(
                            EventLogHandle objectArray,
                            int propertyId,
                            int arrayIndex,
                            int flags,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed
                                            );

        // NEW 2
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenEventMetadataEnum(
                            EventLogHandle publisherMetadata,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        // public static extern IntPtr EvtNextEventMetadata(
        internal static extern EventLogHandle EvtNextEventMetadata(
                            EventLogHandle eventMetadataEnum,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetEventMetadataProperty(
                            EventLogHandle eventMetadata,
                            [MarshalAs(UnmanagedType.I4)] EvtEventMetadataPropertyId propertyId,
                            int flags,
                            int eventMetadataPropertyBufferSize,
                            IntPtr eventMetadataPropertyBuffer,
                            out int eventMetadataPropertyBufferUsed
                                   );

        // Channel Configuration Native Api
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenChannelEnum(
                            EventLogHandle session,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtNextChannelPath(
                            EventLogHandle channelEnum,
                            int channelPathBufferSize,
                            // StringBuilder channelPathBuffer,
                            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder channelPathBuffer,
                            out int channelPathBufferUsed
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenPublisherEnum(
                            EventLogHandle session,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtNextPublisherId(
                            EventLogHandle publisherEnum,
                            int publisherIdBufferSize,
                            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder publisherIdBuffer,
                            out int publisherIdBufferUsed
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenChannelConfig(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string channelPath,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtSaveChannelConfig(
                            EventLogHandle channelConfig,
                            int flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtSetChannelConfigProperty(
                            EventLogHandle channelConfig,
                            [MarshalAs(UnmanagedType.I4)] EvtChannelConfigPropertyId propertyId,
                            int flags,
                            ref EvtVariant propertyValue
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetChannelConfigProperty(
                            EventLogHandle channelConfig,
                            [MarshalAs(UnmanagedType.I4)] EvtChannelConfigPropertyId propertyId,
                            int flags,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed
                                   );

        // Log Information Native API
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string path,
                            [MarshalAs(UnmanagedType.I4)] PathType flags
                                    );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtGetLogInfo(
                            EventLogHandle log,
                            [MarshalAs(UnmanagedType.I4)] EvtLogPropertyId propertyId,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed
                                    );

        // LOG MANIPULATION
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtExportLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string channelPath,
                            [MarshalAs(UnmanagedType.LPWStr)] string query,
                            [MarshalAs(UnmanagedType.LPWStr)] string targetFilePath,
                            int flags
                                        );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtArchiveExportedLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string logFilePath,
                            int locale,
                            int flags
                                        );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtClearLog(
                            EventLogHandle session,
                            [MarshalAs(UnmanagedType.LPWStr)] string channelPath,
                            [MarshalAs(UnmanagedType.LPWStr)] string targetFilePath,
                            int flags
                                        );

        // RENDERING
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtCreateRenderContext(
                            int valuePathsCount,
                            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
                                string[] valuePaths,
                            [MarshalAs(UnmanagedType.I4)] EvtRenderContextFlags flags
                                    );

        [DllImport(WEVTAPI, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EvtRenderFlags flags,
                            int buffSize,
                            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
                            out int buffUsed,
                            out int propCount
                                        );

        [DllImport(WEVTAPI, EntryPoint = "EvtRender", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            EvtRenderFlags flags,
                            int buffSize,
                            IntPtr buffer,
                            out int buffUsed,
                            out int propCount
                                        );

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        internal struct EvtStringVariant
        {
            [MarshalAs(UnmanagedType.LPWStr), FieldOffset(0)]
            public string StringVal;

            [FieldOffset(8)]
            public uint Count;

            [FieldOffset(12)]
            public uint Type;
        }

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtFormatMessage(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             EvtStringVariant[] values,
                             [MarshalAs(UnmanagedType.I4)] EvtFormatMessageFlags flags,
                             int bufferSize,
                             [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
                             out int bufferUsed
                                        );

        [DllImport(WEVTAPI, EntryPoint = "EvtFormatMessage", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtFormatMessageBuffer(
                             EventLogHandle publisherMetadataHandle,
                             EventLogHandle eventHandle,
                             uint messageId,
                             int valueCount,
                             IntPtr values,
                             [MarshalAs(UnmanagedType.I4)] EvtFormatMessageFlags flags,
                             int bufferSize,
                             IntPtr buffer,
                             out int bufferUsed
                                        );

        // SESSION
        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtOpenSession(
                            [MarshalAs(UnmanagedType.I4)] EvtLoginClass loginClass,
                            ref EvtRpcLogin login,
                            int timeout,
                            int flags
                                        );

        // BOOKMARK
        [DllImport(WEVTAPI, EntryPoint = "EvtCreateBookmark", CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        internal static extern EventLogHandle EvtCreateBookmark(
                            [MarshalAs(UnmanagedType.LPWStr)] string bookmarkXml
                                        );

        [DllImport(WEVTAPI, CharSet = CharSet.Unicode, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvtUpdateBookmark(
                            EventLogHandle bookmark,
                            EventLogHandle eventHandle
                                        );
    }
}
