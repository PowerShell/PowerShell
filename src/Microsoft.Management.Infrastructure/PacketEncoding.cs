/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Globalization;

namespace Microsoft.Management.Infrastructure.Options
{
    public enum PacketEncoding
    {
        Default,
        Utf8,
        Utf16,
        // TODO/FIXME: make sure that updates in the native API are reflected here - Paul was planning to add options for big/little endian
    };
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class PacketEncodingExtensionMethods
    {
        public static string ToNativeType(this PacketEncoding packetEncoding)
        {
            switch (packetEncoding)
            {
                case PacketEncoding.Default:
                    return Native.DestinationOptionsMethods.packetEncoding_Default;
                
                case PacketEncoding.Utf8:
                    return Native.DestinationOptionsMethods.packetEncoding_UTF8;
                
                case PacketEncoding.Utf16:
                    return Native.DestinationOptionsMethods.packetEncoding_UTF16;

                default:
                    throw new ArgumentOutOfRangeException("packetEncoding");
            }
        }

        public static PacketEncoding FromNativeType(string packetEncoding)
        {
#if(!_CORECLR)
            if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_Default, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_Default, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return PacketEncoding.Default;
            }
#if(!_CORECLR)
            else if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_UTF8, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            else if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_UTF8, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return PacketEncoding.Utf8;
            }
#if(!_CORECLR)
            else if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_UTF16, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
            else if ( String.Compare( packetEncoding, Native.DestinationOptionsMethods.packetEncoding_UTF16, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
            {
                return PacketEncoding.Utf16;
            }
            else
            {
                throw new ArgumentOutOfRangeException("packetEncoding");
            }
        }
    }
}
