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
    internal class DestinationOptionsMethods
    {
        // Fields
        internal static string packetEncoding_Default;
        internal static string packetEncoding_UTF16;
        internal static string packetEncoding_UTF8;
        internal static string proxyType_Auto;
        internal static string proxyType_IE;
        internal static string proxyType_None;
        internal static string proxyType_WinHTTP;
        internal static string transport_Http;
        internal static string transport_Https;

        // Methods
        static DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        private DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddDestinationCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddProxyCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(DestinationOptionsHandle destinationOptionsHandle, out DestinationOptionsHandle newDestinationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDataLocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, out uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, out bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, out string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, out MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, out uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, out string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, out bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, out bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetProxyType(DestinationOptionsHandle destinationOptionsHandle, out string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(DestinationOptionsHandle destinationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTransport(DestinationOptionsHandle destinationOptionsHandle, out string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUILocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, string optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDataLocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetProxyType(DestinationOptionsHandle destinationOptionsHandle, string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(DestinationOptionsHandle destinationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTransport(DestinationOptionsHandle destinationOptionsHandle, string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUILocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }

        // Nested Types
        internal enum MiImpersonationType
        {
            Default,
            None,
            Identify,
            Impersonate,
            Delegate
        }
    }
}
