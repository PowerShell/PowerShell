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
    internal class OperationCallbacksDefinitions
    {
        // Methods
        public OperationCallbacksDefinitions()
        {
            throw new NotImplementedException();
        }
        //internal static unsafe void ClassAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void IndicationAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void InstanceResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void PromptUserAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        //internal static unsafe void StreamedParameterResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void WriteErrorAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        //internal static unsafe void WriteMessageAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        //internal static unsafe void WriteProgressAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);

        // Nested Types
        //internal unsafe delegate void ClassAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void IndicationAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void InstanceResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void PromptUserAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);

        //internal unsafe delegate void StreamedParameterResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void WriteErrorAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);

        //internal unsafe delegate void WriteMessageAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);

        //internal unsafe delegate void WriteProgressAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
    }
}
