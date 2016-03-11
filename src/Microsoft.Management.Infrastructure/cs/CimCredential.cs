/* Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Security;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Management.Infrastructure.Native;
using Microsoft.Management.Infrastructure.Internal;
using Microsoft.Management.Infrastructure.Internal.Operations;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Represents CimCredential.
    /// </summary>
    public class CimCredential
    {
        private NativeCimCredentialHandle credential;
    
        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(string authenticationMechanism, string certificateThumbprint)
        {
            if( authenticationMechanism == null)
            {
                throw new ArgumentNullException("authenticationMechanism");
            }               
            NativeCimCredential.CreateCimCredential(authenticationMechanism, certificateThumbprint, out credential);
        }

        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(string authenticationMechanism, string domain, string userName, SecureString password)
        {
            if( authenticationMechanism == null)
            {
                throw new ArgumentNullException("authenticationMechanism");
            }             
            if( userName == null)
            {
                throw new ArgumentNullException("userName");
            }            
            NativeCimCredential.CreateCimCredential(authenticationMechanism, domain, userName, password, out credential);
        }

        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(string authenticationMechanism)
        {
            if( authenticationMechanism == null)
            {
                throw new ArgumentNullException("authenticationMechanism");
            }             
            NativeCimCredential.CreateCimCredential(authenticationMechanism, out credential);        
        }    

        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(CertificateAuthenticationMechanism authenticationMechanism, string certificateThumbprint)
        {
            string strAuthenticationMechanism = null;
            if( authenticationMechanism == CertificateAuthenticationMechanism.Default)
            {
                strAuthenticationMechanism = AuthType.AuthTypeClientCerts;
            }
            else if( authenticationMechanism == CertificateAuthenticationMechanism.ClientCertificate)
            {
                strAuthenticationMechanism = AuthType.AuthTypeClientCerts;
            }
            else if( authenticationMechanism == CertificateAuthenticationMechanism.IssuerCertificate )
            {
                strAuthenticationMechanism = AuthType.AuthTypeIssuerCert;
            }
            else
            {
                throw new ArgumentOutOfRangeException("authenticationMechanism");
            }
            NativeCimCredential.CreateCimCredential(strAuthenticationMechanism, certificateThumbprint, out credential);        
        }

        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(PasswordAuthenticationMechanism authenticationMechanism, string domain, string userName, SecureString password)
        {
            if( userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            string strAuthenticationMechanism = null;
            if( authenticationMechanism == PasswordAuthenticationMechanism.Default)
            {
                strAuthenticationMechanism = AuthType.AuthTypeDefault;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.Basic)
            {
                strAuthenticationMechanism = AuthType.AuthTypeBasic;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.Digest)
            {
                strAuthenticationMechanism = AuthType.AuthTypeDigest;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.Negotiate)
            {
                strAuthenticationMechanism = AuthType.AuthTypeNegoWithCredentials;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.Kerberos)
            {
                strAuthenticationMechanism = AuthType.AuthTypeKerberos;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.NtlmDomain)
            {
                strAuthenticationMechanism = AuthType.AuthTypeNTLM;
            }
            else if( authenticationMechanism == PasswordAuthenticationMechanism.CredSsp)
            {
                strAuthenticationMechanism = AuthType.AuthTypeCredSSP;
            }  
            else
            {
                throw new ArgumentOutOfRangeException("authenticationMechanism");
            }            
            NativeCimCredential.CreateCimCredential(strAuthenticationMechanism, domain, userName, password, out credential);                
        }

        /// <summary>
        /// Creates a new Credentials
        /// </summary>
        public CimCredential(ImpersonatedAuthenticationMechanism authenticationMechanism)
        {
            string strAuthenticationMechanism = null;
            if( authenticationMechanism == ImpersonatedAuthenticationMechanism.None)
            {
                strAuthenticationMechanism = AuthType.AuthTypeNone;
            }
            else if( authenticationMechanism == ImpersonatedAuthenticationMechanism.Negotiate)
            {
                strAuthenticationMechanism = AuthType.AuthTypeNegoNoCredentials;
            }
            else if( authenticationMechanism == ImpersonatedAuthenticationMechanism.Kerberos)
            {
                strAuthenticationMechanism = AuthType.AuthTypeKerberos;
            }
            else if( authenticationMechanism == ImpersonatedAuthenticationMechanism.NtlmDomain)
            {
                strAuthenticationMechanism = AuthType.AuthTypeNTLM;
            }
            else
            {
                throw new ArgumentOutOfRangeException("authenticationMechanism");
            }            
            NativeCimCredential.CreateCimCredential(strAuthenticationMechanism, out credential);
        }  
        
        internal NativeCimCredentialHandle GetCredential(){ return credential; }
    }
}
