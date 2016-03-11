/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Flags of CIM operations.
    /// </summary>
    /// <seealso cref="CimOperationOptions"/>
    [Flags]
    [SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags", Justification = "This is a direct copy of the native flags enum (which indeed doesn't cover 0x4, 0x1, 0x100")]
    public enum CimOperationFlags : long
    {
        None = 0,

        // Nothing for Native.MiOperationFlags.ManualAckResults - this is covered by the infrastructure

        NoTypeInformation = Native.MiOperationFlags.NoRtti,
        BasicTypeInformation = Native.MiOperationFlags.BasicRtti,
        StandardTypeInformation = Native.MiOperationFlags.StandardRtti,
        FullTypeInformation = Native.MiOperationFlags.FullRtti,
        
        LocalizedQualifiers = Native.MiOperationFlags.LocalizedQualifiers,
        
        ExpensiveProperties = Native.MiOperationFlags.ExpensiveProperties,
        
        PolymorphismShallow = Native.MiOperationFlags.PolymorphismShallow,
        PolymorphismDeepBasePropsOnly = Native.MiOperationFlags.PolymorphismDeepBasePropsOnly,

        ReportOperationStarted = Native.MiOperationFlags.ReportOperationStarted,
    };

    /// <summary>
    /// Password Authentication mechanisms.
    /// </summary>
    /// <seealso cref="CimOperationOptions"/>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct representation of the native flags (which doesn't have None)")]         
    public enum PasswordAuthenticationMechanism: int
    {
        Default = 0,
        Digest = 1,
        Negotiate = 2,
        Basic = 3,
        Kerberos = 4,
        NtlmDomain = 5,
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "CredSsp is direct representation of the native flag")]              
        CredSsp = 6,
    };
    /// <summary>
    /// Certificate Authentication mechanisms.
    /// </summary>
    /// <seealso cref="CimOperationOptions"/>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct representation of the native flags (which doesn't have None)")]    
    public enum CertificateAuthenticationMechanism: int
    {
        Default = 0,
        ClientCertificate = 1,
        IssuerCertificate = 2,
    };    
    /// <summary>
    /// Impersonated Authentication mechanisms of CIM session.
    /// </summary>
    /// <seealso cref="CimOperationOptions"/>
    public enum ImpersonatedAuthenticationMechanism: int
    {
        None = 0,
        Negotiate = 1,
        Kerberos = 2,
        NtlmDomain = 3,
    };        
}

namespace Microsoft.Management.Infrastructure.Options.Internal
{
    internal static class OperationFlagsExtensionMethods
    {
        public static Native.MiOperationFlags ToNative(this CimOperationFlags operationFlags)
        {
            return (Native.MiOperationFlags)operationFlags;
        }
    }
}