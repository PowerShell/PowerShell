namespace NativeObject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    public static class NativeWindowsMethods
    {
        [DllImport("mi.dll", CallingConvention = MI_PlatformSpecific.MiMainCallConvention)]
        public static extern MI_Result MI_Application_InitializeV1(
            UInt32 flags,
            [MarshalAs(MI_PlatformSpecific.AppropriateStringType)] string applicationID,
            MI_InstanceOutPtr extendedError,
            [In, Out] MI_ApplicationPtr application
            );
    }

    public static class NativeLinuxMethods
    {
           [DllImport("libmi.so", CallingConvention = MI_PlatformSpecific.MiMainCallConvention)]
           public static extern MI_Result MI_Application_InitializeV1(
               UInt32 flags,
               [MarshalAs(MI_PlatformSpecific.AppropriateStringType)] string applicationID,
               MI_InstanceOutPtr extendedError,
               [In, Out] MI_ApplicationPtr application
               );
    }

    public partial class NativeMethods
    {
        public static readonly int IntPtrSize = Marshal.SizeOf(typeof(IntPtr));

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention)]
        public delegate void MI_Session_Close_CompletionCallback(IntPtr callbackContext);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiMainCallConvention)]
        public delegate IntPtr MI_MainFunction(IntPtr callbackContext);

        public delegate void MI_SessionCallbacks_WriteError(MI_Application application, object callbackContext, MI_Instance instance);
        public delegate void MI_SessionCallbacks_WriteMessage(MI_Application application, object callbackContext, MI_WriteMessageChannel channel, string message);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_SessionCallbacks_WriteErrorNative(IntPtr application, IntPtr callbackContext, IntPtr instance);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_SessionCallbacks_WriteMessageNative(IntPtr application, IntPtr callbackContext, MI_WriteMessageChannel channel, IntPtr message);

        public delegate void MI_OperationCallback_PromptUserResult(MI_Operation operation, MI_OperationCallback_ResponseType responseType);
        public delegate void MI_OperationCallback_ResultAcknowledgement(MI_Operation operation);

        public delegate void MI_OperationCallback_PromptUser(MI_Operation operation, object callbackContext, string message, MI_PromptType promptType, MI_OperationCallback_PromptUserResult promptUserResult);
        public delegate void MI_OperationCallback_WriteError(MI_Operation operation, object callbackContext, MI_Instance instance, MI_OperationCallback_PromptUserResult promptUserResult);
        public delegate void MI_OperationCallback_WriteMessage(MI_Operation operation, object callbackContext, MI_WriteMessageChannel channel, string message);
        public delegate void MI_OperationCallback_WriteProgress(MI_Operation operation, object callbackContext, string activity, string currentOperation, string statusDescription, UInt32 percentageComplete, UInt32 secondsRemaining);
        public delegate void MI_OperationCallback_Instance(MI_Operation operation, object callbackContext, MI_Instance instance, bool moreResults, MI_Result resultCode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);
        public delegate void MI_OperationCallback_Indication(MI_Operation operation, object callbackContext, MI_Instance instance, string bookmark, string machineID, bool moreResults, MI_Result resultcode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);
        public delegate void MI_OperationCallback_Class(MI_Operation operation, object callbackContext, MI_Class classResult, bool moreResults, MI_Result resultCode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);
        public delegate void MI_OperationCallback_StreamedParameter(MI_Operation operation, object callbackContext, string parameterName, MI_Type resultType, MI_Value result, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_PromptUserNative(MI_Operation operation, object callbackContext, string message, MI_PromptType promptType, MI_OperationCallback_PromptUserResult promptUserResult);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_WriteErrorNative(MI_Operation operation, object callbackContext, MI_Instance instance, MI_OperationCallback_PromptUserResult promptUserResult);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_WriteMessageNative(MI_Operation operation, object callbackContext, MI_WriteMessageChannel channel, string message);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_WriteProgressNative(MI_Operation operation, object callbackContext, string activity, string currentOperation, string statusDescription, UInt32 percentageComplete, UInt32 secondsRemaining);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_InstanceNative(MI_Operation operation, object callbackContext, MI_Instance instance, bool moreResults, MI_Result resultCode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_IndicationNative(MI_Operation operation, object callbackContext, MI_Instance instance, string bookmark, string machineID, bool moreResults, MI_Result resultcode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_ClassNative(MI_Operation operation, object callbackContext, MI_Class classResult, bool moreResults, MI_Result resultCode, string errorString, MI_Instance errorDetails, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);

        [UnmanagedFunctionPointer(MI_PlatformSpecific.MiCallConvention, CharSet = MI_PlatformSpecific.AppropriateCharSet)]
        public delegate void MI_OperationCallback_StreamedParameterNative(MI_Operation operation, object callbackContext, string parameterName, MI_Type resultType, MI_Value result, MI_OperationCallback_ResultAcknowledgement resultAcknowledgement);

        public static unsafe void memcpy(byte* dst, byte* src, int size, uint count)
        {
            long byteCount = size * count;
            for (long i = 0; i < byteCount; i++)
            {
                *dst++ = *src++;
            }
        }

        public static unsafe void memset(byte* dst, byte val, uint byteCount)
        {
            for (long i = 0; i < byteCount; i++)
            {
                *dst++ = val;
            }
        }
    }
}
