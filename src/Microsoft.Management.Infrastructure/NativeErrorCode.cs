/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Error codes defined by the native MI Client API
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "NativeErrorCode is a regular enum, not a flags enum.  No idea why FxCop complained about this enum.")]
    public enum NativeErrorCode
    {
        Ok = Native.MiResult.OK,
        Failed = Native.MiResult.FAILED,
        AccessDenied = Native.MiResult.ACCESS_DENIED,
        InvalidNamespace = Native.MiResult.INVALID_NAMESPACE,
        InvalidParameter = Native.MiResult.INVALID_PARAMETER,
        InvalidClass = Native.MiResult.INVALID_CLASS,
        NotFound = Native.MiResult.NOT_FOUND,
        NotSupported = Native.MiResult.NOT_SUPPORTED,
        ClassHasChildren = Native.MiResult.CLASS_HAS_CHILDREN,
        ClassHasInstances = Native.MiResult.CLASS_HAS_INSTANCES,
        InvalidSuperClass = Native.MiResult.INVALID_SUPERCLASS,
        AlreadyExists = Native.MiResult.ALREADY_EXISTS,
        NoSuchProperty = Native.MiResult.NO_SUCH_PROPERTY,
        TypeMismatch = Native.MiResult.TYPE_MISMATCH,
        QueryLanguageNotSupported = Native.MiResult.QUERY_LANGUAGE_NOT_SUPPORTED,
        InvalidQuery = Native.MiResult.INVALID_QUERY,
        MethodNotAvailable = Native.MiResult.METHOD_NOT_AVAILABLE,
        MethodNotFound = Native.MiResult.METHOD_NOT_FOUND,
        NamespaceNotEmpty = Native.MiResult.NAMESPACE_NOT_EMPTY,
        InvalidEnumerationContext = Native.MiResult.INVALID_ENUMERATION_CONTEXT,
        InvalidOperationTimeout = Native.MiResult.INVALID_OPERATION_TIMEOUT,
        PullHasBeenAbandoned = Native.MiResult.PULL_HAS_BEEN_ABANDONED,
        PullCannotBeAbandoned = Native.MiResult.PULL_CANNOT_BE_ABANDONED,
        FilteredEnumerationNotSupported = Native.MiResult.FILTERED_ENUMERATION_NOT_SUPPORTED,
        ContinuationOnErrorNotSupported = Native.MiResult.CONTINUATION_ON_ERROR_NOT_SUPPORTED,
        ServerLimitsExceeded = Native.MiResult.SERVER_LIMITS_EXCEEDED,
        ServerIsShuttingDown = Native.MiResult.SERVER_IS_SHUTTING_DOWN,
    }
}

namespace Microsoft.Management.Infrastructure.Internal
{
    internal static class NativeErrorCodeExtensionMethods
    {
        public static NativeErrorCode ToNativeErrorCode(this Native.MiResult miResult)
        {
            return (NativeErrorCode)miResult;
        }
    }
}
