/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Options.Internal;
using System.Globalization;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Options of <see cref="CimSession"/> that uses WSMan as the transport protocol
    /// </summary>
    public class WSManSessionOptions : CimSessionOptions
    {
        /// <summary>
        /// Creates a new <see cref="WSManSessionOptions"/> instance
        /// </summary>
        public WSManSessionOptions()
            : base(Native.ApplicationMethods.protocol_WSMan)
        {
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="optionsToClone"/>
        /// </summary>
        /// <param name="optionsToClone">options to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsToClone"/> is <c>null</c></exception>
        public WSManSessionOptions(WSManSessionOptions optionsToClone)
            : base(optionsToClone)
        {
        }

        // REVIEW PLEASE: native API uses MI_uint32 for the port number.  should we limit that to UInt16 in the managed layer?

        /// <summary>
        /// Sets destination port
        /// </summary>
        /// <value></value>
        public uint DestinationPort
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetDestinationPort(
                    this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                uint port;
                Native.MiResult result = Native.DestinationOptionsMethods.GetDestinationPort(
                    this.DestinationOptionsHandleOnDemand, out port);
                CimException.ThrowIfMiResultFailure(result);
                return port;
            }
        }

        /// <summary>
        /// Sets maximum size of SOAP envelope
        /// </summary>
        /// <value></value>
        public uint MaxEnvelopeSize
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetMaxEnvelopeSize(
                    this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                uint size;
                Native.MiResult result = Native.DestinationOptionsMethods.GetMaxEnvelopeSize(
                    this.DestinationOptionsHandleOnDemand, out size);
                CimException.ThrowIfMiResultFailure(result);
                return size;
            }
        }

        /// <summary>
        /// Sets whether the client should validate that the server certificate is signed by a trusted certificate authority (CA).
        /// </summary>
        /// <value></value>
        public bool CertCACheck
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetCertCACheck(this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool check;
                Native.MiResult result = Native.DestinationOptionsMethods.GetCertCACheck(this.DestinationOptionsHandleOnDemand, out check);
                CimException.ThrowIfMiResultFailure(result);
                return check;
            }
        }

        /// <summary>
        /// Sets whether the client should validate that the certificate common name (CN) of the server matches the hostname of the server.
        /// </summary>
        /// <value></value>
        public bool CertCNCheck
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetCertCNCheck(this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool check;
                Native.MiResult result = Native.DestinationOptionsMethods.GetCertCNCheck(this.DestinationOptionsHandleOnDemand, out check);
                CimException.ThrowIfMiResultFailure(result);
                return check;
            }
        }

        /// <summary>
        /// Sets whether the client should validate the revocation status of the server certificate.
        /// </summary>
        /// <value></value>
        public bool CertRevocationCheck
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetCertRevocationCheck(
                    this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool check;
                Native.MiResult result = Native.DestinationOptionsMethods.GetCertRevocationCheck(
                    this.DestinationOptionsHandleOnDemand, out check);
                CimException.ThrowIfMiResultFailure(result);
                return check;
            }
        }

        /// <summary>
        /// Sets whether the client should use SSL.
        /// </summary>
        /// <value></value>
        public bool UseSsl
        {
            set
            {
                this.AssertNotDisposed();

                if (value)
                {
                    Native.MiResult result = Native.DestinationOptionsMethods.SetTransport(
                        this.DestinationOptionsHandleOnDemand,
                        Native.DestinationOptionsMethods.transport_Https);
                    CimException.ThrowIfMiResultFailure(result);
                }
                else
                {
                    Native.MiResult result = Native.DestinationOptionsMethods.SetTransport(
                        this.DestinationOptionsHandleOnDemand,
                        Native.DestinationOptionsMethods.transport_Http);
                    CimException.ThrowIfMiResultFailure(result);
                }
            }
            get
            {
                this.AssertNotDisposed();
                string transport;
                Native.MiResult result = Native.DestinationOptionsMethods.GetTransport(
                        this.DestinationOptionsHandleOnDemand,
                        out transport);
                CimException.ThrowIfMiResultFailure(result);
#if(!_CORECLR)
                if ( string.Compare( transport, Native.DestinationOptionsMethods.transport_Https, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ) == 0 )
#else
                if ( string.Compare( transport, Native.DestinationOptionsMethods.transport_Https, StringComparison.CurrentCultureIgnoreCase ) == 0 )
#endif
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Sets type of HTTP proxy.
        /// </summary>
        /// <value></value>
        public ProxyType ProxyType
        {
            set
            {
                this.AssertNotDisposed();

                string nativeProxyType = value.ToNativeType();
                Native.MiResult result = Native.DestinationOptionsMethods.SetProxyType(
                    this.DestinationOptionsHandleOnDemand, nativeProxyType);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                string type;
                Native.MiResult result = Native.DestinationOptionsMethods.GetProxyType(
                    this.DestinationOptionsHandleOnDemand, out type);
                CimException.ThrowIfMiResultFailure(result);
                return ProxyTypeExtensionMethods.FromNativeType(type);
            }
        }

        /// <summary>
        /// Sets packet encoding.
        /// </summary>
        /// <value></value>
        public PacketEncoding PacketEncoding
        {
            set
            {
                this.AssertNotDisposed();

                string nativePacketEncoding = value.ToNativeType();
                Native.MiResult result = Native.DestinationOptionsMethods.SetPacketEncoding(
                    this.DestinationOptionsHandleOnDemand, nativePacketEncoding);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();

                string nativePacketEncoding;
                Native.MiResult result = Native.DestinationOptionsMethods.GetPacketEncoding(
                    this.DestinationOptionsHandleOnDemand, out nativePacketEncoding);
                CimException.ThrowIfMiResultFailure(result);
                return PacketEncodingExtensionMethods.FromNativeType(nativePacketEncoding);
            }
        }

        /// <summary>
        /// Sets packet privacy
        /// </summary>
        /// <value></value>
        public bool NoEncryption
        {
            set
            {
                this.AssertNotDisposed();

                bool noEncryption = value;
                bool packetPrivacy = !noEncryption;
                Native.MiResult result = Native.DestinationOptionsMethods.SetPacketPrivacy(this.DestinationOptionsHandleOnDemand, packetPrivacy);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool packetPrivacy;
                Native.MiResult result = Native.DestinationOptionsMethods.GetPacketPrivacy(this.DestinationOptionsHandleOnDemand, out packetPrivacy);
                CimException.ThrowIfMiResultFailure(result);
                bool noEncryption = !packetPrivacy;
                return noEncryption;
            }
        }

        /// <summary>
        /// Sets whether to encode port in service principal name (SPN).
        /// </summary>
        /// <value></value>
        public bool EncodePortInServicePrincipalName
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetEncodePortInSPN(
                    this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool encodePortInServicePrincipalName;
                Native.MiResult result = Native.DestinationOptionsMethods.GetEncodePortInSPN(
                    this.DestinationOptionsHandleOnDemand, out encodePortInServicePrincipalName);
                CimException.ThrowIfMiResultFailure(result);
                return encodePortInServicePrincipalName;
            }
        }

        /// <summary>
        /// Sets http url prefix.
        /// </summary>
        /// <value></value>
        public Uri HttpUrlPrefix
        {
            set
            {
                if (value == null) // empty string ok
                {
                    throw new ArgumentNullException("value");
                }
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetHttpUrlPrefix(this.DestinationOptionsHandleOnDemand, value.ToString());
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                string httpUrlPrefix;
                Native.MiResult result = Native.DestinationOptionsMethods.GetHttpUrlPrefix(this.DestinationOptionsHandleOnDemand, out httpUrlPrefix);
                if (result != Native.MiResult.OK)
                {
                    return null;
                }
                try
                {
                    try
                    {
                        return new Uri(httpUrlPrefix, UriKind.Relative);
                    }
#if(!_CORECLR)
                    catch (UriFormatException)
#else
                    catch (FormatException)
#endif
                    {
                        return new Uri(httpUrlPrefix, UriKind.Absolute);
                    }
                }
#if(!_CORECLR)
                catch (UriFormatException)
#else
                catch (FormatException)
#endif
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Sets a Proxy Credential
        /// </summary>
        /// <param name="credential"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="credential"/> is <c>null</c></exception>
        public void AddProxyCredentials(CimCredential credential)
        {
            if (credential == null)
            {
                throw new ArgumentNullException("credential");
            }
            this.AssertNotDisposed();

            Native.MiResult result = Native.DestinationOptionsMethods.AddProxyCredentials(this.DestinationOptionsHandleOnDemand, credential.GetCredential());
            CimException.ThrowIfMiResultFailure(result);
        }
    }
}
