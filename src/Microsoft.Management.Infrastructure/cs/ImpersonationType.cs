/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

namespace Microsoft.Management.Infrastructure.Options
{
    public enum ImpersonationType
    {
        None = Native.DestinationOptionsMethods.MiImpersonationType.None,
        Default = Native.DestinationOptionsMethods.MiImpersonationType.Default,
        Delegate = Native.DestinationOptionsMethods.MiImpersonationType.Delegate,
        Identify = Native.DestinationOptionsMethods.MiImpersonationType.Identify,
        Impersonate = Native.DestinationOptionsMethods.MiImpersonationType.Impersonate,
    };
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class ImpersonationTypeExtensionMethods
    {
        public static Native.DestinationOptionsMethods.MiImpersonationType ToNativeType(this ImpersonationType impersonationType)
        {
            return (Native.DestinationOptionsMethods.MiImpersonationType) impersonationType;
        }
    }
}