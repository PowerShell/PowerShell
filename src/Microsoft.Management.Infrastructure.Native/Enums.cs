using System;

namespace Microsoft.Management.Infrastructure.Native
{
    internal enum MiType:uint
    {
        Boolean,
        UInt8,
        SInt8,
        UInt16,
        SInt16,
        UInt32,
        SInt32,
        UInt64,
        SInt64,
        Real32,
        Real64,
        Char16,
        DateTime,
        String,
        Reference,
        Instance,
        BooleanArray,
        UInt8Array,
        SInt8Array,
        UInt16Array,
        SInt16Array,
        UInt32Array,
        SInt32Array,
        UInt64Array,
        SInt64Array,
        Real32Array,
        Real64Array,
        Char16Array,
        DateTimeArray,
        StringArray,
        ReferenceArray,
        InstanceArray
    }

    internal enum MIWriteMessageChannel
    {
        MIWriteMessageChannelWarning,
        MIWriteMessageChannelVerbose,
        MIWriteMessageChannelDebug
    }

    internal enum MiCallbackMode
    {
        CALLBACK_REPORT,
        CALLBACK_INQUIRE,
        CALLBACK_IGNORE
    }

    internal enum MiCancellationReason
    {
        None,
        Timeout,
        Shutdown,
        ServiceStop
    }

    [Flags]
    internal enum MiFlags : uint
    {
        ABSTRACT = 0x20000,
        ADOPT = 0x80000000,
        ANY = 0x7f,
        ASSOCIATION = 0x10,
        BORROW = 0x40000000,
        CLASS = 1,
        DISABLEOVERRIDE = 0x100,
        ENABLEOVERRIDE = 0x80,
        EXPENSIVE = 0x80000,
        IN = 0x2000,
        INDICATION = 0x20,
        KEY = 0x1000,
        METHOD = 2,
        NOTMODIFIED = 0x2000000,
        NULLFLAG = 0x20000000,
        OUT = 0x4000,
        PARAMETER = 8,
        PROPERTY = 4,
        READONLY = 0x200000,
        REFERENCE = 0x40,
        REQUIRED = 0x8000,
        RESTRICTED = 0x200,
        STATIC = 0x10000,
        STREAM = 0x100000,
        TERMINAL = 0x40000,
        TOSUBCLASS = 0x400,
        TRANSLATABLE = 0x800
    }

    [Flags]
    internal enum MiOperationFlags : uint
    {
        BasicRtti = 2,
        ExpensiveProperties = 0x40,
        FullRtti = 4,
        LocalizedQualifiers = 8,
        ManualAckResults = 1,
        NoRtti = 0x400,
        PolymorphismDeepBasePropsOnly = 0x180,
        PolymorphismShallow = 0x80,
        ReportOperationStarted = 0x200,
        StandardRtti = 0x800
    }

    internal enum MiPromptType
    {
        PROMPTTYPE_NORMAL,
        PROMPTTYPE_CRITICAL
    }

    internal enum MIResponseType
    {
        MIResponseTypeNo,
        MIResponseTypeYes,
        MIResponseTypeNoToAll,
        MIResponseTypeYesToAll
    }

    internal enum MiResult
    {
        ACCESS_DENIED = 2,
        ALREADY_EXISTS = 11,
        CLASS_HAS_CHILDREN = 8,
        CLASS_HAS_INSTANCES = 9,
        CONTINUATION_ON_ERROR_NOT_SUPPORTED = 0x1a,
        FAILED = 1,
        FILTERED_ENUMERATION_NOT_SUPPORTED = 0x19,
        INVALID_CLASS = 5,
        INVALID_ENUMERATION_CONTEXT = 0x15,
        INVALID_NAMESPACE = 3,
        INVALID_OPERATION_TIMEOUT = 0x16,
        INVALID_PARAMETER = 4,
        INVALID_QUERY = 15,
        INVALID_SUPERCLASS = 10,
        METHOD_NOT_AVAILABLE = 0x10,
        METHOD_NOT_FOUND = 0x11,
        NAMESPACE_NOT_EMPTY = 20,
        NO_SUCH_PROPERTY = 12,
        NOT_FOUND = 6,
        NOT_SUPPORTED = 7,
        OK = 0,
        PULL_CANNOT_BE_ABANDONED = 0x18,
        PULL_HAS_BEEN_ABANDONED = 0x17,
        QUERY_LANGUAGE_NOT_SUPPORTED = 14,
        SERVER_IS_SHUTTING_DOWN = 0x1c,
        SERVER_LIMITS_EXCEEDED = 0x1b,
        TYPE_MISMATCH = 13
    }

    internal enum MiSubscriptionDeliveryType
    {
        SubscriptionDeliveryType_Pull = 1,
        SubscriptionDeliveryType_Push = 2
    }
}
