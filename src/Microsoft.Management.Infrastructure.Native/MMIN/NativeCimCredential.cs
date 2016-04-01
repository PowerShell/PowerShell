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
    internal class NativeCimCredential
    {
        // Methods
        private NativeCimCredential()
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string certificateThumbprint, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string domain, string userName, SecureString password, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
    }
}
