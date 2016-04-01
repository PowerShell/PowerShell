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
    internal abstract class ExceptionSafeCallbackBase
    {
        // Fields
        internal OperationCallbackProcessingContext callbackProcessingContext;
        //private OperationCallbacks.InternalErrorCallbackDelegate internalErrorCallback;
        //internal unsafe MI_OperationWrapper* pmiOperationWrapper;

        // Methods
        //protected unsafe ExceptionSafeCallbackBase(void* callbackContext);
        protected abstract void InvokeUserCallback();
        internal void InvokeUserCallbackAndCatchInternalErrors()
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //private bool IsInternalException(Exception e);
        //private void ReportInternalError(Exception exception);
    }

    internal class ExceptionSafeClassCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Class modopt(IsConst)* pmiClass;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeClassCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeIndicationCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* bookmark;
        //private unsafe ushort modopt(IsConst)* errorString;
        //private unsafe ushort modopt(IsConst)* machineID;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeIndicationCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeInstanceResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeInstanceResultCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafePromptUserCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_PromptType promptType;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafePromptUserCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeStreamedParameterResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private _MiType miType;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe _MiValue modopt(IsConst)* pmiParameterValue;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private unsafe ushort modopt(IsConst)* wszParameterName;

        // Methods
        //internal unsafe ExceptionSafeStreamedParameterResultCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteErrorCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Instance* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult;

        // Methods
        //internal unsafe ExceptionSafeWriteErrorCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteMessageCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint channel;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafeWriteMessageCallback(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteProgressCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint percentageComplete;
        //private unsafe _MI_Operation* pmiOperation;
        //private uint secondsRemaining;
        //private unsafe ushort modopt(IsConst)* wszActivity;
        //private unsafe ushort modopt(IsConst)* wszCurrentOperation;
        //private unsafe ushort modopt(IsConst)* wszStatusDescription;

        // Methods
        //internal unsafe ExceptionSafeWriteProgressCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }
}
