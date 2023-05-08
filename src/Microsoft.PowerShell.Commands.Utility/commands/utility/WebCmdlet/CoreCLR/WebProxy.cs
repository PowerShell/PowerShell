// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;

namespace Microsoft.PowerShell.Commands
{
    internal class WebProxy : System.Net.WebProxy, IEquatable<WebProxy>
    {
        internal WebProxy(Uri? address, bool BypassOnLocal)
        {
            Address = address;
            BypassProxyOnLocal = BypassOnLocal;
        }

        public override bool Equals(object? obj) => Equals(obj as WebProxy);

        public override int GetHashCode() => HashCode.Combine(Address, Credentials, BypassProxyOnLocal, UseDefaultCredentials, BypassArrayList);

        public bool Equals(WebProxy? other)
        {
            if (other is null)
            {
                return false;
            }
      
            return GetHashCode() == other.GetHashCode();
        }
    }
}
