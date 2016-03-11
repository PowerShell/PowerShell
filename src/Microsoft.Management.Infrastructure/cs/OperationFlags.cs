/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure
{
    [Flags]
    [SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags", Justification = "This is a direct copy of the native flags enum (which indeed doesn't cover 0x4, 0x1, 0x100")]
    public enum OperationFlags : long
    {
        None = 0,

        // Nothing for Native.MiOperationFlags.ManualAckResults - this is covered by the infrastructure

        BasicTypeInformation = Native.MiOperationFlags.BasicRtti,
        FullTypeInformation = Native.MiOperationFlags.FullRtti,

        LocalizedQualifiers = Native.MiOperationFlags.LocalizedQualifiers,

        ExpensiveProperties = Native.MiOperationFlags.ExpensiveProperties,

        PolymorphismShallow = Native.MiOperationFlags.PolymorphismShallow,
        PolymorphismDeepBasePropsOnly = Native.MiOperationFlags.PolymorphismDeepBasePropsOnly,
    };

    internal static class OperationFlagsExtensionMethods
    {
        public static Native.MiOperationFlags ToNative(this OperationFlags operationFlags)
        {
            return (Native.MiOperationFlags)operationFlags;
        }
    }
}
