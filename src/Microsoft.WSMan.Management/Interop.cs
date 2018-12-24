// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#pragma warning disable 1591

namespace Microsoft.WSMan.Management
{

    #region "public Api"

    #region WsManEnumFlags
    /// <summary><param>_WSManEnumFlags enumeration.</param></summary>

    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManEnumFlags
    {
        /// <summary><param><c>WSManFlagNonXmlText</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 1.</param></summary>
        WSManFlagNonXmlText = 1,

        /// <summary><param><c>WSManFlagReturnObject</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 0.</param></summary>
        WSManFlagReturnObject = 0,

        /// <summary><param><c>WSManFlagReturnEPR</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 2.</param></summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnEPR = 2,

        /// <summary><param><c>WSManFlagReturnObjectAndEPR</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 4.</param></summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        WSManFlagReturnObjectAndEPR = 4,

        /// <summary><param><c>WSManFlagHierarchyDeep</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 0.</param></summary>
        WSManFlagHierarchyDeep = 0,

        /// <summary><param><c>WSManFlagHierarchyShallow</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 32.</param></summary>
        WSManFlagHierarchyShallow = 32,

        /// <summary><param><c>WSManFlagHierarchyDeepBasePropsOnly</c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 64.</param></summary>
        WSManFlagHierarchyDeepBasePropsOnly = 64,

        /// <summary><param><c>WSManFlagAssociationInstance </c> constant of <c>_WSManEnumFlags</c> enumeration.  .</param><param>Constant value is 64.</param></summary>
        WSManFlagAssociationInstance = 128
    }

    #endregion WsManEnumFlags

    #region WsManSessionFlags
    /// <summary><param>WSManSessionFlags enumeration.</param></summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [TypeLibType((short)0)]
    public enum WSManSessionFlags
    {
        /// <summary><param><c>no flag</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 1.</param></summary>
        WSManNone = 0,

        /// <summary><param><c>WSManFlagUTF8</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 1.</param></summary>
        WSManFlagUtf8 = 1,

        /// <summary><param><c>WSManFlagCredUsernamePassword</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 4096.</param></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagCredUserNamePassword = 4096,

        /// <summary><param><c>WSManFlagSkipCACheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 8192.</param></summary>
        WSManFlagSkipCACheck = 8192,

        /// <summary><param><c>WSManFlagSkipCNCheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 16384.</param></summary>
        WSManFlagSkipCNCheck = 16384,

        /// <summary><param><c>WSManFlagUseNoAuthentication</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 32768.</param></summary>
        WSManFlagUseNoAuthentication = 32768,

        /// <summary><param><c>WSManFlagUseDigest</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 65536.</param></summary>
        WSManFlagUseDigest = 65536,

        /// <summary><param><c>WSManFlagUseNegotiate</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 131072.</param></summary>
        WSManFlagUseNegotiate = 131072,

        /// <summary><param><c>WSManFlagUseBasic</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 262144.</param></summary>
        WSManFlagUseBasic = 262144,

        /// <summary><param><c>WSManFlagUseKerberos</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 524288.</param></summary>
        WSManFlagUseKerberos = 524288,

        /// <summary><param><c>WSManFlagNoEncryption</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 1048576.</param></summary>
        WSManFlagNoEncryption = 1048576,

        /// <summary><param><c>WSManFlagEnableSPNServerPort</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 4194304.</param></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Spn")]
        WSManFlagEnableSpnServerPort = 4194304,

        /// <summary><param><c>WSManFlagUTF16</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 8388608.</param></summary>
        WSManFlagUtf16 = 8388608,

        /// <summary><param><c>WSManFlagUseCredSsp</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 16777216.</param></summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ssp")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        WSManFlagUseCredSsp = 16777216,

        /// <summary><param><c>WSManFlagUseClientCertificate</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 2097152.</param></summary>
        WSManFlagUseClientCertificate = 2097152,

        /// <summary><param><c>WSManFlagSkipRevocationCheck</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 33554432.</param></summary>
        WSManFlagSkipRevocationCheck = 33554432,

        /// <summary><param><c>WSManFlagAllowNegotiateImplicitCredentials</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 67108864.</param></summary>
        WSManFlagAllowNegotiateImplicitCredentials = 67108864,

        /// <summary><param><c>WSManFlagUseSsl</c> constant of <c>_WSManSessionFlags</c> enumeration.  .</param><param>Constant value is 134217728.</param></summary>
        WSManFlagUseSsl = 134217728
    }
    #endregion WsManSessionFlags

    #region AuthenticationMechanism
    /// <summary>WSManEnumFlags enumeration</summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum AuthenticationMechanism
    {
        /// <summary>
        /// Use no authentication
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Use Default authentication
        /// </summary>
        Default = 0x1,
        /// <summary>
        /// Use digest authentication for a remote operation
        /// </summary>
        Digest = 0x2,
        /// <summary>
        /// Use negotiate authentication for a remote operation (may use kerberos or ntlm)
        /// </summary>
        Negotiate = 0x4,
        /// <summary>
        /// Use basic authentication for a remote operation
        /// </summary>
        Basic = 0x8,
        /// <summary>
        /// Use kerberos authentication for a remote operation
        /// </summary>
        Kerberos = 0x10,
        /// <summary>
        /// Use client certificate authentication for a remote operation
        /// </summary>
        ClientCertificate = 0x20,
        /// <summary>
        /// Use CredSSP authentication for a remote operation
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Credssp")]
        Credssp = 0x80,
   }

    #endregion AuthenticationMechanism

    #region IWsMan

    /// <summary><param><c>IWSMan</c> interface.</param></summary>
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Error")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
    [Guid("190D8637-5CD3-496D-AD24-69636BB5A3B5")]
    [ComImport]
    [TypeLibType((short)4304)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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

        /// <summary><param><c>CreateSession</c> method of <c>IWSMan</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif
        /// <summary><param><c>CreateConnectionOptions</c> method of <c>IWSMan</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue);
        //
        [DispId(2)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateConnectionOptions();

        /// <summary><param><c>CommandLine</c> property of <c>IWSMan</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;.</param></remarks>
        // IDL: BSTR CommandLine;
        //
        string CommandLine
        {
            // IDL: HRESULT CommandLine ([out, retval] BSTR* ReturnValue);

            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }

        /// <summary><param><c>Error</c> property of <c>IWSMan</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;.</param></remarks>
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
    /// <summary><param><c>IWSManConnectionOptions</c> interface.</param></summary>
    [Guid("F704E861-9E52-464F-B786-DA5EB2320FDD")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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
        /// <summary><param><c>UserName</c> property of <c>IWSManConnectionOptions</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>UserName</c> property was the following:  <c>BSTR UserName</c>;.</param></remarks>
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

        /// <summary><param><c>Password</c> property of <c>IWSManConnectionOptions</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Password</c> property was the following:  <c>BSTR Password</c>;.</param></remarks>
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

    /// <summary><param><c>IWSManConnectionOptions</c> interface.</param></summary>
    [Guid("EF43EDF7-2A48-4d93-9526-8BD6AB6D4A6B")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IWSManConnectionOptionsEx : IWSManConnectionOptions
    {
        /// <summary><param><c>CertificateThumbprint</c> property of <c>IWSManConnectionOptionsEx</c> interface.</param></summary>
        string CertificateThumbprint
        {
            [DispId(3)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

            [DispId(1)]
            set;
        }

    }

    /// <summary><param><c>IWSManConnectionOptions</c> interface.</param></summary>
    [Guid("F500C9EC-24EE-48ab-B38D-FC9A164C658E")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    public interface IWSManConnectionOptionsEx2 : IWSManConnectionOptionsEx
    {
        /// <summary><param><c>SetProxy</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(4)]
        void SetProxy(int accessType,
            int authenticationMechanism,
            [In, MarshalAs(UnmanagedType.BStr)] string userName,
            [In, MarshalAs(UnmanagedType.BStr)] string password);

        /// <summary><param><c>ProxyIEConfig</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(5)]
        int ProxyIEConfig();

        /// <summary><param><c>ProxyWinHttpConfig</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(6)]
        int ProxyWinHttpConfig();

        /// <summary><param><c>ProxyAutoDetect</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(7)]
        int ProxyAutoDetect();

        /// <summary><param><c>ProxyNoProxyServer</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(8)]
        int ProxyNoProxyServer();

        /// <summary><param><c>ProxyAuthenticationUseNegotiate</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(9)]
        int ProxyAuthenticationUseNegotiate();

        /// <summary><param><c>ProxyAuthenticationUseBasic</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(10)]
        int ProxyAuthenticationUseBasic();

        /// <summary><param><c>ProxyAuthenticationUseDigest</c> method of <c>IWSManConnectionOptionsEx2</c> interface.</param></summary>
        [DispId(11)]
        int ProxyAuthenticationUseDigest();
    };

    #endregion IWSManConnectionOptions

    #region IWSManEnumerator
    /// <summary><param><c>IWSManEnumerator</c> interface.</param></summary>
    [Guid("F3457CA9-ABB9-4FA5-B850-90E8CA300E7F")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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

        /// <summary><param><c>ReadItem</c> method of <c>IWSManEnumerator</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>ReadItem</c> method was the following:  <c>HRESULT ReadItem ([out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT ReadItem ([out, retval] BSTR* ReturnValue);

        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string ReadItem();

        /// <summary><param><c>AtEndOfStream</c> property of <c>IWSManEnumerator</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>AtEndOfStream</c> property was the following:  <c>BOOL AtEndOfStream</c>;.</param></remarks>
        // IDL: BOOL AtEndOfStream;

        bool AtEndOfStream
        {
            // IDL: HRESULT AtEndOfStream ([out, retval] BOOL* ReturnValue);

            [DispId(2)]
            [return: MarshalAs(UnmanagedType.Bool)]
            get;
        }

        /// <summary><param><c>Error</c> property of <c>IWSManEnumerator</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;.</param></remarks>
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
    /// <summary><param><c>IWSManEx</c> interface.</param></summary>
    [Guid("2D53BDAA-798E-49E6-A1AA-74D01256F411")]
    [ComImport]
    [TypeLibType((short)4304)]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
#else
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIDispatch)]
#endif
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Username")]
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

        /// <summary><param><c>CreateSession</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CreateSession</c> method was the following:  <c>HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT CreateSession ([optional, defaultvalue(string.Empty)] BSTR connection, [optional, defaultvalue(0)] long flags, [optional] IDispatch* connectionOptions, [out, retval] IDispatch** ReturnValue);

        [DispId(1)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IUnknown)] object connectionOptions);
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object CreateSession([MarshalAs(UnmanagedType.BStr)] string connection, int flags, [MarshalAs(UnmanagedType.IDispatch)] object connectionOptions);
#endif

        /// <summary><param><c>CreateConnectionOptions</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CreateConnectionOptions</c> method was the following:  <c>HRESULT CreateConnectionOptions ([out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
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

        /// <summary><param><c>Error</c> property of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;.</param></remarks>
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

        /// <summary><param><c>CreateResourceLocator</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CreateResourceLocator</c> method was the following:  <c>HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT CreateResourceLocator ([optional, defaultvalue(string.Empty)] BSTR strResourceLocator, [out, retval] IDispatch** ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "str")]
        [DispId(5)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object CreateResourceLocator([MarshalAs(UnmanagedType.BStr)] string strResourceLocator);

        /// <summary><param><c>SessionFlagUTF8</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUTF8</c> method was the following:  <c>HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUTF8 ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "UTF")]
        [DispId(6)]
        int SessionFlagUTF8();

        /// <summary><param><c>SessionFlagCredUsernamePassword</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagCredUsernamePassword</c> method was the following:  <c>HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagCredUsernamePassword ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Username")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
        [DispId(7)]
        int SessionFlagCredUsernamePassword();

        /// <summary><param><c>SessionFlagSkipCACheck</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagSkipCACheck</c> method was the following:  <c>HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagSkipCACheck ([out, retval] long* ReturnValue);

        [DispId(8)]
        int SessionFlagSkipCACheck();

        /// <summary><param><c>SessionFlagSkipCNCheck</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagSkipCNCheck</c> method was the following:  <c>HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagSkipCNCheck ([out, retval] long* ReturnValue);

        [DispId(9)]
        int SessionFlagSkipCNCheck();

        /// <summary><param><c>SessionFlagUseDigest</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUseDigest</c> method was the following:  <c>HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUseDigest ([out, retval] long* ReturnValue);

        [DispId(10)]
        int SessionFlagUseDigest();

        /// <summary><param><c>SessionFlagUseNegotiate</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUseNegotiate</c> method was the following:  <c>HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUseNegotiate ([out, retval] long* ReturnValue);

        [DispId(11)]
        int SessionFlagUseNegotiate();

        /// <summary><param><c>SessionFlagUseBasic</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUseBasic</c> method was the following:  <c>HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUseBasic ([out, retval] long* ReturnValue);

        [DispId(12)]
        int SessionFlagUseBasic();

        /// <summary><param><c>SessionFlagUseKerberos</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUseKerberos</c> method was the following:  <c>HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUseKerberos ([out, retval] long* ReturnValue);

        [DispId(13)]
        int SessionFlagUseKerberos();

        /// <summary><param><c>SessionFlagNoEncryption</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagNoEncryption</c> method was the following:  <c>HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagNoEncryption ([out, retval] long* ReturnValue);

        [DispId(14)]
        int SessionFlagNoEncryption();

        /// <summary><param><c>SessionFlagEnableSPNServerPort</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagEnableSPNServerPort</c> method was the following:  <c>HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagEnableSPNServerPort ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        [DispId(15)]
        int SessionFlagEnableSPNServerPort();

        /// <summary><param><c>SessionFlagUseNoAuthentication</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>SessionFlagUseNoAuthentication</c> method was the following:  <c>HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT SessionFlagUseNoAuthentication ([out, retval] long* ReturnValue);

        [DispId(16)]
        int SessionFlagUseNoAuthentication();

        /// <summary><param><c>EnumerationFlagNonXmlText</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagNonXmlText</c> method was the following:  <c>HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagNonXmlText ([out, retval] long* ReturnValue);

        [DispId(17)]
        int EnumerationFlagNonXmlText();

        /// <summary><param><c>EnumerationFlagReturnEPR</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagReturnEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagReturnEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(18)]
        int EnumerationFlagReturnEPR();

        /// <summary><param><c>EnumerationFlagReturnObjectAndEPR</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagReturnObjectAndEPR</c> method was the following:  <c>HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagReturnObjectAndEPR ([out, retval] long* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EPR")]
        [DispId(19)]
        int EnumerationFlagReturnObjectAndEPR();

        /// <summary><param><c>GetErrorMessage</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>GetErrorMessage</c> method was the following:  <c>HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT GetErrorMessage (unsigned long errorNumber, [out, retval] BSTR* ReturnValue);

        [DispId(20)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string GetErrorMessage(uint errorNumber);

        /// <summary><param><c>EnumerationFlagHierarchyDeep</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagHierarchyDeep</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeep ([out, retval] long* ReturnValue);

        [DispId(21)]
        int EnumerationFlagHierarchyDeep();

        /// <summary><param><c>EnumerationFlagHierarchyShallow</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagHierarchyShallow</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyShallow ([out, retval] long* ReturnValue);

        [DispId(22)]
        int EnumerationFlagHierarchyShallow();

        /// <summary><param><c>EnumerationFlagHierarchyDeepBasePropsOnly</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagHierarchyDeepBasePropsOnly</c> method was the following:  <c>HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagHierarchyDeepBasePropsOnly ([out, retval] long* ReturnValue);

        [DispId(23)]
        int EnumerationFlagHierarchyDeepBasePropsOnly();

        /// <summary><param><c>EnumerationFlagReturnObject</c> method of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>EnumerationFlagReturnObject</c> method was the following:  <c>HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT EnumerationFlagReturnObject ([out, retval] long* ReturnValue);

        [DispId(24)]
        int EnumerationFlagReturnObject();

        /// <summary><param><c>CommandLine</c> property of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;.</param></remarks>
        // IDL: BSTR CommandLine;
        [DispId(28)]
        int EnumerationFlagAssociationInstance();

        /// <summary><param><c>CommandLine</c> property of <c>IWSManEx</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>CommandLine</c> property was the following:  <c>BSTR CommandLine</c>;.</param></remarks>
        // IDL: BSTR CommandLine;
        [DispId(29)]
        int EnumerationFlagAssociatedInstance();
    }
#endregion IWsManEx

#region IWsManResourceLocator

    /// <summary><param><c>IWSManResourceLocator</c> interface.</param></summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]

    [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]

    [Guid("A7A1BA28-DE41-466A-AD0A-C4059EAD7428")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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
        /// <summary><param><c>resourceUri</c> property of <c>IWSManResourceLocator</c> interface.  .</param><param>Set the resource URI. Must contain path only -- query string is not allowed here.</param></summary>
        /// <remarks><param>An original IDL definition of <c>resourceUri</c> property was the following:  <c>BSTR resourceUri</c>;.</param></remarks>
        // Set the resource URI. Must contain path only -- query string is not allowed here.
        // IDL: BSTR resourceUri;
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        string ResourceUri
        {

            // IDL: HRESULT resourceUri (BSTR value);
            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            set;

            // IDL: HRESULT resourceUri ([out, retval] BSTR* ReturnValue);

            [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
            [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
            [DispId(1)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;

        }

        /// <summary><param><c>AddSelector</c> method of <c>IWSManResourceLocator</c> interface.  </param><param>Add selector to resource locator.</param></summary>
        /// <remarks><param>An original IDL definition of <c>AddSelector</c> method was the following:  <c>HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue)</c>;.</param></remarks>
        // Add selector to resource locator
        // IDL: HRESULT AddSelector (BSTR resourceSelName, VARIANT selValue);

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "resource")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "sel")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sel")]
        [DispId(2)]
        void AddSelector([MarshalAs(UnmanagedType.BStr)] string resourceSelName, object selValue);

        /// <summary><param><c>ClearSelectors</c> method of <c>IWSManResourceLocator</c> interface.  </param><param>Clear all selectors.</param></summary>
        /// <remarks><param>An original IDL definition of <c>ClearSelectors</c> method was the following:  <c>HRESULT ClearSelectors (void)</c>;.</param></remarks>
        // Clear all selectors
        // IDL: HRESULT ClearSelectors (void);

        [DispId(3)]
        void ClearSelectors();

        /// <summary><param><c>FragmentPath</c> property of <c>IWSManResourceLocator</c> interface.  </param><param>Gets the fragment path.</param></summary>
        /// <remarks><param>An original IDL definition of <c>FragmentPath</c> property was the following:  <c>BSTR FragmentPath</c>;.</param></remarks>
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

        /// <summary><param><c>FragmentDialect</c> property of <c>IWSManResourceLocator</c> interface.  </param><param>Gets the Fragment dialect.</param></summary>
        /// <remarks><param>An original IDL definition of <c>FragmentDialect</c> property was the following:  <c>BSTR FragmentDialect</c>;.</param></remarks>
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

        /// <summary><param><c>AddOption</c> method of <c>IWSManResourceLocator</c> interface.  </param><param>Add option to resource locator.</param></summary>
        /// <remarks><param>An original IDL definition of <c>AddOption</c> method was the following:  <c>HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply)</c>;.</param></remarks>
        // Add option to resource locator
        // IDL: HRESULT AddOption (BSTR OptionName, VARIANT OptionValue, [optional, defaultvalue(0)] long mustComply);
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Option")]
        [DispId(6)]
        void AddOption([MarshalAs(UnmanagedType.BStr)] string OptionName, object OptionValue, int mustComply);

        /// <summary><param><c>MustUnderstandOptions</c> property of <c>IWSManResourceLocator</c> interface.  </param><param>Sets the MustUnderstandOptions value.</param></summary>
        /// <remarks><param>An original IDL definition of <c>MustUnderstandOptions</c> property was the following:  <c>long MustUnderstandOptions</c>;.</param></remarks>
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

        /// <summary><param><c>ClearOptions</c> method of <c>IWSManResourceLocator</c> interface.  </param><param>Clear all options.</param></summary>
        /// <remarks><param>An original IDL definition of <c>ClearOptions</c> method was the following:  <c>HRESULT ClearOptions (void)</c>;.</param></remarks>
        // Clear all options
        // IDL: HRESULT ClearOptions (void);

        [DispId(8)]
        void ClearOptions();

        /// <summary><param><c>Error</c> property of <c>IWSManResourceLocator</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;.</param></remarks>
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
    /// <summary><param><c>IWSManSession</c> interface.</param></summary>
    [Guid("FC84FC58-1286-40C4-9DA0-C8EF6EC241E0")]
    [ComImport]
    [TypeLibType((short)4288)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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

        /// <summary><param><c>Get</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Get</c> method was the following:  <c>HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT Get (VARIANT resourceUri, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get")]
        [DispId(1)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Get(object resourceUri, int flags);

        /// <summary><param><c>Put</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Put</c> method was the following:  <c>HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT Put (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(2)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Put(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        /// <summary><param><c>Create</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Create</c> method was the following:  <c>HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT Create (VARIANT resourceUri, BSTR resource, [optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(3)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Create(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string resource, int flags);

        /// <summary><param><c>Delete</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Delete</c> method was the following:  <c>HRESULT Delete (VARIANT resourceUri, [optional, defaultvalue(0)] long flags)</c>;.</param></remarks>
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
        String Invoke([MarshalAs(UnmanagedType.BStr)] string actionURI, [In] object resourceUri, [MarshalAs(UnmanagedType.BStr)] string parameters, [In] int flags);

        /// <summary><param><c>Enumerate</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Enumerate</c> method was the following:  <c>HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT Enumerate (VARIANT resourceUri, [optional, defaultvalue(string.Empty)] BSTR filter, [optional, defaultvalue(string.Empty)] BSTR dialect, [optional, defaultvalue(0)] long flags, [out, retval] IDispatch** ReturnValue);

        [DispId(6)]
#if CORECLR
        [return: MarshalAs(UnmanagedType.IUnknown)]
#else
        [return: MarshalAs(UnmanagedType.IDispatch)]
#endif
        object Enumerate(object resourceUri, [MarshalAs(UnmanagedType.BStr)] string filter, [MarshalAs(UnmanagedType.BStr)] string dialect, int flags);

        /// <summary><param><c>Identify</c> method of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Identify</c> method was the following:  <c>HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue)</c>;.</param></remarks>
        // IDL: HRESULT Identify ([optional, defaultvalue(0)] long flags, [out, retval] BSTR* ReturnValue);

        [DispId(7)]
        [return: MarshalAs(UnmanagedType.BStr)]
        string Identify(int flags);

        /// <summary><param><c>Error</c> property of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Error</c> property was the following:  <c>BSTR Error</c>;.</param></remarks>
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

        /// <summary><param><c>BatchItems</c> property of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>BatchItems</c> property was the following:  <c>long BatchItems</c>;.</param></remarks>
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

        /// <summary><param><c>Timeout</c> property of <c>IWSManSession</c> interface.</param></summary>
        /// <remarks><param>An original IDL definition of <c>Timeout</c> property was the following:  <c>long Timeout</c>;.</param></remarks>
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
    /// <summary><param><c>IWSManResourceLocatorInternal</c> interface.</param></summary>
    [Guid("EFFAEAD7-7EC8-4716-B9BE-F2E7E9FB4ADB")]
    [ComImport]
    [TypeLibType((short)400)]
#if CORECLR
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
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

    /// <summary><param><c>WSMan</c> interface.</param></summary>
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

    /// <summary><param><c>GPClass</c> interface.</param></summary>
    [Guid("EA502722-A23D-11d1-A7D3-0000F87571E3")]
    [ComImport]
    [ClassInterface(ClassInterfaceType.None)]
    public class GPClass
    {
    }

    [ComImport, Guid("EA502723-A23D-11d1-A7D3-0000F87571E3"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IGroupPolicyObject
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

    /// <summary><param><c>GpoNativeApi</c></param></summary>
    public sealed class GpoNativeApi
    {
        private GpoNativeApi() { }

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern System.IntPtr EnterCriticalPolicySection(
             [In, MarshalAs(UnmanagedType.Bool)] bool bMachine);

        [DllImport("Userenv.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool LeaveCriticalPolicySection(
             [In] System.IntPtr hSection);
    }
#endregion

}

#pragma warning restore 1591
