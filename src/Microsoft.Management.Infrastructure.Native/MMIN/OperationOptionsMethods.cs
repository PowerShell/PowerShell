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
    internal class OperationOptionsMethods
    {
        // Methods
        private OperationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(OperationOptionsHandle operationOptionsHandle, out OperationOptionsHandle newOperationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUri(OperationOptionsHandle operationOptionsHandle, out string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, out string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(OperationOptionsHandle operationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUseMachineID(OperationOptionsHandle operationOptionsHandle, out bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(OperationOptionsHandle operationOptionsHandle, string optionName, object optionValue, MiType miType, [MarshalAs(UnmanagedType.U1)] bool mustComply)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDisableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEnableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, string optionValue)
        {
            //TODO: Implement
            return MiResult.OK;
            //throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserRegularMode(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode, [MarshalAs(UnmanagedType.U1)] bool ackValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUri(OperationOptionsHandle operationOptionsHandle, string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(OperationOptionsHandle operationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUseMachineID(OperationOptionsHandle operationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
    }
}
