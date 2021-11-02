// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This internal class contains wrapper methods over the Native
** Methods of the Eventlog API.   Unlike the raw Native Methods,
** these methods throw EventLogExceptions, check platform
** availability and perform additional helper functionality
** specific to function.  Also, all methods of this class expose
** the Link Demand for Unmanaged Permission to callers.
**
============================================================*/

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace System.Diagnostics.Eventing.Reader
{
    internal static class NativeWrapper
    {
        public class SystemProperties
        {
            // indicates if the SystemProperties values were already computed (for this event Instance, surely).
            public bool filled = false;

            public ushort? Id = null;
            public byte? Version = null;
            public ushort? Qualifiers = null;
            public byte? Level = null;
            public ushort? Task = null;
            public byte? Opcode = null;
            public ulong? Keywords = null;
            public ulong? RecordId = null;
            public string ProviderName = null;
            public Guid? ProviderId = null;
            public string ChannelName = null;
            public uint? ProcessId = null;
            public uint? ThreadId = null;
            public string ComputerName = null;
            public System.Security.Principal.SecurityIdentifier UserId = null;
            public DateTime? TimeCreated = null;
            public Guid? ActivityId = null;
            public Guid? RelatedActivityId = null;

            public SystemProperties()
            {
            }
        }

        private static void ThrowEventLogException(int errorCode)
        {
            string message = new Win32Exception(errorCode).Message;

            switch (errorCode)
            {
                case 2:
                case 3:
                case 15007:
                case 15027:
                case 15028:
                case 15002:
                    throw new EventLogNotFoundException(message);

                case 13:
                case 15005:
                    throw new EventLogInvalidDataException(message);

                case 1818: // RPC_S_CALL_CANCELED is converted to ERROR_CANCELLED
                case 1223:
                    throw new OperationCanceledException();

                case 15037:
                    throw new EventLogProviderDisabledException(message);

                case 5:
                    throw new UnauthorizedAccessException();

                case 15011:
                case 15012:
                    throw new EventLogReadingException(message);

                default:
                    throw new EventLogException(message);
            }
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtQuery(
                            EventLogHandle session,
                            string path,
                            string query,
                            int flags)
        {
            EventLogHandle handle = UnsafeNativeMethods.EvtQuery(session, path, query, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
                ThrowEventLogException(win32Error);
            return handle;
        }

        [System.Security.SecurityCritical]
        public static void EvtSeek(
                            EventLogHandle resultSet,
                            long position,
                            EventLogHandle bookmark,
                            int timeout,
                            UnsafeNativeMethods.EvtSeekFlags flags)
        {
            bool status = UnsafeNativeMethods.EvtSeek(resultSet, position, bookmark, timeout, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecurityCritical]
        public static bool EvtNext(
                            EventLogHandle queryHandle,
                            int eventSize,
                            IntPtr[] events,
                            int timeout,
                            int flags,
                            ref int returned)
        {
            bool status = UnsafeNativeMethods.EvtNext(queryHandle, eventSize, events, timeout, flags, ref returned);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status && win32Error != UnsafeNativeMethods.ERROR_NO_MORE_ITEMS)
                ThrowEventLogException(win32Error);
            return win32Error == 0;
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtCancel(EventLogHandle handle)
        {
            if (!UnsafeNativeMethods.EvtCancel(handle))
            {
                int win32Error = Marshal.GetLastWin32Error();
                ThrowEventLogException(win32Error);
            }
        }

        [System.Security.SecurityCritical]
        public static void EvtClose(IntPtr handle)
        {
            //
            // purposely don't check and throw - this is
            // always called in cleanup / finalize / etc..
            //
            UnsafeNativeMethods.EvtClose(handle);
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenProviderMetadata(
                            EventLogHandle session,
                            string ProviderId,
                            string logFilePath,
                            int locale,
                            int flags)
        {
            //
            // ignore locale and pass 0 instead: that way, the thread locale will be retrieved in the API layer
            // and the "strict rendering" flag will NOT be set.  Otherwise, the fall back logic is broken and the descriptions
            // are not returned if the exact locale is not present on the server.
            //
            EventLogHandle handle = UnsafeNativeMethods.EvtOpenPublisherMetadata(session, ProviderId, logFilePath, 0, flags);

            int win32Error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
                ThrowEventLogException(win32Error);
            return handle;
        }

        [System.Security.SecurityCritical]
        public static int EvtGetObjectArraySize(EventLogHandle objectArray)
        {
            int arraySize;
            bool status = UnsafeNativeMethods.EvtGetObjectArraySize(objectArray, out arraySize);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
            return arraySize;
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenEventMetadataEnum(EventLogHandle ProviderMetadata, int flags)
        {
            EventLogHandle emEnumHandle = UnsafeNativeMethods.EvtOpenEventMetadataEnum(ProviderMetadata, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (emEnumHandle.IsInvalid)
                ThrowEventLogException(win32Error);
            return emEnumHandle;
        }

        // returns null if EOF
        [System.Security.SecurityCritical]
        public static EventLogHandle EvtNextEventMetadata(EventLogHandle eventMetadataEnum, int flags)
        {
            EventLogHandle emHandle = UnsafeNativeMethods.EvtNextEventMetadata(eventMetadataEnum, flags);
            int win32Error = Marshal.GetLastWin32Error();

            if (emHandle.IsInvalid)
            {
                if (win32Error != UnsafeNativeMethods.ERROR_NO_MORE_ITEMS)
                    ThrowEventLogException(win32Error);
                return null;
            }

            return emHandle;
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenChannelEnum(EventLogHandle session, int flags)
        {
            EventLogHandle channelEnum = UnsafeNativeMethods.EvtOpenChannelEnum(session, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (channelEnum.IsInvalid)
                ThrowEventLogException(win32Error);
            return channelEnum;
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenProviderEnum(EventLogHandle session, int flags)
        {
            EventLogHandle pubEnum = UnsafeNativeMethods.EvtOpenPublisherEnum(session, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (pubEnum.IsInvalid)
                ThrowEventLogException(win32Error);
            return pubEnum;
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenChannelConfig(EventLogHandle session, string channelPath, int flags)
        {
            EventLogHandle handle = UnsafeNativeMethods.EvtOpenChannelConfig(session, channelPath, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
                ThrowEventLogException(win32Error);
            return handle;
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtSaveChannelConfig(EventLogHandle channelConfig, int flags)
        {
            bool status = UnsafeNativeMethods.EvtSaveChannelConfig(channelConfig, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenLog(EventLogHandle session, string path, PathType flags)
        {
            EventLogHandle logHandle = UnsafeNativeMethods.EvtOpenLog(session, path, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (logHandle.IsInvalid)
                ThrowEventLogException(win32Error);
            return logHandle;
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtExportLog(
                            EventLogHandle session,
                            string channelPath,
                            string query,
                            string targetFilePath,
                            int flags)
        {
            bool status;
            status = UnsafeNativeMethods.EvtExportLog(session, channelPath, query, targetFilePath, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtArchiveExportedLog(
                            EventLogHandle session,
                            string logFilePath,
                            int locale,
                            int flags)
        {
            bool status;
            status = UnsafeNativeMethods.EvtArchiveExportedLog(session, logFilePath, locale, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtClearLog(
                            EventLogHandle session,
                            string channelPath,
                            string targetFilePath,
                            int flags)
        {
            bool status;
            status = UnsafeNativeMethods.EvtClearLog(session, channelPath, targetFilePath, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtCreateRenderContext(
                            int valuePathsCount,
                            string[] valuePaths,
                            UnsafeNativeMethods.EvtRenderContextFlags flags)
        {
            EventLogHandle renderContextHandleValues = UnsafeNativeMethods.EvtCreateRenderContext(valuePathsCount, valuePaths, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (renderContextHandleValues.IsInvalid)
                ThrowEventLogException(win32Error);
            return renderContextHandleValues;
        }

        [System.Security.SecurityCritical]
        public static void EvtRender(
                            EventLogHandle context,
                            EventLogHandle eventHandle,
                            UnsafeNativeMethods.EvtRenderFlags flags,
                            StringBuilder buffer)
        {
            int buffUsed;
            int propCount;
            bool status = UnsafeNativeMethods.EvtRender(context, eventHandle, flags, buffer.Capacity, buffer, out buffUsed, out propCount);
            int win32Error = Marshal.GetLastWin32Error();

            if (!status)
            {
                if (win32Error == UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                {
                    // reallocate the new RenderBuffer with the right size.
                    buffer.Capacity = buffUsed;
                    status = UnsafeNativeMethods.EvtRender(context, eventHandle, flags, buffer.Capacity, buffer, out buffUsed, out propCount);
                    win32Error = Marshal.GetLastWin32Error();
                }

                if (!status)
                {
                    ThrowEventLogException(win32Error);
                }
            }
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtOpenSession(UnsafeNativeMethods.EvtLoginClass loginClass, ref UnsafeNativeMethods.EvtRpcLogin login, int timeout, int flags)
        {
            EventLogHandle handle = UnsafeNativeMethods.EvtOpenSession(loginClass, ref login, timeout, flags);
            int win32Error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
                ThrowEventLogException(win32Error);
            return handle;
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle EvtCreateBookmark(string bookmarkXml)
        {
            EventLogHandle handle = UnsafeNativeMethods.EvtCreateBookmark(bookmarkXml);
            int win32Error = Marshal.GetLastWin32Error();
            if (handle.IsInvalid)
                ThrowEventLogException(win32Error);
            return handle;
        }

        [System.Security.SecurityCritical]
        public static void EvtUpdateBookmark(EventLogHandle bookmark, EventLogHandle eventHandle)
        {
            bool status = UnsafeNativeMethods.EvtUpdateBookmark(bookmark, eventHandle);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);
        }

        [System.Security.SecuritySafeCritical]
        public static object EvtGetEventInfo(EventLogHandle handle, UnsafeNativeMethods.EvtEventPropertyId enumType)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetEventInfo(handle, enumType, 0, IntPtr.Zero, out bufferNeeded);
                int error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (error == UnsafeNativeMethods.ERROR_SUCCESS) { }
                    else
                        if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetEventInfo(handle, enumType, bufferNeeded, buffer, out bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecurityCritical]
        public static object EvtGetQueryInfo(EventLogHandle handle, UnsafeNativeMethods.EvtQueryPropertyId enumType)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded = 0;
            try
            {
                bool status = UnsafeNativeMethods.EvtGetQueryInfo(handle, enumType, 0, IntPtr.Zero, ref bufferNeeded);
                int error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetQueryInfo(handle, enumType, bufferNeeded, buffer, ref bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static object EvtGetPublisherMetadataProperty(EventLogHandle pmHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId thePropertyId)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetPublisherMetadataProperty(pmHandle, thePropertyId, 0, 0, IntPtr.Zero, out bufferNeeded);
                int error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetPublisherMetadataProperty(pmHandle, thePropertyId, 0, bufferNeeded, buffer, out bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecurityCritical]
        internal static EventLogHandle EvtGetPublisherMetadataPropertyHandle(EventLogHandle pmHandle, UnsafeNativeMethods.EvtPublisherMetadataPropertyId thePropertyId)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                int bufferNeeded;
                bool status = UnsafeNativeMethods.EvtGetPublisherMetadataProperty(pmHandle, thePropertyId, 0, 0, IntPtr.Zero, out bufferNeeded);
                int error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetPublisherMetadataProperty(pmHandle, thePropertyId, 0, bufferNeeded, buffer, out bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                //
                // note: there is a case where returned variant does have allocated native resources
                // associated with (e.g. ConfigArrayHandle).  If PtrToStructure throws, then we would
                // leak that resource - fortunately PtrToStructure only throws InvalidArgument which
                // is a logic error - not a possible runtime condition here.  Other System exceptions
                // shouldn't be handled anyhow and the application will terminate.
                //
                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToSafeHandle(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        // implies UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageId flag.
        [System.Security.SecurityCritical]
        public static string EvtFormatMessage(EventLogHandle handle, uint msgId)
        {
            int bufferNeeded;

            StringBuilder sb = new(null);
            bool status = UnsafeNativeMethods.EvtFormatMessage(handle, EventLogHandle.Zero, msgId, 0, null, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageId, 0, sb, out bufferNeeded);
            int error = Marshal.GetLastWin32Error();

            // ERROR_EVT_UNRESOLVED_VALUE_INSERT and its cousins are commonly returned for raw message text.
            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT
                        && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_PARAMETER_INSERT
                        && error != UnsafeNativeMethods.ERROR_EVT_MAX_INSERTS_REACHED)
            {
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    ThrowEventLogException(error);
            }

            sb.EnsureCapacity(bufferNeeded);
            status = UnsafeNativeMethods.EvtFormatMessage(handle, EventLogHandle.Zero, msgId, 0, null, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageId, bufferNeeded, sb, out bufferNeeded);
            error = Marshal.GetLastWin32Error();

            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT
                        && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_PARAMETER_INSERT
                        && error != UnsafeNativeMethods.ERROR_EVT_MAX_INSERTS_REACHED)
            {
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                if (error == UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT)
                {
                    return null;
                }

                ThrowEventLogException(error);
            }

            return sb.ToString();
        }

        [System.Security.SecurityCritical]
        public static object EvtGetObjectArrayProperty(EventLogHandle objArrayHandle, int index, int thePropertyId)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetObjectArrayProperty(objArrayHandle, thePropertyId, index, 0, 0, IntPtr.Zero, out bufferNeeded);
                int error = Marshal.GetLastWin32Error();

                if (!status)
                {
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetObjectArrayProperty(objArrayHandle, thePropertyId, index, 0, bufferNeeded, buffer, out bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecurityCritical]
        public static object EvtGetEventMetadataProperty(EventLogHandle handle, UnsafeNativeMethods.EvtEventMetadataPropertyId enumType)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetEventMetadataProperty(handle, enumType, 0, 0, IntPtr.Zero, out bufferNeeded);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (win32Error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(win32Error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetEventMetadataProperty(handle, enumType, 0, bufferNeeded, buffer, out bufferNeeded);
                win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static object EvtGetChannelConfigProperty(EventLogHandle handle, UnsafeNativeMethods.EvtChannelConfigPropertyId enumType)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetChannelConfigProperty(handle, enumType, 0, 0, IntPtr.Zero, out bufferNeeded);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (win32Error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(win32Error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetChannelConfigProperty(handle, enumType, 0, bufferNeeded, buffer, out bufferNeeded);
                win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);

                //
                // note: there is a case where returned variant does have allocated native resources
                // associated with (e.g. ConfigArrayHandle).  If PtrToStructure throws, then we would
                // leak that resource - fortunately PtrToStructure only throws InvalidArgument which
                // is a logic error - not a possible runtime condition here.  Other System exceptions
                // shouldn't be handled anyhow and the application will terminate.
                //
                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtSetChannelConfigProperty(EventLogHandle handle, UnsafeNativeMethods.EvtChannelConfigPropertyId enumType, object val)
        {
            UnsafeNativeMethods.EvtVariant varVal = new();

            CoTaskMemSafeHandle taskMem = new();

            using (taskMem)
            {
                if (val != null)
                {
                    switch (enumType)
                    {
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigEnabled:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBoolean;
                                if ((bool)val) varVal.Bool = 1;
                                else varVal.Bool = 0;
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelConfigAccess:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeString;
                                taskMem.SetMemory(Marshal.StringToCoTaskMemUni((string)val));
                                varVal.StringVal = taskMem.GetMemory();
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigLogFilePath:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeString;
                                taskMem.SetMemory(Marshal.StringToCoTaskMemUni((string)val));
                                varVal.StringVal = taskMem.GetMemory();
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigMaxSize:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt64;
                                varVal.ULong = (ulong)((long)val);
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigLevel:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt32;
                                varVal.UInteger = (uint)((int)val);
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelPublishingConfigKeywords:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt64;
                                varVal.ULong = (ulong)((long)val);
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigRetention:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBoolean;
                                if ((bool)val) varVal.Bool = 1;
                                else varVal.Bool = 0;
                            }

                            break;
                        case UnsafeNativeMethods.EvtChannelConfigPropertyId.EvtChannelLoggingConfigAutoBackup:
                            {
                                varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBoolean;
                                if ((bool)val) varVal.Bool = 1;
                                else varVal.Bool = 0;
                            }

                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
                else
                {
                    varVal.Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeNull;
                }

                bool status = UnsafeNativeMethods.EvtSetChannelConfigProperty(handle, enumType, 0, ref varVal);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);
            }
        }

        [System.Security.SecurityCritical]
        public static string EvtNextChannelPath(EventLogHandle handle, ref bool finish)
        {
            StringBuilder sb = new(null);
            int channelNameNeeded;

            bool status = UnsafeNativeMethods.EvtNextChannelPath(handle, 0, sb, out channelNameNeeded);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
            {
                if (win32Error == UnsafeNativeMethods.ERROR_NO_MORE_ITEMS)
                {
                    finish = true;
                    return null;
                }

                if (win32Error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    ThrowEventLogException(win32Error);
            }

            sb.EnsureCapacity(channelNameNeeded);
            status = UnsafeNativeMethods.EvtNextChannelPath(handle, channelNameNeeded, sb, out channelNameNeeded);
            win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);

            return sb.ToString();
        }

        [System.Security.SecurityCritical]
        public static string EvtNextPublisherId(EventLogHandle handle, ref bool finish)
        {
            StringBuilder sb = new(null);
            int ProviderIdNeeded;

            bool status = UnsafeNativeMethods.EvtNextPublisherId(handle, 0, sb, out ProviderIdNeeded);
            int win32Error = Marshal.GetLastWin32Error();
            if (!status)
            {
                if (win32Error == UnsafeNativeMethods.ERROR_NO_MORE_ITEMS)
                {
                    finish = true;
                    return null;
                }

                if (win32Error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    ThrowEventLogException(win32Error);
            }

            sb.EnsureCapacity(ProviderIdNeeded);
            status = UnsafeNativeMethods.EvtNextPublisherId(handle, ProviderIdNeeded, sb, out ProviderIdNeeded);
            win32Error = Marshal.GetLastWin32Error();
            if (!status)
                ThrowEventLogException(win32Error);

            return sb.ToString();
        }

        [System.Security.SecurityCritical]
        public static object EvtGetLogInfo(EventLogHandle handle, UnsafeNativeMethods.EvtLogPropertyId enumType)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                bool status = UnsafeNativeMethods.EvtGetLogInfo(handle, enumType, 0, IntPtr.Zero, out bufferNeeded);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (win32Error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(win32Error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtGetLogInfo(handle, enumType, bufferNeeded, buffer, out bufferNeeded);
                win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);

                UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(buffer);
                return ConvertToObject(varVal);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static void EvtRenderBufferWithContextSystem(EventLogHandle contextHandle, EventLogHandle eventHandle, UnsafeNativeMethods.EvtRenderFlags flag, SystemProperties systemProperties, int SYSTEM_PROPERTY_COUNT)
        {
            IntPtr buffer = IntPtr.Zero;
            IntPtr pointer = IntPtr.Zero;
            int bufferNeeded;
            int propCount;

            try
            {
                bool status = UnsafeNativeMethods.EvtRender(contextHandle, eventHandle, flag, 0, IntPtr.Zero, out bufferNeeded, out propCount);
                if (!status)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtRender(contextHandle, eventHandle, flag, bufferNeeded, buffer, out bufferNeeded, out propCount);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);

                if (propCount != SYSTEM_PROPERTY_COUNT)
                    throw new InvalidOperationException("We do not have " + SYSTEM_PROPERTY_COUNT + " variants given for the UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventValues flag. (System Properties)");

                pointer = buffer;
                // read each Variant structure
                for (int i = 0; i < propCount; i++)
                {
                    UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(pointer);
                    switch (i)
                    {
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemProviderName:
                            systemProperties.ProviderName = (string)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeString);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemProviderGuid:
                            systemProperties.ProviderId = (Guid?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeGuid);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemEventID:
                            systemProperties.Id = (ushort?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt16);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemQualifiers:
                            systemProperties.Qualifiers = (ushort?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt16);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemLevel:
                            systemProperties.Level = (byte?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeByte);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemTask:
                            systemProperties.Task = (ushort?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt16);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemOpcode:
                            systemProperties.Opcode = (byte?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeByte);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemKeywords:
                            systemProperties.Keywords = (ulong?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeHexInt64);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemTimeCreated:
                            systemProperties.TimeCreated = (DateTime?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeFileTime);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemEventRecordId:
                            systemProperties.RecordId = (ulong?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt64);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemActivityID:
                            systemProperties.ActivityId = (Guid?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeGuid);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemRelatedActivityID:
                            systemProperties.RelatedActivityId = (Guid?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeGuid);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemProcessID:
                            systemProperties.ProcessId = (uint?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt32);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemThreadID:
                            systemProperties.ThreadId = (uint?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt32);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemChannel:
                            systemProperties.ChannelName = (string)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeString);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemComputer:
                            systemProperties.ComputerName = (string)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeString);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemUserID:
                            systemProperties.UserId = (SecurityIdentifier)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeSid);
                            break;
                        case (int)UnsafeNativeMethods.EvtSystemPropertyId.EvtSystemVersion:
                            systemProperties.Version = (byte?)ConvertToObject(varVal, UnsafeNativeMethods.EvtVariantType.EvtVarTypeByte);
                            break;
                    }

                    pointer = new IntPtr(((long)pointer + Marshal.SizeOf(varVal)));
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        // EvtRenderContextFlags can be both: EvtRenderContextFlags.EvtRenderContextUser and EvtRenderContextFlags.EvtRenderContextValues
        // Render with Context = ContextUser or ContextValues (with user defined Xpath query strings)
        [System.Security.SecuritySafeCritical]
        public static IList<object> EvtRenderBufferWithContextUserOrValues(EventLogHandle contextHandle, EventLogHandle eventHandle)
        {
            IntPtr buffer = IntPtr.Zero;
            IntPtr pointer = IntPtr.Zero;
            int bufferNeeded;
            int propCount;
            const UnsafeNativeMethods.EvtRenderFlags flag = UnsafeNativeMethods.EvtRenderFlags.EvtRenderEventValues;

            try
            {
                bool status = UnsafeNativeMethods.EvtRender(contextHandle, eventHandle, flag, 0, IntPtr.Zero, out bufferNeeded, out propCount);
                if (!status)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtRender(contextHandle, eventHandle, flag, bufferNeeded, buffer, out bufferNeeded, out propCount);
                int win32Error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(win32Error);

                List<object> valuesList = new(propCount);
                if (propCount > 0)
                {
                    pointer = buffer;
                    for (int i = 0; i < propCount; i++)
                    {
                        UnsafeNativeMethods.EvtVariant varVal = Marshal.PtrToStructure<UnsafeNativeMethods.EvtVariant>(pointer);
                        valuesList.Add(ConvertToObject(varVal));
                        pointer = new IntPtr(((long)pointer + Marshal.SizeOf(varVal)));
                    }
                }

                return valuesList;
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecuritySafeCritical]
        public static string EvtFormatMessageRenderName(EventLogHandle pmHandle, EventLogHandle eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags flag)
        {
            int bufferNeeded;
            StringBuilder sb = new(null);

            bool status = UnsafeNativeMethods.EvtFormatMessage(pmHandle, eventHandle, 0, 0, null, flag, 0, sb, out bufferNeeded);
            int error = Marshal.GetLastWin32Error();

            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT)
            {
                //
                // ERROR_EVT_UNRESOLVED_VALUE_INSERT can be returned.  It means
                // message may have one or more unsubstituted strings.  This is
                // not an exception, but we have no way to convey the partial
                // success out to enduser.
                //
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                if (error != (int)UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    ThrowEventLogException(error);
            }

            sb.EnsureCapacity(bufferNeeded);
            status = UnsafeNativeMethods.EvtFormatMessage(pmHandle, eventHandle, 0, 0, null, flag, bufferNeeded, sb, out bufferNeeded);
            error = Marshal.GetLastWin32Error();

            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT)
            {
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                ThrowEventLogException(error);
            }

            return sb.ToString();
        }

        // The EvtFormatMessage used for the obtaining of the Keywords names.
        [System.Security.SecuritySafeCritical]
        public static IEnumerable<string> EvtFormatMessageRenderKeywords(EventLogHandle pmHandle, EventLogHandle eventHandle, UnsafeNativeMethods.EvtFormatMessageFlags flag)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;

            try
            {
                List<string> keywordsList = new();
                bool status = UnsafeNativeMethods.EvtFormatMessageBuffer(pmHandle, eventHandle, 0, 0, IntPtr.Zero, flag, 0, IntPtr.Zero, out bufferNeeded);
                int error = Marshal.GetLastWin32Error();

                if (!status)
                {
                    switch (error)
                    {
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                            return keywordsList.AsReadOnly();
                    }

                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal(bufferNeeded * 2);
                status = UnsafeNativeMethods.EvtFormatMessageBuffer(pmHandle, eventHandle, 0, 0, IntPtr.Zero, flag, bufferNeeded, buffer, out bufferNeeded);
                error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    switch (error)
                    {
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                        case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                            return keywordsList;
                    }

                    ThrowEventLogException(error);
                }

                IntPtr pointer = buffer;

                while (true)
                {
                    string s = Marshal.PtrToStringUni(pointer);
                    if (string.IsNullOrEmpty(s))
                        break;
                    keywordsList.Add(s);
                    // nr of bytes = # chars * 2 + 2 bytes for character '\0'.
                    pointer = new IntPtr((long)pointer + (s.Length * 2) + 2);
                }

                return keywordsList.AsReadOnly();
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        [System.Security.SecurityCritical]
        public static string EvtRenderBookmark(EventLogHandle eventHandle)
        {
            IntPtr buffer = IntPtr.Zero;
            int bufferNeeded;
            int propCount;
            const UnsafeNativeMethods.EvtRenderFlags flag = UnsafeNativeMethods.EvtRenderFlags.EvtRenderBookmark;

            try
            {
                bool status = UnsafeNativeMethods.EvtRender(EventLogHandle.Zero, eventHandle, flag, 0, IntPtr.Zero, out bufferNeeded, out propCount);
                int error = Marshal.GetLastWin32Error();
                if (!status)
                {
                    if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        ThrowEventLogException(error);
                }

                buffer = Marshal.AllocHGlobal((int)bufferNeeded);
                status = UnsafeNativeMethods.EvtRender(EventLogHandle.Zero, eventHandle, flag, bufferNeeded, buffer, out bufferNeeded, out propCount);
                error = Marshal.GetLastWin32Error();
                if (!status)
                    ThrowEventLogException(error);

                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }

        // Get the formatted description, using the msgId for FormatDescription(string [])
        [System.Security.SecuritySafeCritical]
        public static string EvtFormatMessageFormatDescription(EventLogHandle handle, EventLogHandle eventHandle, string[] values)
        {
            int bufferNeeded;

            UnsafeNativeMethods.EvtStringVariant[] stringVariants = new UnsafeNativeMethods.EvtStringVariant[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                stringVariants[i].Type = (uint)UnsafeNativeMethods.EvtVariantType.EvtVarTypeString;
                stringVariants[i].StringVal = values[i];
            }

            StringBuilder sb = new(null);
            bool status = UnsafeNativeMethods.EvtFormatMessage(handle, eventHandle, 0xffffffff, values.Length, stringVariants, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageEvent, 0, sb, out bufferNeeded);
            int error = Marshal.GetLastWin32Error();

            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT)
            {
                //
                // ERROR_EVT_UNRESOLVED_VALUE_INSERT can be returned.  It means
                // message may have one or more unsubstituted strings.  This is
                // not an exception, but we have no way to convey the partial
                // success out to enduser.
                //
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                if (error != UnsafeNativeMethods.ERROR_INSUFFICIENT_BUFFER)
                    ThrowEventLogException(error);
            }

            sb.EnsureCapacity(bufferNeeded);
            status = UnsafeNativeMethods.EvtFormatMessage(handle, eventHandle, 0xffffffff, values.Length, stringVariants, UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageEvent, bufferNeeded, sb, out bufferNeeded);
            error = Marshal.GetLastWin32Error();

            if (!status && error != UnsafeNativeMethods.ERROR_EVT_UNRESOLVED_VALUE_INSERT)
            {
                switch (error)
                {
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_ID_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_RESOURCE_LANG_NOT_FOUND:
                    case UnsafeNativeMethods.ERROR_MUI_FILE_NOT_FOUND:
                        return null;
                }

                ThrowEventLogException(error);
            }

            return sb.ToString();
        }

        [System.Security.SecurityCritical]
        private static object ConvertToObject(UnsafeNativeMethods.EvtVariant val)
        {
            switch (val.Type)
            {
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt32:
                    return val.UInteger;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt32:
                    return val.Integer;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt16:
                    return val.UShort;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt16:
                    return val.SByte;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeByte:
                    return val.UInt8;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSByte:
                    return val.SByte;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt64:
                    return val.ULong;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt64:
                    return val.Long;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeHexInt64:
                    return val.ULong;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeHexInt32:
                    return val.Integer;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSingle:
                    return val.Single;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeDouble:
                    return val.Double;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeNull:
                    return null;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeString:
                    return ConvertToString(val);
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeAnsiString:
                    return ConvertToAnsiString(val);
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSid:
                    return (val.SidVal == IntPtr.Zero) ? null : new SecurityIdentifier(val.SidVal);
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeGuid:
                    return (val.GuidReference == IntPtr.Zero) ? Guid.Empty : Marshal.PtrToStructure<Guid>(val.GuidReference);
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeEvtHandle:
                    return ConvertToSafeHandle(val);
                case (int)(int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeFileTime:
                    return DateTime.FromFileTime((long)val.FileTime);
                case (int)(int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSysTime:
                    UnsafeNativeMethods.SystemTime sysTime = Marshal.PtrToStructure<UnsafeNativeMethods.SystemTime>(val.SystemTime);
                    return new DateTime(sysTime.Year, sysTime.Month, sysTime.Day, sysTime.Hour, sysTime.Minute, sysTime.Second, sysTime.Milliseconds);
                case (int)(int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSizeT:
                    return val.SizeT;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBoolean:
                    if (val.Bool != 0) return true;
                    else return false;
                case (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBinary:
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeByte):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<byte>();
                    byte[] arByte = new byte[val.Count];
                    Marshal.Copy(val.Reference, arByte, 0, (int)val.Count);
                    return arByte;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt16):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<short>();
                    short[] arInt16 = new short[val.Count];
                    Marshal.Copy(val.Reference, arInt16, 0, (int)val.Count);
                    return arInt16;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt32):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<int>();
                    int[] arInt32 = new int[val.Count];
                    Marshal.Copy(val.Reference, arInt32, 0, (int)val.Count);
                    return arInt32;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeInt64):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<long>();
                    long[] arInt64 = new long[val.Count];
                    Marshal.Copy(val.Reference, arInt64, 0, (int)val.Count);
                    return arInt64;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSingle):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<float>();
                    float[] arSingle = new float[val.Count];
                    Marshal.Copy(val.Reference, arSingle, 0, (int)val.Count);
                    return arSingle;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeDouble):
                    if (val.Reference == IntPtr.Zero) return Array.Empty<double>();
                    double[] arDouble = new double[val.Count];
                    Marshal.Copy(val.Reference, arDouble, 0, (int)val.Count);
                    return arDouble;
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSByte):
                    return ConvertToArray<sbyte>(val, sizeof(sbyte)); // not CLS-compliant
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt16):
                    return ConvertToArray<ushort>(val, sizeof(ushort));
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt64):
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeHexInt64):
                    return ConvertToArray<ulong>(val, sizeof(ulong));
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeUInt32):
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeHexInt32):
                    return ConvertToArray<uint>(val, sizeof(uint));
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeString):
                    return ConvertToStringArray(val, false);
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeAnsiString):
                    return ConvertToStringArray(val, true);
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBoolean):
                    return ConvertToBoolArray(val);
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeGuid):
                    return ConvertToArray<Guid>(val, 16 * sizeof(byte));
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeFileTime):
                    return ConvertToFileTimeArray(val);
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSysTime):
                    return ConvertToSysTimeArray(val);
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeBinary): // both length and count in the manifest: tracrpt supports, Crimson APIs don't
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSizeT):  // unused: array of win:pointer is returned as HexIntXX
                case ((int)UnsafeNativeMethods.EvtMasks.EVT_VARIANT_TYPE_ARRAY | (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeSid): // unsupported by native APIs
                default:
                    throw new EventLogInvalidDataException();
            }
        }

        [System.Security.SecurityCritical]
        public static object ConvertToObject(UnsafeNativeMethods.EvtVariant val, UnsafeNativeMethods.EvtVariantType desiredType)
        {
            if (val.Type == (int)UnsafeNativeMethods.EvtVariantType.EvtVarTypeNull) return null;
            if (val.Type != (int)desiredType)
                throw new EventLogInvalidDataException();

            return ConvertToObject(val);
        }

        [System.Security.SecurityCritical]
        public static string ConvertToString(UnsafeNativeMethods.EvtVariant val)
        {
            if (val.StringVal == IntPtr.Zero)
                return string.Empty;
            else
                return Marshal.PtrToStringUni(val.StringVal);
        }

        [System.Security.SecurityCritical]
        public static string ConvertToAnsiString(UnsafeNativeMethods.EvtVariant val)
        {
            if (val.AnsiString == IntPtr.Zero)
                return string.Empty;
            else
                return Marshal.PtrToStringAnsi(val.AnsiString);
        }

        [System.Security.SecurityCritical]
        public static EventLogHandle ConvertToSafeHandle(UnsafeNativeMethods.EvtVariant val)
        {
            if (val.Handle == IntPtr.Zero)
                return EventLogHandle.Zero;
            else
                return new EventLogHandle(val.Handle, true);
        }

        [System.Security.SecurityCritical]
        public static Array ConvertToArray<T>(UnsafeNativeMethods.EvtVariant val, int size) where T : struct
        {
            IntPtr ptr = val.Reference;
            if (ptr == IntPtr.Zero)
            {
                return Array.CreateInstance(typeof(T), 0);
            }
            else
            {
                Array array = Array.CreateInstance(typeof(T), (int)val.Count);
                for (int i = 0; i < val.Count; i++)
                {
                    array.SetValue(Marshal.PtrToStructure<T>(ptr), i);
                    ptr = new IntPtr((long)ptr + size);
                }

                return array;
            }
        }

        [System.Security.SecurityCritical]
        public static Array ConvertToBoolArray(UnsafeNativeMethods.EvtVariant val)
        {
            // NOTE: booleans are padded to 4 bytes in ETW
            IntPtr ptr = val.Reference;
            if (ptr == IntPtr.Zero)
            {
                return Array.Empty<bool>();
            }
            else
            {
                bool[] array = new bool[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    bool value = Marshal.ReadInt32(ptr) != 0;
                    array[i] = value;
                    ptr = new IntPtr((long)ptr + 4);
                }

                return array;
            }
        }

        [System.Security.SecurityCritical]
        public static Array ConvertToFileTimeArray(UnsafeNativeMethods.EvtVariant val)
        {
            IntPtr ptr = val.Reference;
            if (ptr == IntPtr.Zero)
            {
                return Array.Empty<DateTime>();
            }
            else
            {
                DateTime[] array = new DateTime[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    array[i] = DateTime.FromFileTime(Marshal.ReadInt64(ptr));
                    ptr = new IntPtr((long)ptr + 8 * sizeof(byte)); // FILETIME values are 8 bytes
                }

                return array;
            }
        }

        [System.Security.SecurityCritical]
        public static Array ConvertToSysTimeArray(UnsafeNativeMethods.EvtVariant val)
        {
            IntPtr ptr = val.Reference;
            if (ptr == IntPtr.Zero)
            {
                return Array.Empty<DateTime>();
            }
            else
            {
                DateTime[] array = new DateTime[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    UnsafeNativeMethods.SystemTime sysTime = Marshal.PtrToStructure<UnsafeNativeMethods.SystemTime>(ptr);
                    array[i] = new DateTime(sysTime.Year, sysTime.Month, sysTime.Day, sysTime.Hour, sysTime.Minute, sysTime.Second, sysTime.Milliseconds);
                    ptr = new IntPtr((long)ptr + 16 * sizeof(byte)); // SystemTime values are 16 bytes
                }

                return array;
            }
        }

        [System.Security.SecurityCritical]
        public static string[] ConvertToStringArray(UnsafeNativeMethods.EvtVariant val, bool ansi)
        {
            if (val.Reference == IntPtr.Zero)
            {
                return Array.Empty<string>();
            }
            else
            {
                IntPtr ptr = val.Reference;
                IntPtr[] pointersToString = new IntPtr[val.Count];
                Marshal.Copy(ptr, pointersToString, 0, (int)val.Count);
                string[] stringArray = new string[val.Count];
                for (int i = 0; i < val.Count; i++)
                {
                    stringArray[i] = ansi ? Marshal.PtrToStringAnsi(pointersToString[i]) : Marshal.PtrToStringUni(pointersToString[i]);
                }

                return stringArray;
            }
        }
    }
}
