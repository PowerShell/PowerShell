// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;

namespace Microsoft.PowerShell.Commands
{
    internal class WebProxy : IWebProxy
    {
        private ICredentials _credentials;
        private Uri _proxyAddress;

        internal WebProxy(Uri address)
        {
            if (address == null)
            {
                throw new ArgumentNullException("address");
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
                throw new ArgumentNullException("destination");
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
