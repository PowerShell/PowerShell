using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class OperationCallbacks
    {
        // Fields
        //private ClassCallbackDelegate<backing_store> ClassCallback;
        //private IndicationResultCallbackDelegate<backing_store> IndicationResultCallback;
        //private InstanceResultCallbackDelegate<backing_store> InstanceResultCallback;
        //private InternalErrorCallbackDelegate<backing_store> InternalErrorCallback;
        //private object <backing_store>ManagedOperationContext;
        //private PromptUserCallbackDelegate<backing_store> PromptUserCallback;
        //private StreamedParameterCallbackDelegate<backing_store> StreamedParameterCallback;
        //private WriteErrorCallbackDelegate<backing_store> WriteErrorCallback;
        //private WriteMessageCallbackDelegate<backing_store> WriteMessageCallback;
        //private WriteProgressCallbackDelegate<backing_store> WriteProgressCallback;
        //private static Action<Action, Func<Exception, bool>, Action<Exception>> userFilteredExceptionHandler;

        // Methods
        static OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        public OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        internal static void InvokeWithUserFilteredExceptionHandler(Action tryBody, Func<Exception, bool> userFilter, Action<Exception> catchBody)
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //internal unsafe bool SetMiOperationCallbacks(_MI_OperationCallbacks* pmiOperationCallbacks, MI_OperationWrapper* pmiOperationWrapper);

        // Properties
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal ClassCallbackDelegate ClassCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal IndicationResultCallbackDelegate IndicationResultCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InstanceResultCallbackDelegate InstanceResultCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InternalErrorCallbackDelegate InternalErrorCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp and in cs/CimOperationOptions.cs")]
        internal object ManagedOperationContext { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal PromptUserCallbackDelegate PromptUserCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal StreamedParameterCallbackDelegate StreamedParameterCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteErrorCallbackDelegate WriteErrorCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteMessageCallbackDelegate WriteMessageCallback { get; set; }
        [SuppressMessage("Micros.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteProgressCallbackDelegate WriteProgressCallback { get; set; }

        // Nested Types
        internal delegate void ClassCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, ClassHandle classHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void IndicationResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, string bookmark, string machineID, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InstanceResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InternalErrorCallbackDelegate(OperationCallbackProcessingContext callbackContextWhereInternalErrorOccurred, Exception exception);

        internal delegate void PromptUserCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string message, MiPromptType promptType, out MIResponseType response);

        internal delegate void StreamedParameterCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string parameterName, object parameterValue, MiType parameterType);

        internal delegate void WriteErrorCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, out MIResponseType response);

        internal delegate void WriteMessageCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, uint channel, string message);

        internal delegate void WriteProgressCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string activity, string currentOperation, string statusDescription, uint percentageComplete, uint secondsRemaining);
    }
}
