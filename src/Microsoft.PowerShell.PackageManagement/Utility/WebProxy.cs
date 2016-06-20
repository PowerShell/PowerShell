using System;
using System.Net;

namespace Microsoft.PowerShell.PackageManagement.Utility
{
    /// <summary>
    /// Used by OneGet cmdlet to supply webproxy to provider
    /// We cannot use System.Net.WebProxy because this is not available on CoreClr
    /// </summary>
    internal class InternalWebProxy : IWebProxy
    {
        Uri _proxyUri;
        ICredentials _credentials;

        public InternalWebProxy(Uri uri, ICredentials credentials)
        {
            Credentials = credentials;
            _proxyUri = uri;
        }

        /// <summary>
        /// Credentials used by WebProxy
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        public Uri GetProxy(Uri destination)
        {
            return _proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
}
