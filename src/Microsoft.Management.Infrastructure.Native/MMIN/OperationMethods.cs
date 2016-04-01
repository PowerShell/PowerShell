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
    internal class OperationMethods
    {
        // Methods
        private OperationMethods()
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult Cancel(OperationHandle operationHandle, MiCancellationReason cancellationReason)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetClass(OperationHandle operationHandle, out ClassHandle classHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetIndication(OperationHandle operationHandle, out InstanceHandle instanceHandle, out string bookmark, out string machineID, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetInstance(OperationHandle operationHandle, out InstanceHandle instanceHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
    }
}
