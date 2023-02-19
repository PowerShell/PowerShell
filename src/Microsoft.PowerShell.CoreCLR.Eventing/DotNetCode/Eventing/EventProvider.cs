// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System.Diagnostics.Eventing
{
    public class EventProvider : IDisposable
    {
        [SecurityCritical]
        private UnsafeNativeMethods.EtwEnableCallback _etwCallback;  // Trace Callback function

        private long _regHandle;                       // Trace Registration Handle
        private byte _level;                            // Tracing Level
        private long _anyKeywordMask;                  // Trace Enable Flags
        private long _allKeywordMask;                  // Match all keyword
        private int _enabled;                           // Enabled flag from Trace callback
        private readonly Guid _providerId;              // Control Guid
        private int _disposed;                          // when 1, provider has unregister

        [ThreadStatic]
        private static WriteEventErrorCode t_returnCode; // thread slot to keep last error

        [ThreadStatic]
        private static Guid t_activityId;

        private const int s_basicTypeAllocationBufferSize = 16;
        private const int s_etwMaxNumberArguments = 32;
        private const int s_etwAPIMaxStringCount = 8;
        private const int s_maxEventDataDescriptors = 128;
        private const int s_traceEventMaximumSize = 65482;
        private const int s_traceEventMaximumStringSize = 32724;

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public enum WriteEventErrorCode : int
        {
            // check mapping to runtime codes
            NoError = 0,
            NoFreeBuffers = 1,
            EventTooBig = 2
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct EventData
        {
            [FieldOffset(0)]
            internal ulong DataPointer;

            [FieldOffset(8)]
            internal uint Size;

            [FieldOffset(12)]
            internal int Reserved;
        }

        private enum ActivityControl : uint
        {
            EVENT_ACTIVITY_CTRL_GET_ID = 1,
            EVENT_ACTIVITY_CTRL_SET_ID = 2,
            EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
            EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
            EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
        }

        /// <summary>
        /// Constructor for EventProvider class.
        /// </summary>
        /// <param name="providerGuid">
        /// Unique GUID among all trace sources running on a system
        /// </param>
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "guid")]
        public EventProvider(Guid providerGuid)
        {
            _providerId = providerGuid;

            //
            // EtwRegister the ProviderId with ETW
            //
            EtwRegister();
        }

        /// <summary>
        /// This method registers the controlGuid of this class with ETW.
        /// We need to be running on Vista or above. If not an
        /// PlatformNotSupported exception will be thrown.
        /// If for some reason the ETW EtwRegister call failed
        /// a NotSupported exception will be thrown.
        /// </summary>
        [System.Security.SecurityCritical]
        private unsafe void EtwRegister()
        {
            uint status;

            _etwCallback = new UnsafeNativeMethods.EtwEnableCallback(EtwEnableCallBack);

            status = UnsafeNativeMethods.EventRegister(in _providerId, _etwCallback, null, ref _regHandle);
            if (status != 0)
            {
                throw new Win32Exception((int)status);
            }
        }

        //
        // implement Dispose Pattern to early deregister from ETW instead of waiting for
        // the finalizer to call deregistration.
        // Once the user is done with the provider it needs to call Close() or Dispose()
        // If neither are called the finalizer will unregister the provider anyway
        //
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Security.SecuritySafeCritical]
        protected virtual void Dispose(bool disposing)
        {
            //
            // explicit cleanup is done by calling Dispose with true from
            // Dispose() or Close(). The disposing argument is ignored because there
            // are no unmanaged resources.
            // The finalizer calls Dispose with false.
            //

            //
            // check if the object has been already disposed
            //
            if (_disposed == 1) return;

            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                // somebody is already disposing the provider
                return;
            }

            //
            // Disables Tracing in the provider, then unregister
            //

            _enabled = 0;

            Deregister();
        }

        /// <summary>
        /// This method deregisters the controlGuid of this class with ETW.
        /// </summary>
        public virtual void Close()
        {
            Dispose();
        }

        ~EventProvider()
        {
            Dispose(false);
        }

        /// <summary>
        /// This method un-registers from ETW.
        /// </summary>
        [System.Security.SecurityCritical]
        private unsafe void Deregister()
        {
            //
            // Unregister from ETW using the RegHandle saved from
            // the register call.
            //

            if (_regHandle != 0)
            {
                UnsafeNativeMethods.EventUnregister(_regHandle);
                _regHandle = 0;
            }
        }

        [System.Security.SecurityCritical]
        private unsafe void EtwEnableCallBack(
                        [In] ref System.Guid sourceId,
                        [In] int isEnabled,
                        [In] byte setLevel,
                        [In] long anyKeyword,
                        [In] long allKeyword,
                        [In] void* filterData,
                        [In] void* callbackContext
                        )
        {
            _enabled = isEnabled;
            _level = setLevel;
            _anyKeywordMask = anyKeyword;
            _allKeywordMask = allKeyword;
            return;
        }

        /// <summary>
        /// IsEnabled, method used to test if provider is enabled.
        /// </summary>
        public bool IsEnabled()
        {
            return _enabled != 0;
        }

        /// <summary>
        /// IsEnabled, method used to test if event is enabled.
        /// </summary>
        /// <param name="level">
        /// Level to test
        /// </param>
        /// <param name="keywords">
        /// Keyword to test
        /// </param>
        public bool IsEnabled(byte level, long keywords)
        {
            //
            // If not enabled at all, return false.
            //

            if (_enabled == 0)
            {
                return false;
            }

            // This also covers the case of Level == 0.
            if ((level <= _level) ||
                (_level == 0))
            {
                //
                // Check if Keyword is enabled
                //

                if ((keywords == 0) ||
                    (((keywords & _anyKeywordMask) != 0) &&
                     ((keywords & _allKeywordMask) == _allKeywordMask)))
                {
                    return true;
                }
            }

            return false;
        }

        public static WriteEventErrorCode GetLastWriteEventError()
        {
            return t_returnCode;
        }

        //
        // Helper function to set the last error on the thread
        //
        private static void SetLastError(int error)
        {
            switch (error)
            {
                case UnsafeNativeMethods.ERROR_ARITHMETIC_OVERFLOW:
                case UnsafeNativeMethods.ERROR_MORE_DATA:
                    t_returnCode = WriteEventErrorCode.EventTooBig;
                    break;
                case UnsafeNativeMethods.ERROR_NOT_ENOUGH_MEMORY:
                    t_returnCode = WriteEventErrorCode.NoFreeBuffers;
                    break;
            }
        }

        [System.Security.SecurityCritical]
        private static unsafe string EncodeObject(ref object data, EventData* dataDescriptor, byte* dataBuffer)
        /*++

        Routine Description:

           This routine is used by WriteEvent to unbox the object type and
           to fill the passed in ETW data descriptor.

        Arguments:

           data - argument to be decoded

           dataDescriptor - pointer to the descriptor to be filled

           dataBuffer - storage buffer for storing user data, needed because cant get the address of the object

        Return Value:

           null if the object is a basic type other than string. String otherwise

        --*/
        {
            dataDescriptor->Reserved = 0;

            string sRet = data as string;
            if (sRet != null)
            {
                dataDescriptor->Size = (uint)((sRet.Length + 1) * 2);
                return sRet;
            }

            if (data == null)
            {
                dataDescriptor->Size = 0;
                dataDescriptor->DataPointer = 0;
            }
            else if (data is IntPtr)
            {
                dataDescriptor->Size = (uint)sizeof(IntPtr);
                IntPtr* intptrPtr = (IntPtr*)dataBuffer;
                *intptrPtr = (IntPtr)data;
                dataDescriptor->DataPointer = (ulong)intptrPtr;
            }
            else if (data is int)
            {
                dataDescriptor->Size = (uint)sizeof(int);
                int* intptrPtr = (int*)dataBuffer;
                *intptrPtr = (int)data;
                dataDescriptor->DataPointer = (ulong)intptrPtr;
            }
            else if (data is long)
            {
                dataDescriptor->Size = (uint)sizeof(long);
                long* longptr = (long*)dataBuffer;
                *longptr = (long)data;
                dataDescriptor->DataPointer = (ulong)longptr;
            }
            else if (data is uint)
            {
                dataDescriptor->Size = (uint)sizeof(uint);
                uint* uintptr = (uint*)dataBuffer;
                *uintptr = (uint)data;
                dataDescriptor->DataPointer = (ulong)uintptr;
            }
            else if (data is ulong)
            {
                dataDescriptor->Size = (uint)sizeof(ulong);
                ulong* ulongptr = (ulong*)dataBuffer;
                *ulongptr = (ulong)data;
                dataDescriptor->DataPointer = (ulong)ulongptr;
            }
            else if (data is char)
            {
                dataDescriptor->Size = (uint)sizeof(char);
                char* charptr = (char*)dataBuffer;
                *charptr = (char)data;
                dataDescriptor->DataPointer = (ulong)charptr;
            }
            else if (data is byte)
            {
                dataDescriptor->Size = (uint)sizeof(byte);
                byte* byteptr = (byte*)dataBuffer;
                *byteptr = (byte)data;
                dataDescriptor->DataPointer = (ulong)byteptr;
            }
            else if (data is short)
            {
                dataDescriptor->Size = (uint)sizeof(short);
                short* shortptr = (short*)dataBuffer;
                *shortptr = (short)data;
                dataDescriptor->DataPointer = (ulong)shortptr;
            }
            else if (data is sbyte)
            {
                dataDescriptor->Size = (uint)sizeof(sbyte);
                sbyte* sbyteptr = (sbyte*)dataBuffer;
                *sbyteptr = (sbyte)data;
                dataDescriptor->DataPointer = (ulong)sbyteptr;
            }
            else if (data is ushort)
            {
                dataDescriptor->Size = (uint)sizeof(ushort);
                ushort* ushortptr = (ushort*)dataBuffer;
                *ushortptr = (ushort)data;
                dataDescriptor->DataPointer = (ulong)ushortptr;
            }
            else if (data is float)
            {
                dataDescriptor->Size = (uint)sizeof(float);
                float* floatptr = (float*)dataBuffer;
                *floatptr = (float)data;
                dataDescriptor->DataPointer = (ulong)floatptr;
            }
            else if (data is double)
            {
                dataDescriptor->Size = (uint)sizeof(double);
                double* doubleptr = (double*)dataBuffer;
                *doubleptr = (double)data;
                dataDescriptor->DataPointer = (ulong)doubleptr;
            }
            else if (data is bool)
            {
                dataDescriptor->Size = (uint)sizeof(bool);
                bool* boolptr = (bool*)dataBuffer;
                *boolptr = (bool)data;
                dataDescriptor->DataPointer = (ulong)boolptr;
            }
            else if (data is Guid)
            {
                dataDescriptor->Size = (uint)sizeof(Guid);
                Guid* guidptr = (Guid*)dataBuffer;
                *guidptr = (Guid)data;
                dataDescriptor->DataPointer = (ulong)guidptr;
            }
            else if (data is decimal)
            {
                dataDescriptor->Size = (uint)sizeof(decimal);
                decimal* decimalptr = (decimal*)dataBuffer;
                *decimalptr = (decimal)data;
                dataDescriptor->DataPointer = (ulong)decimalptr;
            }
            else
            {
                // To our eyes, everything else is a just a string
                sRet = data.ToString();
                dataDescriptor->Size = (uint)((sRet.Length + 1) * 2);
                return sRet;
            }

            return null;
        }

        /// <summary>
        /// WriteMessageEvent, method to write a string with level and Keyword.
        /// The activity ID will be propagated only if the call stays on the same native thread as SetActivityId().
        /// </summary>
        /// <param name="eventMessage">
        /// Message to write
        /// </param>
        /// <param name="eventLevel">
        /// Level to test
        /// </param>
        /// <param name="eventKeywords">
        /// Keyword to test
        /// </param>
        [System.Security.SecurityCritical]
        public bool WriteMessageEvent(string eventMessage, byte eventLevel, long eventKeywords)
        {
            int status = 0;

            ArgumentNullException.ThrowIfNull(eventMessage);

            if (IsEnabled(eventLevel, eventKeywords))
            {
                if (eventMessage.Length > s_traceEventMaximumStringSize)
                {
                    t_returnCode = WriteEventErrorCode.EventTooBig;
                    return false;
                }

                unsafe
                {
                    fixed (char* pdata = eventMessage)
                    {
                        status = (int)UnsafeNativeMethods.EventWriteString(_regHandle, eventLevel, eventKeywords, pdata);
                    }

                    if (status != 0)
                    {
                        SetLastError(status);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// WriteMessageEvent, method to write a string with level=0 and Keyword=0
        /// The activity ID will be propagated only if the call stays on the same native thread as SetActivityId().
        /// </summary>
        /// <param name="eventMessage">
        /// Message to log
        /// </param>
        public bool WriteMessageEvent(string eventMessage)
        {
            return WriteMessageEvent(eventMessage, 0, 0);
        }

        /// <summary>
        /// WriteEvent method to write parameters with event schema properties.
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="eventPayload">
        /// </param>
        public bool WriteEvent(in EventDescriptor eventDescriptor, params object[] eventPayload)
        {
            return WriteTransferEvent(in eventDescriptor, Guid.Empty, eventPayload);
        }

        /// <summary>
        /// WriteEvent, method to write a string with event schema properties.
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="data">
        /// string to log.
        /// </param>
        [System.Security.SecurityCritical]
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public bool WriteEvent(in EventDescriptor eventDescriptor, string data)
        {
            uint status = 0;

            ArgumentNullException.ThrowIfNull(data);

            if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
            {
                if (data.Length > s_traceEventMaximumStringSize)
                {
                    t_returnCode = WriteEventErrorCode.EventTooBig;
                    return false;
                }

                EventData userData;

                userData.Size = (uint)((data.Length + 1) * 2);
                userData.Reserved = 0;

                unsafe
                {
                    fixed (char* pdata = data)
                    {
                        Guid activityId = GetActivityId();
                        userData.DataPointer = (ulong)pdata;

                        status = UnsafeNativeMethods.EventWriteTransfer(_regHandle,
                                                                        in eventDescriptor,
                                                                        (activityId == Guid.Empty) ? null : &activityId,
                                                                        null,
                                                                        1,
                                                                        &userData);
                    }
                }
            }

            if (status != 0)
            {
                SetLastError((int)status);
                return false;
            }

            return true;
        }

        /// <summary>
        /// WriteEvent, method to be used by generated code on a derived class.
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="dataCount">
        /// number of event descriptors
        /// </param>
        /// <param name="data">
        /// pointer do the event data
        /// </param>
        [System.Security.SecurityCritical]
        protected bool WriteEvent(in EventDescriptor eventDescriptor, int dataCount, IntPtr data)
        {
            uint status = 0;

            unsafe
            {
                Guid activityId = GetActivityId();

                status = UnsafeNativeMethods.EventWriteTransfer(
                                    _regHandle,
                                    in eventDescriptor,
                                    (activityId == Guid.Empty) ? null : &activityId,
                                    null,
                                    (uint)dataCount,
                                    (void*)data);
            }

            if (status != 0)
            {
                SetLastError((int)status);
                return false;
            }

            return true;
        }

        /// <summary>
        /// WriteTransferEvent, method to write a parameters with event schema properties.
        /// </summary>
        /// <param name="eventDescriptor">
        /// Event Descriptor for this event.
        /// </param>
        /// <param name="relatedActivityId">
        /// </param>
        /// <param name="eventPayload">
        /// </param>
        [System.Security.SecurityCritical]
        public bool WriteTransferEvent(in EventDescriptor eventDescriptor, Guid relatedActivityId, params object[] eventPayload)
        {
            uint status = 0;

            if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
            {
                Guid activityId = GetActivityId();

                unsafe
                {
                    int argCount = 0;
                    EventData* userDataPtr = null;

                    if ((eventPayload != null) && (eventPayload.Length != 0))
                    {
                        argCount = eventPayload.Length;
                        if (argCount > s_etwMaxNumberArguments)
                        {
                            //
                            // too many arguments to log
                            //
                            throw new ArgumentOutOfRangeException(nameof(eventPayload),
                                string.Format(CultureInfo.CurrentCulture, DotNetEventingStrings.ArgumentOutOfRange_MaxArgExceeded, s_etwMaxNumberArguments));
                        }

                        uint totalEventSize = 0;
                        int index;
                        int stringIndex = 0;
                        int[] stringPosition = new int[s_etwAPIMaxStringCount]; // used to keep the position of strings in the eventPayload parameter
                        string[] dataString = new string[s_etwAPIMaxStringCount]; // string arrays from the eventPayload parameter
                        EventData* userData = stackalloc EventData[argCount];             // allocation for the data descriptors
                        userDataPtr = (EventData*)userData;
                        byte* dataBuffer = stackalloc byte[s_basicTypeAllocationBufferSize * argCount]; // 16 byte for unboxing non-string argument
                        byte* currentBuffer = dataBuffer;

                        //
                        // The loop below goes through all the arguments and fills in the data
                        // descriptors. For strings save the location in the dataString array.
                        // Calculates the total size of the event by adding the data descriptor
                        // size value set in EncodeObject method.
                        //
                        for (index = 0; index < eventPayload.Length; index++)
                        {
                            string isString;
                            isString = EncodeObject(ref eventPayload[index], userDataPtr, currentBuffer);
                            currentBuffer += s_basicTypeAllocationBufferSize;
                            totalEventSize += userDataPtr->Size;
                            userDataPtr++;
                            if (isString != null)
                            {
                                if (stringIndex < s_etwAPIMaxStringCount)
                                {
                                    dataString[stringIndex] = isString;
                                    stringPosition[stringIndex] = index;
                                    stringIndex++;
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException(nameof(eventPayload),
                                        string.Format(CultureInfo.CurrentCulture, DotNetEventingStrings.ArgumentOutOfRange_MaxStringsExceeded, s_etwAPIMaxStringCount));
                                }
                            }
                        }

                        if (totalEventSize > s_traceEventMaximumSize)
                        {
                            t_returnCode = WriteEventErrorCode.EventTooBig;
                            return false;
                        }

                        fixed (char* v0 = dataString[0], v1 = dataString[1], v2 = dataString[2], v3 = dataString[3],
                                v4 = dataString[4], v5 = dataString[5], v6 = dataString[6], v7 = dataString[7])
                        {
                            userDataPtr = (EventData*)userData;
                            if (dataString[0] != null)
                            {
                                userDataPtr[stringPosition[0]].DataPointer = (ulong)v0;
                            }

                            if (dataString[1] != null)
                            {
                                userDataPtr[stringPosition[1]].DataPointer = (ulong)v1;
                            }

                            if (dataString[2] != null)
                            {
                                userDataPtr[stringPosition[2]].DataPointer = (ulong)v2;
                            }

                            if (dataString[3] != null)
                            {
                                userDataPtr[stringPosition[3]].DataPointer = (ulong)v3;
                            }

                            if (dataString[4] != null)
                            {
                                userDataPtr[stringPosition[4]].DataPointer = (ulong)v4;
                            }

                            if (dataString[5] != null)
                            {
                                userDataPtr[stringPosition[5]].DataPointer = (ulong)v5;
                            }

                            if (dataString[6] != null)
                            {
                                userDataPtr[stringPosition[6]].DataPointer = (ulong)v6;
                            }

                            if (dataString[7] != null)
                            {
                                userDataPtr[stringPosition[7]].DataPointer = (ulong)v7;
                            }
                        }
                    }

                    status = UnsafeNativeMethods.EventWriteTransfer(_regHandle,
                                                                    in eventDescriptor,
                                                                    (activityId == Guid.Empty) ? null : &activityId,
                                                                    (relatedActivityId == Guid.Empty) ? null : &relatedActivityId,
                                                                    (uint)argCount,
                                                                    userDataPtr);
                }
            }

            if (status != 0)
            {
                SetLastError((int)status);
                return false;
            }

            return true;
        }

        [System.Security.SecurityCritical]
        protected bool WriteTransferEvent(in EventDescriptor eventDescriptor, Guid relatedActivityId, int dataCount, IntPtr data)
        {
            uint status = 0;

            Guid activityId = GetActivityId();

            unsafe
            {
                status = UnsafeNativeMethods.EventWriteTransfer(
                                                _regHandle,
                                                in eventDescriptor,
                                                (activityId == Guid.Empty) ? null : &activityId,
                                                &relatedActivityId,
                                                (uint)dataCount,
                                                (void*)data);
            }

            if (status != 0)
            {
                SetLastError((int)status);
                return false;
            }

            return true;
        }

        [System.Security.SecurityCritical]
        private static Guid GetActivityId()
        {
            return t_activityId;
        }

        [System.Security.SecurityCritical]
        public static void SetActivityId(ref Guid id)
        {
            t_activityId = id;
            UnsafeNativeMethods.EventActivityIdControl((int)ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID, ref id);
        }

        [System.Security.SecurityCritical]
        public static Guid CreateActivityId()
        {
            Guid newId = new();
            UnsafeNativeMethods.EventActivityIdControl((int)ActivityControl.EVENT_ACTIVITY_CTRL_CREATE_ID, ref newId);
            return newId;
        }
    }
}
