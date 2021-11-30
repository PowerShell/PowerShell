// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    internal class WebProxy : IWebProxy
    {
        private ICredentials _credentials;
        private readonly Uri _proxyAddress;

        internal WebProxy(Uri address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            _proxyAddress = address;
        }

        public ICredentials Credentials
        {
            get { return _credentials; }

            set { _credentials = value; }
        }

        internal bool BypassProxyOnLocal
        {
            get; set;
        }

        internal bool UseDefaultCredentials
        {
            get
            {
                return _credentials == CredentialCache.DefaultCredentials;
            }

            set
            {
                _credentials = value ? CredentialCache.DefaultCredentials : null;
            }
        }

        public Uri GetProxy(Uri destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.IsLoopback)
            {
                return destination;
            }

            return _proxyAddress;
        }

        public bool IsBypassed(Uri host)
        {
            return host.IsLoopback;
        }
    }
}
