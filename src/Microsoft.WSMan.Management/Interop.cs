// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

#pragma warning disable 1591

namespace Microsoft.WSMan.Management
{
    #region "public Api"

    #region WsManEnumFlags
    /// <summary><para>_WSManEnumFlags enumeration.</para></summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManEnumFlags
    {
        /// <summary><para><c>WSManFlagNonXmlText</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 1.</para></summary>
        WSManFlagNonXmlText = 1,

        /// <summary><para><c>WSManFlagReturnObject</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 0.</para></summary>
        WSManFlagReturnObject = 0,

        /// <summary><para><c>WSManFlagReturnEPR</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 2.</para></summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnEPR = 2,

        /// <summary><para><c>WSManFlagReturnObjectAndEPR</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 4.</para></summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnObjectAndEPR = 4,

        /// <summary><para><c>WSManFlagHierarchyDeep</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 0.</para></summary>
        WSManFlagHierarchyDeep = 0,

        /// <summary><para><c>WSManFlagHierarchyShallow</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 32.</para></summary>
        WSManFlagHierarchyShallow = 32,

        /// <summary><para><c>WSManFlagHierarchyDeepBasePropsOnly</c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 64.</para></summary>
        WSManFlagHierarchyDeepBasePropsOnly = 64,

        /// <summary><para><c>WSManFlagAssociationInstance </c> constant of <c>_WSManEnumFlags</c> enumeration.  </para><para>Constant value is 64.</para></summary>
        WSManFlagAssociationInstance = 128
    }

    #endregion WsManEnumFlags

    #region WsManSessionFlags
    /// <summary><para>WSManSessionFlags enumeration.</para></summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManSessionFlags
    {
        /// <summary><para><c>no flag</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 1.</para></summary>
        WSManNone = 0,

        /// <summary><para><c>WSManFlagUTF8</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 1.</para></summary>
        WSManFlagUtf8 = 1,

        /// <summary><para><c>WSManFlagCredUsernamePassword</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 4096.</para></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagCredUserNamePassword = 4096,

        /// <summary><para><c>WSManFlagSkipCACheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 8192.</para></summary>
        WSManFlagSkipCACheck = 8192,

        /// <summary><para><c>WSManFlagSkipCNCheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 16384.</para></summary>
        WSManFlagSkipCNCheck = 16384,

        /// <summary><para><c>WSManFlagUseNoAuthentication</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 32768.</para></summary>
        WSManFlagUseNoAuthentication = 32768,

        /// <summary><para><c>WSManFlagUseDigest</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 65536.</para></summary>
        WSManFlagUseDigest = 65536,

        /// <summary><para><c>WSManFlagUseNegotiate</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 131072.</para></summary>
        WSManFlagUseNegotiate = 131072,

        /// <summary><para><c>WSManFlagUseBasic</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 262144.</para></summary>
        WSManFlagUseBasic = 262144,

        /// <summary><para><c>WSManFlagUseKerberos</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 524288.</para></summary>
        WSManFlagUseKerberos = 524288,

        /// <summary><para><c>WSManFlagNoEncryption</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 1048576.</para></summary>
        WSManFlagNoEncryption = 1048576,

        /// <summary><para><c>WSManFlagEnableSPNServerPort</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 4194304.</para></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Spn")]
        WSManFlagEnableSpnServerPort = 4194304,

        /// <summary><para><c>WSManFlagUTF16</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 8388608.</para></summary>
        WSManFlagUtf16 = 8388608,

        /// <summary><para><c>WSManFlagUseCredSsp</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 16777216.</para></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssp")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagUseCredSsp = 16777216,

        /// <summary><para><c>WSManFlagUseClientCertificate</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 2097152.</para></summary>
        WSManFlagUseClientCertificate = 2097152,

        /// <summary><para><c>WSManFlagSkipRevocationCheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 33554432.</para></summary>
        WSManFlagSkipRevocationCheck = 33554432,

        /// <summary><para><c>WSManFlagAllowNegotiateImplicitCredentials</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 67108864.</para></summary>
        WSManFlagAllowNegotiateImplicitCredentials = 67108864,

        /// <summary><para><c>WSManFlagUseSsl</c> constant of <c>_WSManSessionFlags</c> enumeration.  </para><para>Constant value is 134217728.</para></summary>
        WSManFlagUseSsl = 134217728
    }
    #endregion WsManSessionFlags

    #region AuthenticationMechanism
    /// <summary>WSManEnumFlags enumeration</summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum AuthenticationMechanism
    {
        /// <summary>
        /// Use no authentication.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Use Default authentication.
        /// </summary>
        Default = 0x1,
        /// <summary>
        /// Use digest authentication for a remote operation.
        /// </summary>
        Digest = 0x2,
        /// <summary>
        /// Use negotiate authentication for a remote operation (may use kerberos or ntlm)
        /// </summary>
        Negotiate = 0x4,
        /// <summary>
        /// Use basic authentication for a remote operation.
        /// </summary>
        Basic = 0x8,
        /// <summary>
        /// Use kerberos authentication for a remote operation.
        /// </summary>
        Kerberos = 0x10,
        /// <summary>
        /// Use client certificate authentication for a remote operation.
        /// </summary>
        ClientCertificate = 0x20,
        /// <summary>
        /// Use CredSSP authentication for a remote operation.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Credssp")]
        Credssp = 0x80,
    }

    #endregion AuthenticationMechanism

    #region IWsMan

    /// <summary><para><c>IWSMan</c> interface.</para></summary>
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
    [Guid("190D8637-5CD3-496D-AD24-69636BB5A3B5")]
    [ComImport]
    [TypeLibType((short)4304)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSMan
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        /// <summary><para><c>CreateSession</c> method of <c>IWSMan</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif
        /// <summary><para><c>CreateConnectionOptions</c> method of <c>IWSMan</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue);
        //
        [DispId(2)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateConnectionOptions();

        /// <summary><para><c>CommandLine</c> property of <c>IWSMan</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        //
        string CommandLine
        {
            // IDL: HRESULT CommandLine ([out, retval] BSTR* ReturnValue);

            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>Error</c> property of <c>IWSMan</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;
        //

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWsMan

    #region IWSManConnectionOptions
    /// <summary><para><c>IWSManConnectionOptions</c> interface.</para></summary>
    [Guid("F704E861-9E52-464F-B786-DA5EB2320FDD")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
    public interface IWSManConnectionOptions
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
        /// <summary><para><c>UserName</c> property of <c>IWSManConnectionOptions</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>UserName</c> property was the following:  <c>BSTR UserName</c>;</para></remarks>
        // IDL: BSTR UserName;

        string UserName
        {
            // IDL: HRESULT UserName ([out, retval] BSTR* ReturnValue);

            [DispId(1)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT UserName (BSTR value);

            [DispId(1)]
            set;
        }

        /// <summary><para><c>Password</c> property of <c>IWSManConnectionOptions</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Password</c> property was the following:  <c>BSTR Password</c>;</para></remarks>
        // IDL: BSTR Password;

        [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
        string Password
        {
            // IDL: HRESULT Password (BSTR value);

            [SuppressMessage("Microsoft.Design", "CA1044:PropertiesShouldNotBeWriteOnly")]
            [DispId(2)]
            set;
        }
    }

    /// <summary><para><c>IWSManConnectionOptions</c> interface.</para></summary>
    [Guid("EF43EDF7-2A48-4d93-9526-8BD6AB6D4A6B")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IWSManConnectionOptionsEx : IWSManConnectionOptions
    {
        /// <summary><para><c>CertificateThumbprint</c> property of <c>IWSManConnectionOptionsEx</c> interface.</para></summary>
        string CertificateThumbprint
        {
            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

            [DispId(1)]
            set;
        }
    }

    /// <summary><para><c>IWSManConnectionOptions</c> interface.</para></summary>
    [Guid("F500C9EC-24EE-48ab-B38D-FC9A164C658E")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface IWSManConnectionOptionsEx2 : IWSManConnectionOptionsEx
    {
        /// <summary><para><c>SetProxy</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(4)]
        void SetProxy(int accessType,
            int authenticationMechanism,
            [In, MarshalAs(UnmanagedType.BStr)] string userName,
            [In, MarshalAs(UnmanagedType.BStr)] string password);

        /// <summary><para><c>ProxyIEConfig</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(5)]
        int ProxyIEConfig();

        /// <summary><para><c>ProxyWinHttpConfig</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(6)]
        int ProxyWinHttpConfig();

        /// <summary><para><c>ProxyAutoDetect</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(7)]
        int ProxyAutoDetect();

        /// <summary><para><c>ProxyNoProxyServer</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(8)]
        int ProxyNoProxyServer();

        /// <summary><para><c>ProxyAuthenticationUseNegotiate</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(9)]
        int ProxyAuthenticationUseNegotiate();

        /// <summary><para><c>ProxyAuthenticationUseBasic</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(10)]
        int ProxyAuthenticationUseBasic();

        /// <summary><para><c>ProxyAuthenticationUseDigest</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</para></summary>
        [DispId(11)]
        int ProxyAuthenticationUseDigest();
    }

    #endregion IWSManConnectionOptions

    #region IWSManEnumerator
    /// <summary><para><c>IWSManEnumerator</c> interface.</para></summary>
    [Guid("F3457CA9-ABB9-4FA5-B850-90E8CA300E7F")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSManEnumerator
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        /// <summary><para><c>ReadItem</c> method of <c>IWSManEnumerator</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>ReadItem</c> method was the following:  <c>HRESULT ReadItem ([out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT ReadItem ([out, retval] BSTR* ReturnValue);

        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string ReadItem();

        /// <summary><para><c>AtEndOfStream</c> property of <c>IWSManEnumerator</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>AtEndOfStream</c> property was the following:  <c>BOOL AtEndOfStream</c>;</para></remarks>
        // IDL: BOOL AtEndOfStream;

        bool AtEndOfStream
        {
            // IDL: HRESULT AtEndOfStream ([out, retval] BOOL* ReturnValue);

            [DispId(2)]
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
        }

        /// <summary><para><c>Error</c> property of <c>IWSManEnumerator</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);
            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(8)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWSManEnumerator

    #region IWSManEx
    /// <summary><para><c>IWSManEx</c> interface.</para></summary>
    [Guid("2D53BDAA-798E-49E6-A1AA-74D01256F411")]
    [ComImport]
    [TypeLibType((short)4304)]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    public interface IWSManEx
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        /// <summary><para><c>CreateSession</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif

        /// <summary><para><c>CreateConnectionOptions</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue);

        [DispId(2)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateConnectionOptions();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        string CommandLine
        {
            // IDL: HRESULT CommandLine ([out, retval] BSTR* ReturnValue);

            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>Error</c> property of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>CreateResourceLocator</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CreateResourceLocator</c> method was the following:  <c>HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
        [DispId(5)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateResourceLocator([MarshalAs(UnmanagedType.BStr)] string strResourceLocator);

        /// <summary><para><c>SessionFlagUTF8</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUTF8</c> method was the following:  <c>HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        [DispId(6)]
        int SessionFlagUTF8();

        /// <summary><para><c>SessionFlagCredUsernamePassword</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagCredUsernamePassword</c> method was the following:  <c>HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        [DispId(7)]
        int SessionFlagCredUsernamePassword();

        /// <summary><para><c>SessionFlagSkipCACheck</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagSkipCACheck</c> method was the following:  <c>HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue);

        [DispId(8)]
        int SessionFlagSkipCACheck();

        /// <summary><para><c>SessionFlagSkipCNCheck</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagSkipCNCheck</c> method was the following:  <c>HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue);

        [DispId(9)]
        int SessionFlagSkipCNCheck();

        /// <summary><para><c>SessionFlagUseDigest</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseDigest</c> method was the following:  <c>HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue);

        [DispId(10)]
        int SessionFlagUseDigest();

        /// <summary><para><c>SessionFlagUseNegotiate</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseNegotiate</c> method was the following:  <c>HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue);

        [DispId(11)]
        int SessionFlagUseNegotiate();

        /// <summary><para><c>SessionFlagUseBasic</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseBasic</c> method was the following:  <c>HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue);

        [DispId(12)]
        int SessionFlagUseBasic();

        /// <summary><para><c>SessionFlagUseKerberos</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseKerberos</c> method was the following:  <c>HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue);

        [DispId(13)]
        int SessionFlagUseKerberos();

        /// <summary><para><c>SessionFlagNoEncryption</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagNoEncryption</c> method was the following:  <c>HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue);

        [DispId(14)]
        int SessionFlagNoEncryption();

        /// <summary><para><c>SessionFlagEnableSPNServerPort</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagEnableSPNServerPort</c> method was the following:  <c>HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        [DispId(15)]
        int SessionFlagEnableSPNServerPort();

        /// <summary><para><c>SessionFlagUseNoAuthentication</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>SessionFlagUseNoAuthentication</c> method was the following:  <c>HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue);

        [DispId(16)]
        int SessionFlagUseNoAuthentication();

        /// <summary><para><c>EnumerationFlagNonXmlText</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagNonXmlText</c> method was the following:  <c>HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue);

        [DispId(17)]
        int EnumerationFlagNonXmlText();

        /// <summary><para><c>EnumerationFlagReturnEPR</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(18)]
        int EnumerationFlagReturnEPR();

        /// <summary><para><c>EnumerationFlagReturnObjectAndEPR</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnObjectAndEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(19)]
        int EnumerationFlagReturnObjectAndEPR();

        /// <summary><para><c>GetErrorMessage</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>GetErrorMessage</c> method was the following:  <c>HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue);

        [DispId(20)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetErrorMessage(uint errorNumber);

        /// <summary><para><c>EnumerationFlagHierarchyDeep</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyDeep</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue);

        [DispId(21)]
        int EnumerationFlagHierarchyDeep();

        /// <summary><para><c>EnumerationFlagHierarchyShallow</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyShallow</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue);

        [DispId(22)]
        int EnumerationFlagHierarchyShallow();

        /// <summary><para><c>EnumerationFlagHierarchyDeepBasePropsOnly</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagHierarchyDeepBasePropsOnly</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue);

        [DispId(23)]
        int EnumerationFlagHierarchyDeepBasePropsOnly();

        /// <summary><para><c>EnumerationFlagReturnObject</c> method of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>EnumerationFlagReturnObject</c> method was the following:  <c>HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue);

        [DispId(24)]
        int EnumerationFlagReturnObject();

        /// <summary><para><c>CommandLine</c> property of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        [DispId(28)]
        int EnumerationFlagAssociationInstance();

        /// <summary><para><c>CommandLine</c> property of <c>IWSManEx</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;</para></remarks>
        // IDL: BSTR CommandLine;
        [DispId(29)]
        int EnumerationFlagAssociatedInstance();
    }
    #endregion IWsManEx

    #region IWsManResourceLocator

    /// <summary><para><c>IWSManResourceLocator</c> interface.</para></summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]

    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]

    [Guid("A7A1BA28-DE41-466A-AD0A-C4059EAD7428")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    public interface IWSManResourceLocator
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
        /// <summary><para><c>resourceUri</c> property of <c>IWSManResourceLocator</c> interface.  </para><para>Set the resource URI. Must contain path only -- query string is not allowed here.</para></summary>
        /// <remarks><para>An original IDL definition of <c>resourceUri</c> property was the following:  <c>BSTR resourceUri</c>;</para></remarks>
        // Set the resource URI. Must contain path only -- query string is not allowed here.
        // IDL: BSTR resourceUri;
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        string ResourceUri
        {
            // IDL: HRESULT resourceUri ([out, retval] BSTR* ReturnValue);
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

            // IDL: HRESULT resourceUri (BSTR value);
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            set;
        }

        /// <summary><para><c>AddSelector</c> method of <c>IWSManResourceLocator</c> interface.  </para><para>Add selector to resource locator</para></summary>
        /// <remarks><para>An original IDL definition of <c>AddSelector</c> method was the following:  <c>HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue)</c>;</para></remarks>
        // Add selector to resource locator
        // IDL: HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "sel")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]
        [DispId(2)]
        void AddSelector([MarshalAs(UnmanagedType.BStr)] string resourceSelName, object selValue);

        /// <summary><para><c>ClearSelectors</c> method of <c>IWSManResourceLocator</c> interface.  </para><para>Clear all selectors</para></summary>
        /// <remarks><para>An original IDL definition of <c>ClearSelectors</c> method was the following:  <c>HRESULT ClearSelectors (void)</c>;</para></remarks>
        // Clear all selectors
        // IDL: HRESULT ClearSelectors (void);

        [DispId(3)]
        void ClearSelectors();

        /// <summary><para><c>FragmentPath</c> property of <c>IWSManResourceLocator</c> interface.  </para><para>Gets the fragment path</para></summary>
        /// <remarks><para>An original IDL definition of <c>FragmentPath</c> property was the following:  <c>BSTR FragmentPath</c>;</para></remarks>
        // Gets the fragment path
        // IDL: BSTR FragmentPath;

        string FragmentPath
        {
            // IDL: HRESULT FragmentPath ([out, retval] BSTR* ReturnValue);

            [DispId(4)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT FragmentPath (BSTR value);

            [DispId(4)]
            set;
        }

        /// <summary><para><c>FragmentDialect</c> property of <c>IWSManResourceLocator</c> interface.  </para><para>Gets the Fragment dialect</para></summary>
        /// <remarks><para>An original IDL definition of <c>FragmentDialect</c> property was the following:  <c>BSTR FragmentDialect</c>;</para></remarks>
        // Gets the Fragment dialect
        // IDL: BSTR FragmentDialect;

        string FragmentDialect
        {
            // IDL: HRESULT FragmentDialect ([out, retval] BSTR* ReturnValue);

            [DispId(5)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
            // IDL: HRESULT FragmentDialect (BSTR value);

            [DispId(5)]
            set;
        }

        /// <summary><para><c>AddOption</c> method of <c>IWSManResourceLocator</c> interface.  </para><para>Add option to resource locator</para></summary>
        /// <remarks><para>An original IDL definition of <c>AddOption</c> method was the following:  <c>HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply)</c>;</para></remarks>
        // Add option to resource locator
        // IDL: HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply);
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Option")]
        [DispId(6)]
        void AddOption([MarshalAs(UnmanagedType.BStr)] string OptionName, object OptionValue, int mustComply);

        /// <summary><para><c>MustUnderstandOptions</c> property of <c>IWSManResourceLocator</c> interface.  </para><para>Sets the MustUnderstandOptions value</para></summary>
        /// <remarks><para>An original IDL definition of <c>MustUnderstandOptions</c> property was the following:  <c>long MustUnderstandOptions</c>;</para></remarks>
        // Sets the MustUnderstandOptions value
        // IDL: long MustUnderstandOptions;

        int MustUnderstandOptions
        {
            // IDL: HRESULT MustUnderstandOptions ([out, retval] long* ReturnValue);

            [DispId(7)]
            get;
            // IDL: HRESULT MustUnderstandOptions (long value);

            [DispId(7)]
            set;
        }

        /// <summary><para><c>ClearOptions</c> method of <c>IWSManResourceLocator</c> interface.  </para><para>Clear all options</para></summary>
        /// <remarks><para>An original IDL definition of <c>ClearOptions</c> method was the following:  <c>HRESULT ClearOptions (void)</c>;</para></remarks>
        // Clear all options
        // IDL: HRESULT ClearOptions (void);

        [DispId(8)]
        void ClearOptions();

        /// <summary><para><c>Error</c> property of <c>IWSManResourceLocator</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(9)]
            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
    #endregion IWsManResourceLocator

    #region IWSManSession
    /// <summary><para><c>IWSManSession</c> interface.</para></summary>
    [Guid("FC84FC58-1286-40C4-9DA0-C8EF6EC241E0")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif

    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
    public interface IWSManSession
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif

        /// <summary><para><c>Get</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Get</c> method was the following:  <c>HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Get(object resourceUri, int flags);

        /// <summary><para><c>Put</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Put</c> method was the following:  <c>HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(2)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Put(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        /// <summary><para><c>Create</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Create</c> method was the following:  <c>HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(3)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Create(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        /// <summary><para><c>Delete</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Delete</c> method was the following:  <c>HRESULT Delete (VARIANT resourceUri, [optional, defaultvalue(0)] long flags)</c>;</para></remarks>
        // IDL: HRESULT Delete (VARIANT resourceUri, [optional, defaultvalue(0)] long flags);

        [DispId(4)]
        void Delete(object resourceUri, int flags);

        /// <summary>
        /// </summary>
        /// <param name="actionURI"></param>
        /// <param name="resourceUri"></param>
        /// <param name="parameters"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
        [DispId(5)]
        string Invoke([MarshalAs(UnmanagedType.BStr)] string actionURI, [In] object resourceUri, [MarshalAs(UnmanagedType.BStr)] string parameters, [In] int flags);

        /// <summary><para><c>Enumerate</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Enumerate</c> method was the following:  <c>HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue);

        [DispId(6)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object Enumerate(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string filter, [MarshalAs(UnmanagedType.BStr)] string dialect, int flags);

        /// <summary><para><c>Identify</c> method of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Identify</c> method was the following:  <c>HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;</para></remarks>
        // IDL: HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(7)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Identify(int flags);

        /// <summary><para><c>Error</c> property of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;</para></remarks>
        // IDL: BSTR Error;

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
        string Error
        {
            // IDL: HRESULT Error ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
            [DispId(8)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><para><c>BatchItems</c> property of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>BatchItems</c> property was the following:  <c>long BatchItems</c>;</para></remarks>
        // IDL: long BatchItems;

        int BatchItems
        {
            // IDL: HRESULT BatchItems ([out, retval] long* ReturnValue);

            [DispId(9)]
            get;
            // IDL: HRESULT BatchItems (long value);

            [DispId(9)]
            set;
        }

        /// <summary><para><c>Timeout</c> property of <c>IWSManSession</c> interface.</para></summary>
        /// <remarks><para>An original IDL definition of <c>Timeout</c> property was the following:  <c>long Timeout</c>;</para></remarks>
        // IDL: long Timeout;

        int Timeout
        {
            // IDL: HRESULT Timeout ([out, retval] long* ReturnValue);

            [DispId(10)]
            get;
            // IDL: HRESULT Timeout (long value);

            [DispId(10)]
            set;
        }
    }

    #endregion IWSManSession

    #region IWSManResourceLocatorInternal
    /// <summary><para><c>IWSManResourceLocatorInternal</c> interface.</para></summary>
    [Guid("EFFAEAD7-7EC8-4716-B9BE-F2E7E9FB4ADB")]
    [ComImport]
    [TypeLibType((short)400)]
#if CORECLR
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IWSManResourceLocatorInternal
    {
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetTypeInfo();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetIDsOfNames();

        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Invoke();
#endif
    }

    #endregion IWSManResourceLocatorInternal

    /// <summary><para><c>WSMan</c> interface.</para></summary>
    [Guid("BCED617B-EC03-420b-8508-977DC7A686BD")]
    [ComImport]
#if CORECLR
    [ClassInterface(ClassInterfaceType.None)]
#else
    [ClassInterface(ClassInterfaceType.AutoDual)]
#endif
    public class WSManClass
    {
    }

    #region IGroupPolicyObject

    /// <summary><para><c>GPClass</c> interface.</para></summary>
    [Guid("EA502722-A23D-11d1-A7D3-0000F87571E3")]
    [ComImport]
    [ClassInterface(ClassInterfaceType.None)]
    public class GPClass
    {
    }

    [ComImport, Guid("EA502723-A23D-11d1-A7D3-0000F87571E3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IGroupPolicyObject
    {
        void New(
          [MarshalAs(UnmanagedType.LPWStr)] string pszDomainName,
          [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName,
          uint dwFlags);

        void OpenDSGPO(
          [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
          uint dwFlags);

        void OpenLocalMachineGPO(uint dwFlags);

        void OpenRemoteMachineGPO(
          [MarshalAs(UnmanagedType.LPWStr)] string pszComputerName,
          uint dwFlags);

        void Save(
          [MarshalAs(UnmanagedType.Bool)] bool bMachine,
          [MarshalAs(UnmanagedType.Bool)] bool bAdd,
          [MarshalAs(UnmanagedType.LPStruct)] Guid pGuidExtension,
          [MarshalAs(UnmanagedType.LPStruct)] Guid pGuid);

        void Delete();

        void GetName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        void GetDisplayName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        void SetDisplayName(
          [MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetPath(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        void GetDSPath(
          uint dwSection,
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        void GetFileSysPath(
          uint dwSection,
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath,
          int cchMaxPath);

        IntPtr GetRegistryKey(uint dwSection);

        uint GetOptions();

        void SetOptions(uint dwOptions, uint dwMask);

        void GetMachineName(
          [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
          int cchMaxLength);

        uint GetPropertySheetPages(out IntPtr hPages);
    }

    #endregion IGroupPolicyObject

    /// <summary><para><c>GpoNativeApi</c></para></summary>
    public sealed class GpoNativeApi
    {
        private GpoNativeApi() { }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern System.IntPtr EnterCriticalPolicySection(
             [In, MarshalAs(UnmanagedType.Bool)] bool bMachine);

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LeaveCriticalPolicySection(
             [In] System.IntPtr hSection);
    }
    #endregion
}

#pragma warning restore 1591
