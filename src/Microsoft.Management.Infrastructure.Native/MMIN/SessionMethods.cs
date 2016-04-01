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
    internal class SessionMethods
    {
        // Methods
        private SessionMethods()
        {
            throw new NotImplementedException();
        }
        internal static void AssociatorInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string assocClass, string resultClass, string sourceRole, string resultRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void DeleteInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateClasses(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool classNamesOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetClass(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Invoke(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, string methodName, InstanceHandle instanceHandleForTargetOfInvocation, InstanceHandle instanceHandleForMethodParameters, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ModifyInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Micros.Usage", "CA1801:ReviewUnusedParameters", MessageId = "keysOnly")]
        internal static void QueryInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ReferenceInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string associationClassName, string sourceRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Subscribe(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void TestConnection(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
    }
}
