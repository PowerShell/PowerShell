// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    internal class WebProxy : System.Net.WebProxy, IEquatable<WebProxy>
    {
        internal WebProxy(Uri? address)
        {
            Address = address;
        }

        public override bool Equals(object? obj) => Equals(obj as WebProxy);

        public override int GetHashCode() => HashCode.Combine(Address, Credentials, BypassProxyOnLocal);

        public bool Equals(WebProxy? other)
        {
            if (other is null)
            {
                return false;
            }
      
            return Address == other.Address
                   && Credentials == other.Credentials
                   && BypassProxyOnLocal == other.BypassProxyOnLocal
                   && UseDefaultCredentials == other.UseDefaultCredentials
                   && (BypassList as IStructuralEquatable).Equals(other.BypassList, EqualityComparer<string>.Default);
        }
    }
}
