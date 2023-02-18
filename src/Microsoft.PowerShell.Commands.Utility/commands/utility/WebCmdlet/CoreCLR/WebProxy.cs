// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    internal class WebProxy : IWebProxy, IEquatable<WebProxy>
    {
        private ICredentials _credentials;
        private readonly Uri _proxyAddress;

        internal WebProxy(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address);

            _proxyAddress = address;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as WebProxy);
        }

        public override int GetHashCode()
        {
            return this._proxyAddress.GetHashCode() * 17 + this._credentials.GetHashCode();
        }

        public bool Equals(WebProxy other)
        {
            if (other is null)
            {
                return false;
            }
            // _proxyAddress cannot be null as it is set in the constructor            
            return other._credentials == _credentials
                && _proxyAddress.Equals(other._proxyAddress)
                && BypassProxyOnLocal == other.BypassProxyOnLocal;

        }

        public ICredentials Credentials
        {
            get => _credentials;

            set => _credentials = value;
        }

        internal bool BypassProxyOnLocal { get; set; }

        internal bool UseDefaultCredentials
        {
            get => _credentials == CredentialCache.DefaultCredentials;

            set => _credentials = value ? CredentialCache.DefaultCredentials : null;
        }

        public Uri GetProxy(Uri destination)
        {
            ArgumentNullException.ThrowIfNull(destination);

            return destination.IsLoopback ? destination : _proxyAddress;
        }

        public bool IsBypassed(Uri host) => host.IsLoopback;

    }
}
