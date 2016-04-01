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
    internal class AuthType
    {
        // Fields
        internal static string AuthTypeBasic;
        internal static string AuthTypeClientCerts;
        internal static string AuthTypeCredSSP;
        internal static string AuthTypeDefault;
        internal static string AuthTypeDigest;
        internal static string AuthTypeIssuerCert;
        internal static string AuthTypeKerberos;
        internal static string AuthTypeNegoNoCredentials;
        internal static string AuthTypeNegoWithCredentials;
        internal static string AuthTypeNone;
        internal static string AuthTypeNTLM;

        // Methods
        static AuthType()
        {
            throw new NotImplementedException();
        }
        private AuthType()
        {
            throw new NotImplementedException();
        }
    }
}
