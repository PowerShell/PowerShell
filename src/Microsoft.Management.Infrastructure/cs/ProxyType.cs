/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Globalization;

namespace Microsoft.Management.Infrastructure.Options
{
    public enum ProxyType
    {
        None,
        WinHttp,
        Auto,
        InternetExplorer,
    };
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class ProxyTypeExtensionMethods
    {
        public static string ToNativeType(this ProxyType proxyType)
        {
            switch (proxyType)
            {
                case ProxyType.None:
                    return Native.DestinationOptionsMethods.proxyType_None;
                
                case ProxyType.WinHttp:
                    return Native.DestinationOptionsMethods.proxyType_WinHTTP;
                
                case ProxyType.Auto:
                    return Native.DestinationOptionsMethods.proxyType_Auto;

                case ProxyType.InternetExplorer:
                    return Native.DestinationOptionsMethods.proxyType_IE;

                default:
                    throw new ArgumentOutOfRangeException("proxyType");
            }
        }

        public static ProxyType FromNativeType(string proxyType)
        {
#if(!_CORECLR)
            if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_None, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_None, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return ProxyType.None;
            }
#if(!_CORECLR)
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_WinHTTP, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_WinHTTP, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return ProxyType.WinHttp;
            }
#if(!_CORECLR)
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_Auto, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_Auto, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return ProxyType.Auto;
            }
#if(!_CORECLR)
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_IE, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            else if ( String.Compare( proxyType, Native.DestinationOptionsMethods.proxyType_IE, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return ProxyType.InternetExplorer;
            }
            else
            {
                throw new ArgumentOutOfRangeException("proxyType");
            }
        }
    }
}