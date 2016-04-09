/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Cim flags
    /// </summary>
    [Flags]
    public enum CimFlags : long
    {
        None = 0,

        Class = Native.MiFlags.CLASS,
        Method = Native.MiFlags.METHOD,
        Property = Native.MiFlags.PROPERTY,
        Parameter = Native.MiFlags.PARAMETER,
        Association = Native.MiFlags.ASSOCIATION,
        Indication = Native.MiFlags.INDICATION,
        Reference = Native.MiFlags.REFERENCE,
        Any = Native.MiFlags.ANY,

        /* Qualifier flavors */
        EnableOverride = Native.MiFlags.ENABLEOVERRIDE,
        DisableOverride = Native.MiFlags.DISABLEOVERRIDE,
        Restricted = Native.MiFlags.RESTRICTED,
        ToSubclass = Native.MiFlags.TOSUBCLASS,
        Translatable = Native.MiFlags.TRANSLATABLE,

        /* Select boolean qualifier */
        Key = Native.MiFlags.KEY,
        In = Native.MiFlags.IN,
        Out = Native.MiFlags.OUT,
        Required = Native.MiFlags.REQUIRED,
        Static = Native.MiFlags.STATIC,
        Abstract = Native.MiFlags.ABSTRACT,
        Terminal = Native.MiFlags.TERMINAL,
        Expensive = Native.MiFlags.EXPENSIVE,
        Stream = Native.MiFlags.STREAM,
        ReadOnly = Native.MiFlags.READONLY,

        /* Special flags */
        NotModified = Native.MiFlags.NOTMODIFIED,
        NullValue = Native.MiFlags.NULLFLAG,
        Borrow = Native.MiFlags.BORROW,
        Adopt = Native.MiFlags.ADOPT,
    };

    /// <summary>
    /// CimSubscriptionDeliveryType
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct copy of the native flags enum")]
    public enum CimSubscriptionDeliveryType : int
    {
        None = Native.MiSubscriptionDeliveryType.SubscriptionDeliveryType_Push,
        Push = Native.MiSubscriptionDeliveryType.SubscriptionDeliveryType_Push,
        Pull = Native.MiSubscriptionDeliveryType.SubscriptionDeliveryType_Pull,
    }
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class CimFlagsExtensionMethods
    {
        public static Native.MiFlags ToMiFlags(this CimFlags cimFlags)
        {
            return (Native.MiFlags)cimFlags;
        }
    }

    internal static class MiFlagsExtensionMethods
    {
        public static CimFlags ToCimFlags(this Native.MiFlags miFlags)
        {
            return (CimFlags) miFlags;
        }
    }
}