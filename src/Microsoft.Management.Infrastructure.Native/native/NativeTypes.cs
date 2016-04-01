using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeObject
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_Interval
    {
        public UInt32 days;
        public UInt32 hours;
        public UInt32 minutes;
        public UInt32 seconds;
        public UInt32 microseconds;
        public UInt32 __padding1;
        public UInt32 __padding2;
        public UInt32 __padding3;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_Timestamp
    {
        public UInt32 year;
        public UInt32 month;
        public UInt32 day;
        public UInt32 hour;
        public UInt32 minute;
        public UInt32 second;
        public UInt32 microseconds;
        public Int32 utc;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_Datetime
    {
        [FieldOffset(0)]
        public bool isTimestamp;

        [FieldOffset(4)]
        public MI_Timestamp timestamp;

        [FieldOffset(4)]
        public MI_Interval interval;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_UsernamePasswordCreds
    {
        public string domain;
        public string username;
        public string password;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public struct MI_UserCredentials
    {
        [FieldOffset(0)]
        public IntPtr authenticationType;

        [FieldOffset(4)]
        public MI_UsernamePasswordCreds usernamePassword;

        [FieldOffset(4)]
        public IntPtr certificateThumbprint;

        public string authenticationTypeString
        {
            get
            {
                return MI_PlatformSpecific.PtrToString(this.authenticationType);
            }
        }
    }

    public class MI_SessionCallbacks
    {
        public NativeMethods.MI_SessionCallbacks_WriteMessage writeMessage;
        public NativeMethods.MI_SessionCallbacks_WriteError writeError;

        public static MI_SessionCallbacks Null
        {
            get
            {
                return null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_SessionCallbacksNative
    {
        IntPtr callbackContext;
        public NativeMethods.MI_SessionCallbacks_WriteMessageNative writeMessage;
        public NativeMethods.MI_SessionCallbacks_WriteErrorNative writeError;
    }

    public class MI_OperationCallbacks
    {
        public NativeMethods.MI_OperationCallback_PromptUser promptUser;
        public NativeMethods.MI_OperationCallback_WriteError writeError;
        public NativeMethods.MI_OperationCallback_WriteMessage writeMessage;
        public NativeMethods.MI_OperationCallback_WriteProgress writeProgress;

        public NativeMethods.MI_OperationCallback_Instance instanceResult;
        public NativeMethods.MI_OperationCallback_Indication indicationResult;
        public NativeMethods.MI_OperationCallback_Class classResult;

        public NativeMethods.MI_OperationCallback_StreamedParameter streamedParameterResult;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
    public class MI_OperationCallbacksNative
    {
        IntPtr callbackContext;

        public NativeMethods.MI_OperationCallback_PromptUserNative promptUser;
        public NativeMethods.MI_OperationCallback_WriteErrorNative writeError;
        public NativeMethods.MI_OperationCallback_WriteMessageNative writeMessage;
        public NativeMethods.MI_OperationCallback_WriteProgressNative writeProgress;

        public NativeMethods.MI_OperationCallback_InstanceNative instanceResult;
        public NativeMethods.MI_OperationCallback_IndicationNative indicationResult;
        public NativeMethods.MI_OperationCallback_ClassNative classResult;

        public NativeMethods.MI_OperationCallback_StreamedParameterNative streamedParameterResult;
    }
}
