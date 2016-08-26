/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using Runspaces = System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation;
using Security = System.Management.Automation.Security;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Runtime.InteropServices;
using System.Management.Automation.Provider;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;
using System.Security;
using DWORD = System.UInt32;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the Certificate Provider dynamic parameters.
    /// 
    /// We only support one dynamic parameter for Win 7 and earlier:
    /// CodeSigningCert
    /// If provided, we only return certificates valid for signing code or
    /// scripts.
    /// </summary>    

    internal sealed class CertificateProviderCodeSigningDynamicParameters
    {
        /// <summary>
        /// switch that controls whether we only return 
        /// code signing certs.
        /// </summary>
        [Parameter()]
        public SwitchParameter CodeSigningCert
        {
            get { return _codeSigningCert; }
            set { _codeSigningCert = value; }
        }

        private SwitchParameter _codeSigningCert = new SwitchParameter();
    }

    /// <summary>
    /// Defines the type of DNS string
    /// The structure contains punycode name and unicode name
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct DnsNameRepresentation
    {
        /// <summary>
        /// punycode version of DNS name
        /// </summary>   
        private string _punycodeName;

        /// <summary>
        /// Unicode version of DNS name
        /// </summary>   
        private string _unicodeName;

        /// <summary>
        /// ambiguous constructor of a DnsNameRepresentation
        /// </summary>   
        public DnsNameRepresentation(string inputDnsName)
        {
            _punycodeName = inputDnsName;
            _unicodeName = inputDnsName;
        }

        /// <summary>
        /// specific constructor of a DnsNameRepresentation
        /// </summary>   
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Punycode")]
        public DnsNameRepresentation(
            string inputPunycodeName,
            string inputUnicodeName)
        {
            _punycodeName = inputPunycodeName;
            _unicodeName = inputUnicodeName;
        }

        /// <summary>
        /// value comparison
        /// </summary>   
        public bool Equals(DnsNameRepresentation dnsName)
        {
            bool match = false;

            if (_unicodeName != null && dnsName._unicodeName != null)
            {
                if (String.Equals(
                            _unicodeName,
                            dnsName._unicodeName,
                            StringComparison.OrdinalIgnoreCase))
                {
                    match = true;
                }
            }
            else if (_unicodeName == null && dnsName._unicodeName == null)
            {
                match = true;
            }
            return match;
        }

        /// <summary>
        /// get property of Punycode
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Punycode")]
        public string Punycode
        {
            get
            {
                return _punycodeName;
            }
        }

        /// <summary>
        /// get property of Unicode
        /// </summary>
        public string Unicode
        {
            get
            {
                return _unicodeName;
            }
        }

        /// <summary>
        /// get display string
        /// </summary>
        public override string ToString()
        {
            // Use case sensitive comparison here.
            // We don't ever expect to see the punycode and unicode strings
            // to differ only by upper/lower case.  If they do, that's really
            // a code bug, and the effect is to just display both strings.

            return String.Equals(_punycodeName, _unicodeName) ?
                        _punycodeName :
                        _unicodeName + " (" + _punycodeName + ")";
        }
    }

    /// <summary>
    /// Defines the Certificate Provider dynamic parameters.
    /// 
    /// We only support one dynamic parameter for Win 7 and earlier:
    /// CodeSigningCert
    /// If provided, we only return certificates valid for signing code or
    /// scripts.
    ///
    /// For Win 8 and later, we also support:
    /// SSLServerAuthentication
    /// If provided, only return certificates valid for server authentication.
    ///
    /// DnsName
    /// If provided, only return certificates matching the supplied DNS Name.
    ///
    /// Eku
    /// If provided, only return certificates containing all of the OIDs
    /// supplied.
    ///
    /// ExpiringInDays
    /// If provided, only return certificates expiring within the specified
    /// number of days.
    ///
    /// </summary>    

    internal sealed class CertificateProviderDynamicParameters
    {
        /// <summary>
        /// switch that controls whether we only return 
        /// code signing certs.
        /// </summary>
        [Parameter()]
        public SwitchParameter CodeSigningCert
        {
            get { return _codeSigningCert; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _codeSigningCert = value; }
        }
        private SwitchParameter _codeSigningCert = new SwitchParameter();

        /// <summary>
        /// switch that controls whether we only return 
        /// data encipherment certs.
        /// </summary>
        [Parameter()]
        public SwitchParameter DocumentEncryptionCert
        {
            get { return _documentEncryptionCert; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _documentEncryptionCert = value; }
        }
        private SwitchParameter _documentEncryptionCert = new SwitchParameter();

        /// <summary>
        /// switch that controls whether we only return 
        /// code signing certs.
        /// </summary>
        [Parameter()]
        public SwitchParameter SSLServerAuthentication
        {
            get { return _sslServerAuthentication; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _sslServerAuthentication = value; }
        }

        private SwitchParameter _sslServerAuthentication = new SwitchParameter();

        /// <summary>
        /// string to filter certs by DNSName
        /// Expected content is a single DNS Name that may start and/or end
        /// with '*': "contoso.com" or "*toso.c*"
        /// </summary>
        [Parameter()]
        public DnsNameRepresentation DnsName
        {
            get { return _dnsName; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _dnsName = value; }
        }

        private DnsNameRepresentation _dnsName;

        /// <summary>
        /// string to filter certs by EKU
        /// Expected content is one or more OID strings:
        /// "1.3.6.1.5.5.7.3.1", etc.
        /// For a cert to match, it must be valid for all listed OIDs.
        /// </summary>
        [Parameter()]
        public string[] Eku
        {
            get { return _eku; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _eku = value; }
        }

        private string[] _eku = null;

        /// <summary>
        /// string to filter certs by the number of valid days
        /// Expected content is a non-negative integer.
        /// "0" matches all certs that have already expired.
        /// "1" matches all certs that are currently valid and will expire
        /// by midnight tonight (local time).
        /// </summary>
        [Parameter()]
        public int ExpiringInDays
        {
            get { return _expiringInDays; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method is used by command line processing")]
            set
            { _expiringInDays = value; }
        }

        private int _expiringInDays = -1;
    }

    /// <summary>
    /// Defines the Certificate Provider remove-item dynamic parameters.
    /// 
    /// Currently, we only support one dynamic parameter: DeleteKey
    /// If provided, we will delete the private key when we remove a certificate
    /// 
    /// </summary>    
    internal sealed class ProviderRemoveItemDynamicParameters
    {
        /// <summary>
        /// switch that controls whether we should delete private key
        /// when remove a certificate
        /// </summary>
        [Parameter()]
        public SwitchParameter DeleteKey
        {
            get
            {
                {
                    return _deleteKey;
                }
            }

            set
            {
                {
                    _deleteKey = value;
                }
            }
        }

        private SwitchParameter _deleteKey = new SwitchParameter();
    }

    /// <summary>
    /// Defines the safe handle class for native cert store handles,
    /// HCERTSTORE.
    /// </summary>
    internal sealed class CertificateStoreHandle : SafeHandle
    {
        public CertificateStoreHandle() : base(IntPtr.Zero, true)
        {
            return;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            bool fResult = false;

            if (IntPtr.Zero != handle)
            {
                fResult = Security.NativeMethods.CertCloseStore(handle, 0);
                handle = IntPtr.Zero;
            }
            return fResult;
        }

        public IntPtr Handle
        {
            get { return handle; }
            set { handle = value; }
        }
    }

    /// <summary>
    /// Defines the safe handle class for native cert store handles,
    /// HCERTSTORE.
    /// </summary>
    internal sealed class CertificateFilterHandle : SafeHandle
    {
        public CertificateFilterHandle() : base(IntPtr.Zero, true)
        {
            return;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            bool fResult = false;

            if (IntPtr.Zero != handle)
            {
                Security.NativeMethods.CCFindCertificateFreeFilter(handle);
                handle = IntPtr.Zero;
                fResult = true;
            }
            return fResult;
        }

        public IntPtr Handle
        {
            get { return handle; }
            set { handle = value; }
        }
    }

    /// <summary>
    /// Defines the Certificate Provider store handle class
    /// </summary>
    internal sealed class X509NativeStore
    {
        //#region tracer

        /// <summary>
        /// Initializes a new instance of the X509NativeStore class.
        /// </summary>
        public X509NativeStore(X509StoreLocation StoreLocation, string StoreName)
        {
            _storeLocation = StoreLocation;
            _storeName = StoreName;
        }

        public void Open(bool includeArchivedCerts)
        {
            if (_storeHandle != null && _archivedCerts != includeArchivedCerts)
            {
                _storeHandle = null;        // release the old handle
            }
            if (_storeHandle == null)
            {
                _valid = false;
                _open = false;

                Security.NativeMethods.CertOpenStoreFlags StoreFlags =
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_SHARE_STORE_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_SHARE_CONTEXT_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG;

                if (includeArchivedCerts)
                {
                    StoreFlags |= Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG;
                }

                switch (_storeLocation.Location)
                {
                    case StoreLocation.LocalMachine:
                        StoreFlags |= Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
                        break;

                    case StoreLocation.CurrentUser:
                        StoreFlags |= Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
                        break;

                    default:
                        //ThrowItemNotFound(storeLocation.ToString(), CertificateProviderItem.StoreLocation);
                        break;
                }

                IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                _storeName);
                if (IntPtr.Zero == hCertStore)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                _storeHandle = new CertificateStoreHandle();
                _storeHandle.Handle = hCertStore;

                //we only do CertControlStore for stores other than UserDS
                if (!String.Equals(
                                _storeName,
                                "UserDS",
                                StringComparison.OrdinalIgnoreCase))
                {
                    if (!Security.NativeMethods.CertControlStore(
                                _storeHandle.Handle,
                                0,
                                Security.NativeMethods.CertControlStoreType.CERT_STORE_CTRL_AUTO_RESYNC,
                                IntPtr.Zero))
                    {
                        _storeHandle = null;
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                }

                _valid = true;
                _open = true;
                _archivedCerts = includeArchivedCerts;
            }
        }

        public IntPtr GetFirstCert(
            CertificateFilterInfo filter)
        {
            _filterHandle = null;
            if (DownLevelHelper.NativeFilteringSupported() && filter != null)
            {
                IntPtr hFilter = IntPtr.Zero;

                _filterHandle = new CertificateFilterHandle();
                int hr = Security.NativeMethods.CCFindCertificateBuildFilter(
                                                    filter.FilterString,
                                                    ref hFilter);
                if (hr != Security.NativeConstants.S_OK)
                {
                    _filterHandle = null;
                    throw new System.ComponentModel.Win32Exception(hr);
                }
                _filterHandle.Handle = hFilter;
            }
            return GetNextCert(IntPtr.Zero);
        }

        public IntPtr GetNextCert(IntPtr certContext)
        {
            if (!_open)
            {
                throw Marshal.GetExceptionForHR(
                                    Security.NativeMethods.CRYPT_E_NOT_FOUND);
            }
            if (Valid)
            {
                if (_filterHandle != null)
                {
                    certContext = Security.NativeMethods.CCFindCertificateFromFilter(
                                                        _storeHandle.Handle,
                                                        _filterHandle.Handle,
                                                        certContext);
                }
                else
                {
                    certContext = Security.NativeMethods.CertEnumCertificatesInStore(
                                                        _storeHandle.Handle,
                                                        certContext);
                }
            }
            else
            {
                certContext = IntPtr.Zero;
            }
            return certContext;
        }

        public IntPtr GetCertByName(string Name)
        {
            IntPtr certContext = IntPtr.Zero;

            if (!_open)
            {
                throw Marshal.GetExceptionForHR(
                                    Security.NativeMethods.CRYPT_E_NOT_FOUND);
            }
            if (Valid)
            {
                if (DownLevelHelper.HashLookupSupported())
                {
                    certContext = Security.NativeMethods.CertFindCertificateInStore(
                            _storeHandle.Handle,
                            Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                            0,                                // dwFindFlags
                            Security.NativeMethods.CertFindType.CERT_FIND_HASH_STR,
                            Name,
                            IntPtr.Zero);                     // pPrevCertContext
                }
                else
                {
                    //
                    // the pre-Win8 CAPI2 code does not provide an easy way
                    // to directly access a specific certificate.
                    // We have to iterate through all certs to find
                    // what we want.
                    //

                    while (true)
                    {
                        certContext = GetNextCert(certContext);
                        if (IntPtr.Zero == certContext)
                        {
                            break;
                        }
                        X509Certificate2 cert = new X509Certificate2(certContext);
                        if (String.Equals(
                                    cert.Thumbprint,
                                    Name,
                                    StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }
                }
            }
            return certContext;
        }

        public void FreeCert(IntPtr certContext)
        {
            Security.NativeMethods.CertFreeCertificateContext(certContext);
        }

        /// <summary>
        /// native IntPtr store handle
        /// </summary>

        public IntPtr StoreHandle
        {
            get
            {
                return _storeHandle.Handle;
            }
        }

        /// <summary>
        /// X509StoreLocation store location
        /// </summary>
        public X509StoreLocation Location
        {
            get
            {
                return _storeLocation;
            }
        }

        /// <summary>
        /// string store name
        /// </summary>
        public string StoreName
        {
            get
            {
                return _storeName;
            }
        }

        /// <summary>
        /// true if a real store is open
        /// </summary>

        public bool Valid
        {
            get
            {
                return _valid;
            }
        }

        private bool _archivedCerts = false;
        private X509StoreLocation _storeLocation = null;
        private string _storeName = null;
        private CertificateStoreHandle _storeHandle = null;
        private CertificateFilterHandle _filterHandle = null;
        private bool _valid = false;
        private bool _open = false;
    }

    /// <summary>
    /// Defines the types of items 
    /// supported by the certificate provider.
    /// </summary>    
    internal enum CertificateProviderItem
    {
        /// <summary>
        /// An unknown item.
        /// </summary>    
        Unknown,

        /// <summary>
        /// An X509 Certificate.
        /// </summary>    
        Certificate,

        /// <summary>
        /// A certificate store location.
        /// For example, cert:\CurrentUser
        /// </summary>    
        Store,

        /// <summary>
        /// A certificate store.
        /// For example, cert:\CurrentUser\My
        /// </summary>    
        StoreLocation
    }

    /// <summary>
    /// Defines the implementation of a Certificate Store Provider.  This provider
    /// allows for stateless namespace navigation of the computer's certificate
    /// store.
    /// </summary>
    [CmdletProvider("Certificate", ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(String), typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.ResolvePath)]
    [OutputType(typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.PushLocation)]
    [OutputType(typeof(Microsoft.PowerShell.Commands.X509StoreLocation), typeof(X509Certificate2), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(X509Store), typeof(X509Certificate2), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    public sealed class CertificateProvider : NavigationCmdletProvider, ICmdletProviderSupportsHelp
    {
        #region tracer

        /// <summary>
        /// tracer for certificate provider
        /// </summary>
        [TraceSource("CertificateProvider",
                      "The core command provider for certificates")]
        private readonly static PSTraceSource s_tracer = PSTraceSource.GetTracer("CertificateProvider",
                      "The core command provider for certificates");

        #endregion tracer

        /// <summary>
        /// Indicate if we already have attempted to load the PKI module
        /// </summary>
        private bool _hasAttemptedToLoadPkiModule = false;

        /// <summary>
        /// lock that guards access to the following static members
        /// -- storeLocations
        /// -- pathCache
        /// </summary>
        private static object s_staticLock = new object();

        /// <summary>
        /// list of store locations. They do not change once initialized.
        ///
        /// Synchronized on staticLock
        /// </summary>
        private static List<X509StoreLocation> s_storeLocations = null;

        /// <summary>
        /// cache that stores paths and their associated objects.
        /// 
        /// key is full path to store-location/store/certificate
        /// value is X509StoreLocation/X509NativeStore/X509Certificate2 object
        ///
        /// Synchronized on staticLock
        /// </summary>
        private static Hashtable s_pathCache = null;

        /// <summary>
        /// we allow either / or \ to be the path separator
        /// </summary>
        private static readonly char[] s_pathSeparators = new char[] { '/', '\\' };

        /// <summary>
        /// regex pattern that defines a valid cert path
        /// </summary>
        private const string certPathPattern = @"^\\((?<StoreLocation>CurrentUser|LocalMachine)(\\(?<StoreName>[a-zA-Z]+)(\\(?<Thumbprint>[0-9a-f]{40}))?)?)?$";

        /// <summary>
        /// Cache the store handle to avoid repeated CertOpenStore calls.
        /// </summary>
        private static X509NativeStore s_storeCache = null;

        /// <summary>
        /// On demand create the Regex to avoid a hit to startup perf.
        /// </summary>
        /// 
        /// <remarks>
        /// Note, its OK that staticLock is being used here because only
        /// IsValidPath is calling this static property so we shouldn't
        /// have any deadlocks due to other locked static members calling
        /// this property.
        /// </remarks>
        /// 
        private static Regex s_certPathRegex = null;
        private static Regex CertPathRegex
        {
            get
            {
                lock (s_staticLock)
                {
                    if (s_certPathRegex == null)
                    {
                        RegexOptions options = RegexOptions.IgnoreCase;

#if !CORECLR
                        options |= RegexOptions.Compiled;
#endif
                        s_certPathRegex = new Regex(certPathPattern, options);
                    }
                }
                return s_certPathRegex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the CertificateProvider class.
        /// This initializes the default certificate store locations.
        /// </summary>
        public CertificateProvider()
        {
            //
            // initialize storeLocations list and also update the cache
            //
            lock (s_staticLock)
            {
                if (s_storeLocations == null)
                {
                    s_pathCache = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    s_storeLocations =
                        new List<X509StoreLocation>();

                    //
                    // create and cache CurrentUser store-location
                    //
                    X509StoreLocation user =
                        new X509StoreLocation(StoreLocation.CurrentUser);
                    s_storeLocations.Add(user);
                    AddItemToCache(StoreLocation.CurrentUser.ToString(),
                                  user);

                    //
                    // create and cache LocalMachine store-location
                    //
                    X509StoreLocation machine =
                        new X509StoreLocation(StoreLocation.LocalMachine);
                    s_storeLocations.Add(machine);
                    AddItemToCache(StoreLocation.LocalMachine.ToString(),
                                   machine);

                    AddItemToCache("", s_storeLocations);
                }
            }
        } // constructor


        /// <summary>
        /// Removes an item at the specified path
        /// </summary>
        /// 
        /// <param name="path">
        /// The path of the item to remove.
        /// </param>
        /// 
        /// <param name="recurse">
        /// Recursively remove.
        /// </param>
        /// 
        /// <returns>
        /// Nothing.  
        /// </returns>
        /// 
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        ///     destination is null or empty.
        /// </exception>

        protected override void RemoveItem(
                                string path,
                                bool recurse)
        {
            path = NormalizePath(path);
            bool isContainer = false;
            bool fDeleteKey = false;

            object outObj = GetItemAtPath(path, false, out isContainer);
            string[] pathElements = GetPathElements(path);

            bool fUserContext = String.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase);

            // isContainer = true means not a valid certificate

            // if source store is user root store and UI is not allowed
            // we raise invalid operation
            if (DetectUIHelper.GetOwnerWindow(Host) == IntPtr.Zero && fUserContext &&
                 String.Equals(pathElements[1], "ROOT", StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.UINotAllowed;
                string errorId = "UINotAllowed";
                ThrowInvalidOperation(errorId, message);
            }

            if (DynamicParameters != null)
            {
                ProviderRemoveItemDynamicParameters dp =
                    DynamicParameters as ProviderRemoveItemDynamicParameters;
                if (dp != null)
                {
                    if (dp.DeleteKey)
                    {
                        fDeleteKey = true;
                    }
                }
            }

            if (isContainer)
            {
                if (pathElements.Length == 2) //is a store
                {
                    //not support user context
                    if (fUserContext)
                    {
                        string message = CertificateProviderStrings.CannotDeleteUserStore;
                        string errorId = "CannotDeleteUserStore";
                        ThrowInvalidOperation(errorId, message);
                    }

                    RemoveCertStore(pathElements[1], fDeleteKey, path);
                    return;
                }
                else //other container than a store
                {
                    string message = CertificateProviderStrings.CannotRemoveContainer;
                    string errorId = "CannotRemoveContainer";
                    ThrowInvalidOperation(errorId, message);
                }
            }
            else //certificate
            {
                //do remove
                X509Certificate2 certificate = outObj as X509Certificate2;
                RemoveCertItem(certificate, fDeleteKey, !fUserContext, path);
                return;
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for remove-item on the Certificate
        /// Provider.  We currently only support one dynamic parameter,
        /// "DeleteKey," that delete private key when we delete a certificate.
        /// </summary>
        /// 
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// 
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        /// 
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            return new ProviderRemoveItemDynamicParameters();
        }

        /// <summary>
        /// Moves an item at the specified path to the given destination.
        /// </summary>
        /// 
        /// <param name="path">
        /// The path of the item to move.
        /// </param>
        /// 
        /// <param name="destination">
        /// The path of the destination.
        /// </param>
        /// 
        /// <returns>
        /// Nothing.  Moved items are written to the context's pipeline.
        /// </returns>
        /// 
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        ///     destination is null or empty.
        /// </exception>

        protected override void MoveItem(
                                string path,
                                string destination)
        {
            //normalize path
            path = NormalizePath(path);
            destination = NormalizePath(destination);

            //get elements from the path
            string[] pathElements = GetPathElements(path);
            string[] destElements = GetPathElements(destination);

            bool isContainer = false;
            object cert = GetItemAtPath(path, false, out isContainer);

            //
            // isContainer = true; means an invalid path
            //
            if (isContainer)
            {
                string message = CertificateProviderStrings.CannotMoveContainer;
                string errorId = "CannotMoveContainer";
                ThrowInvalidOperation(errorId, message);
            }

            if (destElements.Length != 2) //not a store 
            {
                //if the destination leads to the same thumbprint
                if (destElements.Length == 3 &&
                   (String.Equals(pathElements[2], destElements[2], StringComparison.OrdinalIgnoreCase)))
                {
                    //in this case we think of destination path as valid 
                    //and strip the thumbprint part
                    destination = Path.GetDirectoryName(destination);
                }
                else
                {
                    string message = CertificateProviderStrings.InvalidDestStore;
                    string errorId = "InvalidDestStore";
                    ThrowInvalidOperation(errorId, message);
                }
            }

            //the second element is store location
            //we do not allow cross context move
            //we do not allow the destination store is the same as source

            if (!String.Equals(pathElements[0], destElements[0], StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.CannotMoveCrossContext;
                string errorId = "CannotMoveCrossContext";
                ThrowInvalidOperation(errorId, message);
            }

            if (String.Equals(pathElements[1], destElements[1], StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.CannotMoveToSameStore;
                string errorId = "CannotMoveToSameStore";
                ThrowInvalidOperation(errorId, message);
            }

            // if source or destination store is user root store and UI is not allowed
            // we raise invalid operation
            if (DetectUIHelper.GetOwnerWindow(Host) == IntPtr.Zero)
            {
                if ((String.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase) &&
                     String.Equals(pathElements[1], "ROOT", StringComparison.OrdinalIgnoreCase)) ||
                     (String.Equals(destElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase) &&
                     String.Equals(destElements[1], "ROOT", StringComparison.OrdinalIgnoreCase)))
                {
                    string message = CertificateProviderStrings.UINotAllowed;
                    string errorId = "UINotAllowed";
                    ThrowInvalidOperation(errorId, message);
                }
            }

            if (cert != null) //we get cert
            {
                //get destination store
                bool isDestContainer = false;
                object store = GetItemAtPath(destination, false, out isDestContainer);

                X509Certificate2 certificate = cert as X509Certificate2;
                X509NativeStore certstore = store as X509NativeStore;

                if (certstore != null)
                {
                    certstore.Open(true);

                    string action = CertificateProviderStrings.Action_Move;
                    string resource = String.Format(
                                          CultureInfo.CurrentCulture,
                                          CertificateProviderStrings.MoveItemTemplate,
                                          path,
                                          destination);
                    if (ShouldProcess(resource, action))
                    {
                        DoMove(destination, certificate, certstore, path);
                    }
                }
            }
            else
            {
                ThrowItemNotFound(path, CertificateProviderItem.Certificate);
            }
        }

        /// <summary>
        /// Creates a certificate store with the given path.
        /// </summary>
        /// 
        /// <remarks>
        /// New-Item doesn't go through the method "ItemExists". But for the
        /// CertificateProvider, New-Item can create an X509Store and return
        /// it, and the user can access the certificates within the store via its
        /// property "Certificates". We want the extra new properties of the
        /// X509Certificate2 objects to be shown to the user, so we also need 
        /// to import the PKI module in this method, if we haven't tried it yet.
        /// </remarks>
        /// 
        /// <param name="path">
        /// The path of the certificate store to create.
        /// </param>
        /// 
        ///<param name="type">
        /// Ignored. 
        /// Only support store.
        /// </param>
        ///
        /// <param name="value">
        /// Ignored
        /// </param>
        /// 
        /// <returns>
        /// Nothing.  The new certificate store object is 
        /// written to the context's pipeline.
        /// </returns>
        /// 
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        /// </exception>
        ///
        ///
        protected override void NewItem(
                string path,
                string type,
                object value)
        {
            if (!_hasAttemptedToLoadPkiModule)
            {
                // Attempt to load the PKI module if we haven't tried yet
                AttemptToImportPkiModule();
            }

            path = NormalizePath(path);

            //get the elements from the path
            string[] pathElements = GetPathElements(path);

            //only support creating store
            if (pathElements.Length != 2)
            {
                string message = CertificateProviderStrings.CannotCreateItem;
                string errorId = "CannotCreateItem";
                ThrowInvalidOperation(errorId, message);
            }

            bool fUserContext = String.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase);

            //not support user context
            if (fUserContext)
            {
                string message = CertificateProviderStrings.CannotCreateUserStore;
                string errorId = "CannotCreateUserStore";
                ThrowInvalidOperation(errorId, message);
            }
            Security.NativeMethods.CertOpenStoreFlags StoreFlags =
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_CREATE_NEW_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;

            //Create new store
            IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                pathElements[1]);
            if (IntPtr.Zero == hCertStore)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            else //free native store handle
            {
                bool fResult = false;
                fResult = Security.NativeMethods.CertCloseStore(hCertStore, 0);
            }

            X509Store outStore = new X509Store(
                                 pathElements[1],
                                 StoreLocation.LocalMachine);
            WriteItemObject(outStore, path, true);
        }
        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the cert: drive.
        /// </summary>
        ///
        /// <returns>
        /// A collection that contains the PSDriveInfo object
        /// that represents the cert: drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string providerDescription = CertificateProviderStrings.CertProvidername;

            PSDriveInfo drive =
                new PSDriveInfo(
                    "Cert", // drive name
                    ProviderInfo,// provider name
                    @"\",     // root path
                    providerDescription,
                    null);

            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
            drives.Add(drive);

            return drives;
        } // InitializeDefaultDrives

        /// <summary>
        /// Determines if the item at the given path is a store-location 
        /// or store with items in it.
        /// </summary>
        /// 
        /// <param name="path">
        /// The full path to the item.
        /// </param>
        /// 
        /// <returns>
        /// True if the path refers to a store location, or store that contains
        /// certificates.  False otherwise.
        /// </returns>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Some example reasons include:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        /// </exception>
        protected override bool HasChildItems(string path)
        {
            bool result = false;

            Utils.CheckArgForNull(path, "path");

            path = NormalizePath(path);

            if (path.Length == 0)
            {
                return true;
            }

            bool isContainer = false;

            object item = GetItemAtPath(path, false, out isContainer);

            if ((item != null) && isContainer)
            {
                X509StoreLocation storeLocation = item as X509StoreLocation;
                if (storeLocation != null)
                {
                    result = storeLocation.StoreNames.Count > 0;
                }
                else
                {
                    X509NativeStore store = item as X509NativeStore;
                    if (store != null)
                    {
                        store.Open(IncludeArchivedCerts());
                        IntPtr certContext = store.GetFirstCert(null);
                        if (IntPtr.Zero != certContext)
                        {
                            store.FreeCert(certContext);
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Determines if the specified path is syntactically and semantically valid.
        /// An example path looks like this:
        ///     cert:\CurrentUser\My\5F98EBBFE735CDDAE00E33E0FD69050EF9220254
        /// </summary>
        /// 
        /// <param name="path">
        /// The path of the item to check.
        /// </param>
        /// 
        /// <returns>
        /// True if the path is valid, false otherwise.
        /// </returns>
        protected override bool IsValidPath(string path)
        {
            path = NormalizePath(path);
            path = EnsureDriveIsRooted(path);

            bool isCertPath = CertPathRegex.Match(path).Success;

            return isCertPath;
        }

        /// <summary>
        /// Determines if the store location, store, or certificate exists 
        /// at the specified path.
        /// </summary>
        ///
        /// <remarks>
        /// The method ItemExists will be hit by all built-in cmdlets that interact 
        /// with the CertificateProvider except for the New-Item. They are:
        ///     Get-ChildItem
        ///     Set-Location
        ///     Push-Location
        ///     Pop-Location
        ///     Move-Item
        ///     Invoke-Item
        ///     Get-Item
        ///     Remove-Item
        /// So we import the PKI module in this method if we haven't tried yet.
        /// </remarks>
        /// 
        /// <param name="path">
        /// The path of the item to check.
        /// </param>
        ///
        /// <returns>
        /// True if a the store location, store, or certificate exists 
        /// at the specified path.  False otherwise.
        /// </returns>
        ///
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// 
        protected override bool ItemExists(string path)
        {
            if (!_hasAttemptedToLoadPkiModule)
            {
                // Attempt to load the PKI module if we haven't tried yet
                AttemptToImportPkiModule();
            }

            Utils.CheckArgForNull(path, "path");
            bool result = false;
            bool isContainer = false;
            object item = null;

            path = NormalizePath(path);

            if (path.Length == 0)
            {
                result = true;
            }
            else
            {
                //
                // We fetch the item to see if it exists. This is
                // because the managed cert infrastructure does not
                // provide a way to test for existence.
                //
                try
                {
                    item = GetItemAtPath(path, true, out isContainer);
                }
                catch (ProviderInvocationException e)
                {
                    //
                    // if the item is not found, we get ProviderInvocationException
                    // with inner exception set to CertificateProviderItemNotFoundException
                    // If the inner exception is not of that type
                    // then we need to rethrow
                    //
                    if (!(e.InnerException is CertificateProviderItemNotFoundException))
                    {
                        throw;
                    }
                }

                result = (bool)item;
            }

            s_tracer.WriteLine("result = {0}", result);
            return result;
        } // ItemExists

        /// <summary>
        /// Gets the store location, store, or certificate 
        /// at the specified path.
        /// </summary>
        ///
        /// <param name="path">
        /// The path of the item to retrieve.
        /// </param>
        ///
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// 
        protected override void GetItem(string path)
        {
            bool isContainer = false;

            path = NormalizePath(path);
            object item = GetItemAtPath(path, false, out isContainer);
            CertificateFilterInfo filter = GetFilter();

            if (item != null)
            {
                if (!isContainer) //certificate
                {
                    // If the filter is null, output the certificate we got.
                    if (filter == null)
                    {
                        WriteItemObject(item, path, isContainer);
                    }
                    else
                    {
                        // The filter is non null.  If the certificate
                        // satisfies the filter, output it.  Otherwise, don't.

                        X509Certificate2 cert = item as X509Certificate2;
                        Dbg.Diagnostics.Assert(cert != null, "item should be a certificate");

                        // If it's Win8 or above, filter matching for certain properties is done by
                        // the certificate enumeration filter at the API level. In that case,
                        // filter.Purpose will be 'None' and MatchesFilter will return 'True'.
                        if (MatchesFilter(cert, filter))
                        {
                            WriteItemObject(item, path, isContainer);
                        }
                    }
                }
                else  //container
                {
                    // The item is a container. If the filter is non null, we don't output it.
                    if (filter != null)
                    {
                        return;
                    }

                    X509StoreLocation storeLocation = item as X509StoreLocation;
                    if (storeLocation != null)  //store location
                    {
                        WriteItemObject(item, path, isContainer);
                    }
                    else //store
                    {
                        X509NativeStore store = item as X509NativeStore;
                        if (store != null)
                        {
                            //create X509Store
                            X509Store outStore = new X509Store(
                                                    store.StoreName,
                                                    store.Location.Location);
                            WriteItemObject(outStore, path, isContainer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the parent of the given path.
        /// </summary>
        /// 
        /// <param name="path">
        /// The path of which to get the parent.
        /// </param>
        /// 
        /// <param name="root">
        /// The root of the drive.
        /// </param>
        /// 
        /// <returns>
        /// The parent of the given path.
        /// </returns>
        protected override string GetParentPath(string path, string root)
        {
            string parentPath = base.GetParentPath(path, root);

            return parentPath;
        }

        /// <summary>
        /// Gets the name of the leaf element of the specified path.
        /// </summary>
        /// 
        /// <param name="path">
        /// The fully qualified path to the item.
        /// </param>
        /// 
        /// <returns>
        /// The leaf element of the specified path.
        /// </returns>
        protected override string GetChildName(string path)
        {
            //Path for root is empty string
            if (path != null && path.Length == 0)
            {
                return path;
            }
            else
            {
                return MyGetChildName(path);
            }
        }

        /// <summary>
        /// We want to import the PKI module explicitly because a type for X509Certificate
        /// is defined in the PKI module that add new properties to the X509Certificate2
        /// objects. We want to show those new properties to the user without requiring
        /// someone to force the loading of this module.
        /// </summary>
        private void AttemptToImportPkiModule()
        {
            const string moduleName = "pki";

            if (Runspaces.Runspace.DefaultRunspace == null)
            {
                //
                // Requires default runspace. Only import the module.
                // when a default runspace is available.
                //

                return;
            }


            CommandInfo commandInfo =
                new CmdletInfo(
                    "Import-Module",
                     typeof(Microsoft.PowerShell.Commands.ImportModuleCommand));
            Runspaces.Command importModuleCommand = new Runspaces.Command(commandInfo);

            s_tracer.WriteLine("Attempting to load module: {0}", moduleName);

            try
            {
                System.Management.Automation.PowerShell ps = null;
                ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)
                            .AddCommand(importModuleCommand)
                                .AddParameter("Name", moduleName)
                                .AddParameter("Scope", StringLiterals.Global)
                                .AddParameter("ErrorAction", ActionPreference.Ignore)
                                .AddParameter("WarningAction", ActionPreference.Ignore)
                                .AddParameter("InformationAction", ActionPreference.Ignore)
                                .AddParameter("Verbose", false)
                                .AddParameter("Debug", false);
                ps.Invoke();
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
            }

            _hasAttemptedToLoadPkiModule = true;
        }

        private string MyGetChildName(string path)
        {
            // Verify the parameters

            if (String.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException("path");
            }

            // Normalize the path
            path = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

            // Trim trailing back slashes
            path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
            string result = null;

            int separatorIndex = path.LastIndexOf(StringLiterals.DefaultPathSeparator);


            // Since there was no path separator return the entire path
            if (separatorIndex == -1)
            {
                result = path;
            }
            else
            {
                result = path.Substring(separatorIndex + 1);
            }

            return result;
        }
        /// <summary>
        /// Invokes the certificate management UI (certmgr.msc) 
        /// for any path.
        /// </summary>
        ///
        /// <param name="path">
        /// Ignored.
        /// </param>
        protected override void InvokeDefaultAction(string path)
        {
            path = NormalizePath(path);
            string action = CertificateProviderStrings.Action_Invoke;
            string certmgr = "certmgr.msc";
            string certPath = System.IO.Path.Combine(
                System.Environment.ExpandEnvironmentVariables("%windir%"), "system32");

            if (ShouldProcess(path, action))
            {
                System.Diagnostics.Process.Start(System.IO.Path.Combine(certPath, certmgr));
            }
        } // InvokeDefaultAction

        static private string EnsureDriveIsRooted(string path)
        {
            string result = path;

            // Find the drive separator
            int index = path.IndexOf(':');

            if (index != -1)
            {
                // if the drive separator is the end of the path, add
                // the root path separator back
                if (index + 1 == path.Length)
                {
                    result = path + StringLiterals.DefaultPathSeparator;
                }
            }
            else if ((path.Length == 0) || (path[0] != StringLiterals.DefaultPathSeparator))
            {
                result = StringLiterals.DefaultPathSeparator + path;
            }

            s_tracer.WriteLine("result = {0}", result);
            return result;
        } // EnsureDriveIsRooted

        static private ErrorRecord CreateErrorRecord(string path,
                                              CertificateProviderItem itemType)
        {
            Exception e = null;
            string message = null;

            //
            // first, find the resource-id so that we can display
            // correct message
            //
            switch (itemType)
            {
                case CertificateProviderItem.Certificate:
                    message = CertificateProviderStrings.CertificateNotFound;
                    break;

                case CertificateProviderItem.Store:
                    message = CertificateProviderStrings.CertificateStoreNotFound;
                    break;

                case CertificateProviderItem.StoreLocation:
                    message = CertificateProviderStrings.CertificateStoreLocationNotFound;
                    break;

                default:
                    message = CertificateProviderStrings.InvalidPath;
                    break;
            }

            message = String.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                message, path);
            ErrorDetails ed = new ErrorDetails(message);

            //
            // create appropriate exception type
            //
            switch (itemType)
            {
                case CertificateProviderItem.Certificate:
                    e = new CertificateNotFoundException(message);
                    break;

                case CertificateProviderItem.Store:
                    e = new CertificateStoreNotFoundException(message);
                    break;

                case CertificateProviderItem.StoreLocation:
                    e = new CertificateStoreLocationNotFoundException(message);
                    break;

                default:
                    e = new ArgumentException(message);
                    break;
            }

            ErrorRecord er =
                new ErrorRecord(e,
                                "CertProviderItemNotFound",
                                ErrorCategory.ObjectNotFound,
                                null);

            er.ErrorDetails = ed;

            return er;
        }

        private void ThrowErrorRemoting(int stat)
        {
            if (this.Host.Name.Equals("ServerRemoteHost", StringComparison.OrdinalIgnoreCase))
            {
                Exception e = new System.ComponentModel.Win32Exception(stat);
                String error = e.Message;
                string message = CertificateProviderStrings.RemoteErrorMessage;
                error += message;

                Exception e2 = new Exception(error);
                ThrowTerminatingError(
                        new ErrorRecord(
                            e2,
                            "RemotingFailure",
                            ErrorCategory.NotSpecified,
                            null));
            }
            else
            {
                throw new System.ComponentModel.Win32Exception(stat);
            }
        }

        private void ThrowInvalidOperation(string errorId, string message)
        {
            ErrorRecord errorRecord = new ErrorRecord(
                            new InvalidOperationException(message),
                            errorId,
                            ErrorCategory.InvalidOperation,
                            null);
            errorRecord.ErrorDetails = new ErrorDetails(message);
            ThrowTerminatingError(errorRecord);

            return;
        }

        private void ThrowItemNotFound(string path,
                                       CertificateProviderItem itemType)
        {
            ErrorRecord er = CreateErrorRecord(path, itemType);

            ThrowTerminatingError(er);
        }

        static private string NormalizePath(string path)
        {
            if (path.Length > 0)
            {
                char lastChar = path[path.Length - 1];

                if ((lastChar == '/') || (lastChar == '\\'))
                {
                    path = path.Substring(0, path.Length - 1);
                }

                string[] elts = GetPathElements(path);

                path = String.Join("\\", elts);
            }

            return path;
        }

        static private string[] GetPathElements(string path)
        {
            string[] allElts = path.Split(s_pathSeparators);
            string[] result = null;

            Stack<string> elts = new Stack<string>();

            foreach (string e in allElts)
            {
                if ((e == ".") || (e == String.Empty))
                {
                    continue;
                }
                else if (e == "..")
                {
                    if (elts.Count > 0)
                    {
                        elts.Pop();
                    }
                }
                else
                {
                    elts.Push(e);
                }
            }

            result = elts.ToArray();
            Array.Reverse(result);

            return result;
        }

        /// <summary>
        /// Delete private key
        /// </summary>
        ///
        /// <param name="pProvInfo"> key prov info </param>
        /// 
        /// <returns> no return </returns>

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.Automation.Security.NativeMethods.NCryptSetProperty(System.IntPtr,System.String,System.Void*,System.Int32,System.Int32)")]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.Automation.Security.NativeMethods.NCryptFreeObject(System.IntPtr)")]
        private void DoDeleteKey(IntPtr pProvInfo)
        {
            IntPtr hProv = IntPtr.Zero;
            Security.NativeMethods.CRYPT_KEY_PROV_INFO keyProvInfo =
                ClrFacade.PtrToStructure<Security.NativeMethods.CRYPT_KEY_PROV_INFO>(pProvInfo);

            IntPtr hWnd = DetectUIHelper.GetOwnerWindow(Host);

            if (keyProvInfo.dwProvType != 0) //legacy
            {
                if (hWnd != IntPtr.Zero)
                {
                    if (Security.NativeMethods.CryptAcquireContext(
                        ref hProv,
                        keyProvInfo.pwszContainerName,
                        keyProvInfo.pwszProvName,
                        (int)keyProvInfo.dwProvType,
                        (uint)Security.NativeMethods.ProviderFlagsEnum.CRYPT_VERIFYCONTEXT))
                    {
                        unsafe
                        {
                            void* pWnd = hWnd.ToPointer();
                            Security.NativeMethods.CryptSetProvParam(
                                hProv,
                                Security.NativeMethods.ProviderParam.PP_CLIENT_HWND,
                                &pWnd,
                                0);
                            Security.NativeMethods.CryptReleaseContext(hProv, 0);
                        }
                    }
                }

                if (!Security.NativeMethods.CryptAcquireContext(
                                ref hProv,
                                keyProvInfo.pwszContainerName,
                                keyProvInfo.pwszProvName,
                                (int)keyProvInfo.dwProvType,
                                keyProvInfo.dwFlags | (uint)Security.NativeMethods.ProviderFlagsEnum.CRYPT_DELETEKEYSET |
                                (hWnd == IntPtr.Zero ? (uint)Security.NativeMethods.ProviderFlagsEnum.CRYPT_SILENT : 0)))
                {
                    ThrowErrorRemoting(Marshal.GetLastWin32Error());
                }
            }
            else  //cng key
            {
                uint cngKeyFlag = 0;
                int result = 0;

                IntPtr hCNGProv = IntPtr.Zero;
                IntPtr hCNGKey = IntPtr.Zero;

                if ((keyProvInfo.dwFlags & (uint)Security.NativeMethods.ProviderFlagsEnum.CRYPT_MACHINE_KEYSET) != 0)
                {
                    cngKeyFlag = (uint)Security.NativeMethods.NCryptDeletKeyFlag.NCRYPT_MACHINE_KEY_FLAG;
                }

                if (hWnd == IntPtr.Zero ||
                    (keyProvInfo.dwFlags & (uint)Security.NativeMethods.ProviderFlagsEnum.CRYPT_SILENT) != 0)
                {
                    cngKeyFlag |= (uint)Security.NativeMethods.NCryptDeletKeyFlag.NCRYPT_SILENT_FLAG;
                }

                int stat = 0;
                try
                {
                    stat = Security.NativeMethods.NCryptOpenStorageProvider(
                                    ref hCNGProv,
                                    keyProvInfo.pwszProvName,
                                    0);
                    if (stat != 0)
                    {
                        ThrowErrorRemoting(stat);
                    }

                    stat = Security.NativeMethods.NCryptOpenKey(
                                        hCNGProv,
                                        ref hCNGKey,
                                        keyProvInfo.pwszContainerName,
                                        keyProvInfo.dwKeySpec,
                                        cngKeyFlag);
                    if (stat != 0)
                    {
                        ThrowErrorRemoting(stat);
                    }

                    if (0 != (cngKeyFlag & (uint)Security.NativeMethods.NCryptDeletKeyFlag.NCRYPT_SILENT_FLAG))
                    {
                        unsafe
                        {
                            void* pWnd = hWnd.ToPointer();
                            Security.NativeMethods.NCryptSetProperty(
                                hCNGProv,
                                Security.NativeMethods.NCRYPT_WINDOW_HANDLE_PROPERTY,
                                &pWnd,
                                sizeof(void*),
                                0); // dwFlags
                        }
                    }

                    stat = Security.NativeMethods.NCryptDeleteKey(hCNGKey, 0);
                    if (stat != 0)
                    {
                        ThrowErrorRemoting(stat);
                    }
                    hCNGKey = IntPtr.Zero;
                }
                finally
                {
                    if (hCNGProv != IntPtr.Zero)
                        result = Security.NativeMethods.NCryptFreeObject(hCNGProv);

                    if (hCNGKey != IntPtr.Zero)
                        result = Security.NativeMethods.NCryptFreeObject(hCNGKey);
                }
            }
        }

        /// <summary>
        /// Delete the cert store; if -DeleteKey is specified, we also delete 
        /// the associated private key
        /// </summary>
        ///
        /// <param name="storeName"> the store name </param>
        ///
        /// <param name="fDeleteKey"> boolean to specify whether or not to delete private key </param>
        ///
        /// <param name = "sourcePath"> source path </param>
        /// 
        /// <returns> no return </returns>       

        private void RemoveCertStore(string storeName, bool fDeleteKey, string sourcePath)
        {
            //if recurse is true, remove every cert in the store
            IntPtr localName = Security.NativeMethods.CryptFindLocalizedName(storeName);
            string[] pathElements = GetPathElements(sourcePath);
            if (IntPtr.Zero == localName)//not find, we can remove
            {
                X509NativeStore store = null;

                //
                // first open the store
                //
                store = GetStore(sourcePath, false, pathElements);
                store.Open(IncludeArchivedCerts());

                //
                // enumerate over each cert and remove it
                //
                IntPtr certContext = store.GetFirstCert(null);
                while (IntPtr.Zero != certContext)
                {
                    X509Certificate2 cert = new X509Certificate2(certContext);
                    string certPath = sourcePath + cert.Thumbprint;
                    RemoveCertItem(cert, fDeleteKey, true, certPath);

                    certContext = store.GetNextCert(certContext);
                }
                //remove the cert store
                Security.NativeMethods.CertOpenStoreFlags StoreFlags =
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_READONLY_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_DELETE_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;

                //delete store
                IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                storeName);
            }
            else
            {
                string message = String.Format(
                                        CultureInfo.CurrentCulture,
                                        CertificateProviderStrings.RemoveStoreTemplate,
                                        storeName);
                string errorId = "CannotRemoveSystemStore";
                ThrowInvalidOperation(errorId, message);
            }
        }
        /// <summary>
        /// Delete the a single cert from the store; if -DeleteKey is specified, we also delete 
        /// the associated private key
        /// </summary>
        ///
        /// <param name="cert"> an X509Certificate2 object </param>
        ///
        /// <param name="fDeleteKey"> boolean to specify whether or not to delete private key </param>
        ///
        /// <param name="fMachine"> machine context or user </param>
        ///
        /// <param name = "sourcePath"> source path </param>
        /// 
        /// <returns> no return </returns>       
        private void RemoveCertItem(X509Certificate2 cert, bool fDeleteKey, bool fMachine, string sourcePath)
        {
            if (cert != null)
            {
                string action = null;
                if (fDeleteKey)
                {
                    action = CertificateProviderStrings.Action_RemoveAndDeleteKey;
                }
                else
                {
                    action = CertificateProviderStrings.Action_Remove;
                }

                string resource = String.Format(
                                        CultureInfo.CurrentCulture,
                                        CertificateProviderStrings.RemoveItemTemplate,
                                        sourcePath);

                if (ShouldProcess(resource, action))
                {
                    DoRemove(cert, fDeleteKey, fMachine, sourcePath);
                }
            }
        }

        /// <summary>
        /// Delete the cert from the store; if -DeleteKey is specified, we also delete 
        /// the associated private key
        /// </summary>
        ///
        /// <param name="cert"> an X509Certificate2 object </param>
        ///
        /// <param name="fDeleteKey"> boolean to specify whether or not to delete private key </param>
        ///
        /// <param name="fMachine"> machine context or user </param>
        ///
        /// <param name = "sourcePath"> source path </param>
        /// 
        /// <returns> no return </returns>

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private void DoRemove(X509Certificate2 cert, bool fDeleteKey, bool fMachine, string sourcePath)
        {
            //get CERT_KEY_PROV_INFO_PROP_ID
            int provSize = 0;
            IntPtr pProvInfo = IntPtr.Zero;
            bool fHasPrivateKey = false;

            try
            {
                if (fDeleteKey)
                {
                    //it is fine if below call fails
                    if (Security.NativeMethods.CertGetCertificateContextProperty(
                                cert.Handle,
                                Security.NativeMethods.CertPropertyId.CERT_KEY_PROV_INFO_PROP_ID,
                                IntPtr.Zero,
                                ref provSize))
                    {
                        pProvInfo = Marshal.AllocHGlobal((int)provSize);

                        if (Security.NativeMethods.CertGetCertificateContextProperty(
                                cert.Handle,
                                Security.NativeMethods.CertPropertyId.CERT_KEY_PROV_INFO_PROP_ID,
                                pProvInfo,
                                ref provSize))
                        {
                            fHasPrivateKey = true;
                        }
                    }

                    if (!fHasPrivateKey)
                    {
                        //raise a verbose message
                        //we should not use WriteWarning here
                        string verboseNoPrivatekey = CertificateProviderStrings.VerboseNoPrivateKey;
                        WriteVerbose(verboseNoPrivatekey);
                    }
                }

                //do remove certificate
                //should not use the original handle

                if (!Security.NativeMethods.CertDeleteCertificateFromStore(
                            Security.NativeMethods.CertDuplicateCertificateContext(cert.Handle)))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                //commit the change to physical store
                if (sourcePath.Contains("UserDS"))
                {
                    Security.NativeMethods.CERT_CONTEXT context =
                        ClrFacade.PtrToStructure<Security.NativeMethods.CERT_CONTEXT>(cert.Handle);

                    CommitUserDS(context.hCertStore);
                }

                if (DownLevelHelper.LogCertChangeSupported())
                {
                    Security.NativeMethods.LogCertDelete(fMachine, cert.Handle);
                }

                //delete private key
                if (fDeleteKey && fHasPrivateKey)
                {
                    DoDeleteKey(pProvInfo);
                }
            }
            finally
            {
                if (pProvInfo != IntPtr.Zero)
                    Marshal.FreeHGlobal(pProvInfo);
            }
        }

        /// <summary>
        /// Commit store for UserDS store 
        /// </summary>
        ///
        /// <param name="storeHandle"> an IntPtr for store handle </param>
        ///
        /// <returns> no return </returns>
        ///
        private void CommitUserDS(IntPtr storeHandle)
        {
            if (!Security.NativeMethods.CertControlStore(
                                        storeHandle,
                                        0,
                                        Security.NativeMethods.CertControlStoreType.CERT_STORE_CTRL_COMMIT,
                                        IntPtr.Zero))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Delete the cert from the original store and add to the destination store
        /// </summary>
        ///
        /// <param name="destination"> destination path </param>
        ///
        /// <param name="cert"> an X509Certificate2 </param>
        ///
        /// <param name="store"> an X509NativeStore </param>
        ///
        /// <param name="sourcePath"> source path </param>
        ///
        /// <returns> no return </returns>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private void DoMove(string destination, X509Certificate2 cert, X509NativeStore store, string sourcePath)
        {
            IntPtr dupCert = IntPtr.Zero;  //should not free this
            IntPtr outCert = IntPtr.Zero;

            //duplicate cert first
            dupCert = Security.NativeMethods.CertDuplicateCertificateContext(cert.Handle);

            if (dupCert == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            else
            {
                if (!Security.NativeMethods.CertAddCertificateContextToStore(
                                             store.StoreHandle,
                                             cert.Handle,
                                             (uint)Security.NativeMethods.AddCertificateContext.CERT_STORE_ADD_ALWAYS,
                                             ref outCert))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!Security.NativeMethods.CertDeleteCertificateFromStore(dupCert))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                if (DownLevelHelper.LogCertChangeSupported())
                {
                    bool fMachine = store.Location.Location == StoreLocation.LocalMachine;

                    if (cert.HasPrivateKey &&
                        String.Equals(store.StoreName, "MY", StringComparison.OrdinalIgnoreCase))
                    {
                        Security.NativeMethods.LogCertCopy(fMachine, cert.Handle);
                    }

                    Security.NativeMethods.LogCertDelete(fMachine, cert.Handle);
                }
            }

            //commit the change to physical store
            if (destination.Contains("UserDS"))
            {
                CommitUserDS(store.StoreHandle);
            }

            if (sourcePath.Contains("UserDS"))
            {
                Security.NativeMethods.CERT_CONTEXT context = ClrFacade.PtrToStructure<Security.NativeMethods.CERT_CONTEXT>(cert.Handle);

                CommitUserDS(context.hCertStore);
            }

            //get the output object
            X509Certificate2 outObj = new X509Certificate2(outCert);
            string certName = GetCertName(outObj);
            string certPath = MakePath(destination, certName);
            WriteItemObject((object)outObj, certPath, false);
        }

        /// <summary>
        /// fetches the store-location/store/certificate at the
        /// specified path.
        /// </summary>
        ///
        /// <param name="path"> path to the item </param>
        /// <param name="test"> True if this is to only for an ItemExists call. Returns True / False.</param>
        ///
        /// <param name="isContainer"> set to true if item exists and is a container </param>
        ///
        /// <returns> item at the path </returns>
        ///
        private object GetItemAtPath(string path, bool test, out bool isContainer)
        {
            Utils.CheckArgForNull(path, "path");

            object item = null;
            string[] pathElements = GetPathElements(path);

            //
            // certs have a fixed depth hierarchy.
            //
            // pathElements.Length == 0 ==> List<X509StoreLocation>
            // pathElements.Length == 1 ==> X509StoreLocation
            // pathElements.Length == 2 ==> X509NativeStore
            // pathElements.Length == 3 ==> X509Certificate2
            //
            // Thus lengths 1 & 2 are container items.
            //
            isContainer = (pathElements.Length >= 0) &&
                (pathElements.Length <= 2);

            X509NativeStore store = null;

            //
            // handle invalid path depth
            //
            if ((pathElements.Length > 3) ||
                (pathElements.Length < 0))
            {
                if (test)
                {
                    return false;
                }
                else
                {
                    ThrowItemNotFound(path, CertificateProviderItem.Certificate);
                }
            }

            //
            // if path cache already has the item, return it
            //
            item = GetCachedItem(path);

            if (item == null)
            {
                switch (pathElements.Length)
                {
                    case 1:
                        // if this is a single element path and if we
                        // did not find in path-cache, the path
                        // must be wrong. This is because we initialize
                        // the only possible two store locations in ctor
                        if (test)
                        {
                            isContainer = false;
                            return false;
                        }
                        else
                        {
                            ThrowItemNotFound(path, CertificateProviderItem.StoreLocation);
                        }
                        break;

                    case 2:
                        //
                        // items at paths of depth 2 are stores.
                        //

                        //
                        // GetStore() handles store-not-found case. If Test is true,
                        // Item is True / False and we can return it.
                        //
                        store = GetStore(path, test, pathElements);
                        item = store;

                        break;

                    case 3:
                        //
                        // items at paths of depth 3 are certificates.
                        //
                        string storePath = GetParentPath(path, "");
                        string[] storePathElements = GetPathElements(storePath);

                        //
                        // first get the store
                        //

                        store = GetStore(storePath, false, storePathElements);

                        //
                        // store must be opened to get access to the
                        // certificates within it.
                        //

                        store.Open(IncludeArchivedCerts());

                        IntPtr certContext = store.GetCertByName(pathElements[2]);
                        if (IntPtr.Zero == certContext)
                        {
                            if (test)
                            {
                                return false;
                            }
                            else
                            {
                                ThrowItemNotFound(path, CertificateProviderItem.Certificate);
                            }
                        }

                        // Return true / false rather than the certificate
                        if (test)
                        {
                            item = true;
                        }
                        else
                        {
                            item = new X509Certificate2(certContext);
                        }
                        store.FreeCert(certContext);

                        break;

                    default:
                        // already handled by ThrowItemNotFound()
                        // at the beginning.
                        break;
                }
            }

            if ((item != null) && test)
            {
                item = true;
            }

            return item;
        } // GetItem

        /// <summary>
        /// Gets the child items of a given store, or location.
        /// </summary>
        ///
        /// <param name="path">
        /// The full path of the store or location to enumerate.
        /// </param>
        ///
        /// <param name="recurse">
        /// If true, recursively enumerates the child items as well.
        /// </param>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// 
        protected override void GetChildItems(string path, bool recurse)
        {
            path = NormalizePath(path);

            GetChildItemsOrNames(path, recurse, ReturnContainers.ReturnAllContainers, false, GetFilter());
        }

        /// <summary>
        /// Gets the child names of a given store, or location.
        /// </summary>
        ///
        /// <param name="path">
        /// The full path of the store or location to enumerate.
        /// </param>
        ///
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// 
        protected override void GetChildNames(
            string path,
            ReturnContainers returnContainers)
        {
            path = NormalizePath(path);
            GetChildItemsOrNames(path, false, returnContainers, true, GetFilter());
        } // GetChildNames


        /// <summary>
        /// Determines if the item at the specified path is a store
        /// or location.
        /// </summary>        
        ///
        /// <returns>
        /// True if the item at the specified path is a store or location.
        /// False otherwise.
        /// </returns>
        ///
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
        /// 
        /// <exception cref="System.Security.Cryptography.CryptographicException">
        /// This exception can be thrown if any cryptographic error occurs.
        /// It is not possible to know exactly what went wrong.
        /// This is because of the way CryptographicException is designed.
        /// Possible reasons:
        ///  -- certificate is invalid
        ///  -- certificate has no private key
        ///  -- certificate password mismatch
        ///  -- etc
        /// </exception>
        /// 
        protected override bool IsItemContainer(string path)
        {
            path = NormalizePath(path);
            Utils.CheckArgForNull(path, "path");
            bool isContainer = false;

            if (path.Length == 0)
            {
                //
                // root path is always container
                //
                isContainer = true;
            }
            else
            {
                //
                // We fetch the item to see if it is a container. This is
                // because the managed cert infrastructure does not
                // provide a way to test for existence.
                //
                GetItemAtPath(path, true, out isContainer);
            }

            s_tracer.WriteLine("result = {0}", isContainer);
            return isContainer;
        } // IsItemContainer

        /// <summary>
        /// Gets the dynamic parameters for get-item on the Certificate
        /// Provider.  We currently support the following dynamic parameters:
        /// "CodeSigning," that returns only certificates good for signing
        /// code or scripts.
        /// </summary>
        /// 
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// 
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// 
        protected override object GetItemDynamicParameters(string path)
        {
            if (DownLevelHelper.NativeFilteringSupported())
            {
                return new CertificateProviderDynamicParameters();
            }
            else
            {
                return new CertificateProviderCodeSigningDynamicParameters();
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for get-childitem on the Certificate
        /// Provider.  We currently only support one dynamic parameter,
        /// "CodeSigning," that returns only certificates good for signing
        /// code or scripts.
        /// </summary>
        /// 
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// 
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        /// 
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            if (DownLevelHelper.NativeFilteringSupported())
            {
                return new CertificateProviderDynamicParameters();
            }
            else
            {
                return new CertificateProviderCodeSigningDynamicParameters();
            }
        }

        #endregion DriveCmdletProvider overrides

        #region private members

        /// <summary>
        /// Helper function to get store-location/store/cert at
        /// the specified path.
        /// </summary>
        ///
        /// <param name="path"> path to the item  </param>
        ///
        /// <param name="recurse"> whether we need to recursively find all </param>
        ///
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        ///
        /// <param name="returnNames"> whether we only need the names </param>
        ///
        /// <param name="filter"> filter info </param>
        ///
        /// <returns> Does not return a value </returns>
        ///
        /// <remarks>  </remarks>
        ///
        private void GetChildItemsOrNames(
            string path,
            bool recurse,
            ReturnContainers returnContainers,
            bool returnNames,
            CertificateFilterInfo filter)
        {
            object thingToReturn = null;
            string childPath = null;

            bool returnAllContainers = returnContainers == ReturnContainers.ReturnAllContainers;

            Utils.CheckArgForNull(path, "path");

            //
            // children at the root path are store locations
            //
            if (path.Length == 0)
            {
                foreach (X509StoreLocation l in s_storeLocations)
                {
                    thingToReturn = returnNames ?
                        (object)l.LocationName : (object)l;

                    // 'returnNames' is true only when called from
                    // GetChildNames(), in which case 'recurse' will always be
                    // false.  When the -Path parameter needs to be globbed,
                    // the potential location names should be returned by
                    // calling this method from GetChildNames.

                    // The original code didn't have a "|| returnNames" clause.
                    // Suppose the user types:
                    //     dir cert:\curr* -CodeSigningCert -recurse
                    // We need to do path globbing here to resolve wild cards.
                    // Since -CodeSigningCert is present, 'filter' is not null.
                    // Since this method is called from GetChildNames() when
                    // doing the path globbing, 'returnNames' is true and
                    // 'recurse' is false.
                    // In the original code, nothing was returned by
                    // WriteItemObject(), so the path globbing fails and the
                    // above dir command would not display the certificates
                    // as expected.

                    // Another case is:
                    //     dir cert:\ -CodeSigningCert -Recurse
                    // -Recurse is present, so we need to call
                    // DoManualGetChildItems, and inside DoManualGetChildItems,
                    // this method will be called to get the names.
                    // The original code had the same problem for this case.

                    // With the "|| returnNames" clause, we test if this method
                    // is called from the GetChildNames().  When this method is
                    // called from GetChildNames(), 'recurse' will always be
                    // false.  Then we should return the names whether 'filter'
                    // is null or not.

                    if (filter == null || returnNames)
                    {
                        WriteItemObject(thingToReturn, l.LocationName, true);
                    }
                    childPath = l.LocationName;
                    if (recurse)
                    {
                        GetChildItemsOrNames(
                                        childPath,
                                        recurse,
                                        returnContainers,
                                        returnNames,
                                        filter);
                    }
                }
            }
            else
            {
                string[] pathElements = GetPathElements(path);

                //
                // children at depth 1 are stores
                //
                if (pathElements.Length == 1)
                {
                    GetStoresOrNames(pathElements[0],
                                     recurse,
                                     returnNames,
                                     filter);
                }
                //
                // children at depth 2 are certificates
                //
                else if (pathElements.Length == 2)
                {
                    GetCertificatesOrNames(path,
                                           pathElements,
                                           returnNames,
                                           filter);
                }
                else
                {
                    ThrowItemNotFound(path, CertificateProviderItem.Certificate);
                }
            }
        }

        /// <summary>
        /// get the name of the specified certificate
        /// </summary>
        ///
        /// <param name="cert">  </param>
        ///
        /// <returns> cert name  </returns>
        ///
        /// <remarks> we use Thumbprint as the name  </remarks>
        ///
        private static string GetCertName(X509Certificate2 cert)
        {
            return cert.Thumbprint;
        }

        /// <summary>
        /// Get cert objects or their name at the specified path
        /// </summary>
        ///
        /// <param name="path"> path to cert </param>
        ///
        /// <param name="pathElements"> path elements </param>
        ///
        /// <param name="returnNames"> whether we should return only the names (instead of objects) </param>
        ///
        /// <param name="filter"> filter info </param>
        ///
        /// <returns> Does not return a value </returns>
        ///
        private void GetCertificatesOrNames(string path,
                                             string[] pathElements,
                                             bool returnNames,
                                             CertificateFilterInfo filter)
        {
            object thingToReturn = null;
            string certPath = null;
            X509NativeStore store = null;

            //
            // first open the store
            //

            store = GetStore(path, false, pathElements);
            store.Open(IncludeArchivedCerts());

            //
            // enumerate over each cert and return it (or its name)
            //
            IntPtr certContext = store.GetFirstCert(filter);

            while (IntPtr.Zero != certContext)
            {
                X509Certificate2 cert = new X509Certificate2(certContext);

                if (MatchesFilter(cert, filter))
                {
                    string certName = GetCertName(cert);
                    certPath = MakePath(path, certName);

                    if (returnNames)
                    {
                        thingToReturn = (object)certName;
                    }
                    else
                    {
#if CORECLR
                        //TODO:CORECLR See if there is a need to create a copy of cert like its done on Full PS
                        X509Certificate2 cert2 = cert;
#else
                        X509Certificate2 cert2 = new X509Certificate2(cert);
#endif
                        PSObject myPsObj = new PSObject(cert2);
                        thingToReturn = (object)myPsObj;
                    }
                    WriteItemObject(thingToReturn, certPath, false);
                }
                certContext = store.GetNextCert(certContext);
            }
        }

        /// <summary>
        /// get X509StoreLocation object at path
        /// </summary>
        ///
        /// <param name="path">  </param>
        ///
        /// <returns> X509StoreLocation object </returns>
        ///
        private X509StoreLocation GetStoreLocation(string path)
        {
            //
            // we store the only two possible store-location
            // objects during ctor.
            //
            X509StoreLocation location =
                GetCachedItem(path) as X509StoreLocation;

            if (location == null)
            {
                ThrowItemNotFound(path, CertificateProviderItem.StoreLocation);
            }

            return location;
        }

        /// <summary>
        /// get the X509NativeStore object at path
        /// </summary>
        ///
        /// <param name="path"> path to store  </param>
        /// <param name="test"> True if this should be a test for path existence. Returns True or False</param>
        ///
        /// <param name="pathElements"> path elements </param>
        ///
        /// <returns> X509NativeStore object </returns>
        ///
        private X509NativeStore GetStore(string path, bool test, string[] pathElements)
        {
            X509StoreLocation location = GetStoreLocation(pathElements[0]);
            X509NativeStore store = GetStore(path, pathElements[1], location);

            if (store == null)
            {
                if (test)
                {
                    return null;
                }
                else
                {
                    ThrowItemNotFound(path, CertificateProviderItem.Store);
                }
            }

            return store;
        }

        /// <summary>
        /// gets the X509NativeStore at the specified path.
        /// Adds to cache if not already there.
        /// </summary>
        ///
        /// <param name="storePath"> path to the store </param>
        ///
        /// <param name="storeName"> name of store (path leaf element) </param>
        ///
        /// <param name="storeLocation"> location of store (CurrentUser or LocalMachine) </param>
        ///
        /// <returns> X509NativeStore object </returns>
        ///
        private X509NativeStore GetStore(string storePath,
                                   string storeName,
                                   X509StoreLocation storeLocation)
        {
            if (!storeLocation.StoreNames.ContainsKey(storeName))
            {
                ThrowItemNotFound(storePath, CertificateProviderItem.Store);
            }
            if (s_storeCache != null)
            {
                if (s_storeCache.Location != storeLocation ||
                    !String.Equals(
                                s_storeCache.StoreName,
                                storeName,
                                StringComparison.OrdinalIgnoreCase))
                {
                    s_storeCache = null;
                }
            }

            if (s_storeCache == null)
            {
                s_storeCache = new X509NativeStore(storeLocation, storeName);
            }

            return s_storeCache;
        }

        /// <summary>
        /// gets X509NativeStore objects or their name at the specified path.
        /// </summary>
        ///
        /// <param name="path"> path to the store </param>
        ///
        /// <param name="recurse"> recursively return all items if true </param>
        ///
        /// <param name="returnNames">  </param>
        ///
        /// <param name="filter"> filter info </param>
        ///
        /// <returns> Does not return a value </returns>
        ///
        /// <remarks>  </remarks>
        ///
        private void GetStoresOrNames(
            string path,
            bool recurse,
            bool returnNames,
            CertificateFilterInfo filter)
        {
            object thingToReturn = null;

            X509StoreLocation location = GetStoreLocation(path);

            string storePath = null;

            //
            // enumerate over each store
            //
            foreach (string name in location.StoreNames.Keys)
            {
                storePath = MakePath(path, name);
                if (returnNames)
                {
                    thingToReturn = name;
                }
                else
                {
                    X509NativeStore store = GetStore(storePath, name, location);
                    X509Store ManagedStore = new X509Store(
                                                    store.StoreName,
                                                    store.Location.Location);
                    thingToReturn = ManagedStore;
                }

                // 'returnNames' is true only when called from
                // GetChildNames(), in which case 'recurse' will always be
                // false.  When the -Path parameter needs to be globbed,
                // the potential store names should be returned by
                // calling this method from GetChildNames.

                // The original code didn't have a "|| returnNames" clause.
                // Suppose the user types:
                //     dir cert:\CurrentUser\Tru* -CodeSigningCert -recurse
                // We need to do path globbing here to resolve wild cards.
                // Since -CodeSigningCert is present, 'filter' is not null.
                // Since this method is called from GetChildNames() when
                // doing the path globbing, 'returnNames' is true and
                // 'recurse' is false.
                // In the original code, nothing was returned by
                // WriteItemObject(), so the path globbing fails and the
                // above dir command would not display the certificates
                // as expected.

                // Another case is:
                //     dir cert:\CurrentUser -CodeSigningCert -Recurse
                // -Recurse is present, so we need to call
                // DoManualGetChildItems, and inside DoManualGetChildItems,
                // this method will be called to get the names.
                // The original code had the same problem for this case.

                // With the "|| returnNames" clause, we test if this method
                // is called from the GetChildNames().  When this method is
                // called from GetChildNames(), 'recurse' will always be
                // false.  Then we should return the names whether 'filter'
                // is null or not.

                if (filter == null || returnNames)
                {
                    WriteItemObject(thingToReturn, name, true);
                }

                //
                // if recurse is true, get cert objects (or names) as well
                //
                if (recurse)
                {
                    string[] pathElements = GetPathElements(storePath);
                    GetCertificatesOrNames(
                                    storePath,
                                    pathElements,
                                    returnNames,
                                    filter);
                }
            }
        }

        private CertificateFilterInfo GetFilter()
        {
            CertificateFilterInfo filter = null;

            if (DynamicParameters != null)
            {
                if (DownLevelHelper.NativeFilteringSupported())
                {
                    CertificateProviderDynamicParameters dp =
                        DynamicParameters as CertificateProviderDynamicParameters;
                    if (dp != null)
                    {
                        bool filterSpecified = false;

                        filter = new CertificateFilterInfo();
                        if (dp.CodeSigningCert)
                        {
                            filter.Purpose = CertificatePurpose.CodeSigning;
                            filterSpecified = true;
                        }
                        if (dp.DocumentEncryptionCert)
                        {
                            filter.Purpose = CertificatePurpose.DocumentEncryption;
                            filterSpecified = true;
                        }
                        if (dp.SSLServerAuthentication)
                        {
                            filter.SSLServerAuthentication = true;
                            filterSpecified = true;
                        }
                        if (dp.DnsName.Punycode != null)
                        {
                            filter.DnsName = dp.DnsName.Punycode;
                            filterSpecified = true;
                        }
                        if (dp.Eku != null)
                        {
                            filter.Eku = dp.Eku;
                            filterSpecified = true;
                        }
                        if (dp.ExpiringInDays >= 0)
                        {
                            filter.ExpiringInDays = dp.ExpiringInDays;
                            filterSpecified = true;
                        }
                        if (!filterSpecified)
                        {
                            filter = null;
                        }
                    }
                }
                else
                {
                    CertificateProviderCodeSigningDynamicParameters dp =
                        DynamicParameters as CertificateProviderCodeSigningDynamicParameters;
                    if (dp != null)
                    {
                        if (dp.CodeSigningCert)
                        {
                            filter = new CertificateFilterInfo();
                            filter.Purpose = CertificatePurpose.CodeSigning;
                        }
                    }
                }
            }
            return filter;
        }

        private bool IncludeArchivedCerts()
        {
            bool includeArchivedCerts = false;

            if (Force)
            {
                includeArchivedCerts = true;
            }
            return includeArchivedCerts;
        }

        // If it's Win8 or above, filter matching for certain properties is done by
        // the certificate enumeration filter at the API level. In that case,
        // filter.Purpose will be 'None' and MatchesFilter will return 'True'.
        private static bool MatchesFilter(X509Certificate2 cert,
                                   CertificateFilterInfo filter)
        {
            //
            // no filter means, match everything
            //
            if ((filter == null) ||
                (filter.Purpose == CertificatePurpose.NotSpecified) ||
                (filter.Purpose == CertificatePurpose.All))
            {
                return true;
            }

            switch (filter.Purpose)
            {
                case CertificatePurpose.CodeSigning:
                    if (SecuritySupport.CertIsGoodForSigning(cert))
                    {
                        return true;
                    }
                    break;

                case CertificatePurpose.DocumentEncryption:
                    if (SecuritySupport.CertIsGoodForEncryption(cert))
                    {
                        return true;
                    }
                    break;

                default:
                    break;
            }

            return false;
        }

        private static object GetCachedItem(string path)
        {
            object item = null;

            lock (s_staticLock)
            {
                if (s_pathCache.ContainsKey(path))
                {
                    item = s_pathCache[path];
                    Dbg.Diagnostics.Assert(item != null, "GetCachedItem");
                }
            }

            return item;
        }

        private static void AddItemToCache(string path, object item)
        {
            lock (s_staticLock)
            {
                if ((item != null) && (!s_pathCache.ContainsKey(path)))
                {
                    s_pathCache.Add(path, item);
                }
            }
        }

        #endregion private members

        #region ICmdletProviderSupportsHelp Members

        /// <summary>
        /// Get provider-specific help
        /// </summary>
        /// <param name="helpItemName">
        /// Name of help item or cmdlet for which user has requested help
        /// </param>
        /// <param name = "path">
        /// Path to the current location or path to the location of the property that the user needs
        /// help about.
        /// </param>
        /// <returns>
        /// Provider specific MAML help content string
        /// </returns>
        string ICmdletProviderSupportsHelp.GetHelpMaml(string helpItemName, string path)
        {
            //
            // Get the ver and noun from helpItemName
            //
            string verb = null;
            string noun = null;
            try
            {
                if (!String.IsNullOrEmpty(helpItemName))
                {
                    CmdletInfo.SplitCmdletName(helpItemName, out verb, out noun);
                }
                else
                {
                    return String.Empty;
                }

                if (String.IsNullOrEmpty(verb) || String.IsNullOrEmpty(noun))
                {
                    return String.Empty;
                }

                //
                // Load the help file from the current UI culture subfolder of the module's root folder
                //
                XmlDocument document = new XmlDocument();

                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

                string fullHelpPath = Path.Combine(
                    this.ProviderInfo.ApplicationBase,
                    currentUICulture.ToString(),
                    this.ProviderInfo.HelpFile);
                XmlReaderSettings settings = new XmlReaderSettings();
#if !CORECLR
                settings.XmlResolver = null;
#endif
                using (XmlReader reader = XmlReader.Create(fullHelpPath, settings))
                {
                    document.Load(reader);
                }

                // Add "msh" and "command" namespaces from the MAML schema
                XmlNamespaceManager nsMgr = new XmlNamespaceManager(document.NameTable);
                nsMgr.AddNamespace("msh", HelpCommentsParser.mshURI);
                nsMgr.AddNamespace("command", HelpCommentsParser.commandURI);


                // Compose XPath query to select the appropriate node based on the cmdlet
                string xpathQuery = String.Format(
                    CultureInfo.InvariantCulture,
                    HelpCommentsParser.ProviderHelpCommandXPath,
                    String.Empty,
                    verb,
                    noun);

                // Execute the XPath query and return its MAML snippet
                XmlNode result = document.SelectSingleNode(xpathQuery, nsMgr);
                if (result != null)
                {
                    return result.OuterXml;
                }
            }
            catch (XmlException)
            {
                return String.Empty;
            }
            catch (PathTooLongException)
            {
                return String.Empty;
            }
            catch (IOException)
            {
                return String.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return String.Empty;
            }
            catch (NotSupportedException)
            {
                return String.Empty;
            }
            catch (SecurityException)
            {
                return String.Empty;
            }
            catch (XPathException)
            {
                return String.Empty;
            }

            return String.Empty;
        }

        #endregion
    } // CertificateProvider

    /// <summary>
    /// Defines a class to represent a store location in the certificate
    /// provider.  The two possible store locations are CurrentUser and 
    /// LocalMachine
    /// </summary>        
    public sealed class X509StoreLocation
    {
        /// <summary>
        /// Gets the location, as a string.
        /// </summary>        
        public string LocationName
        {
            get
            {
                return _location.ToString();
            }
        }

        /// <summary>
        /// Gets the location as a 
        /// <see cref="System.Security.Cryptography.X509Certificates.StoreLocation" />
        /// </summary>        
        public StoreLocation Location
        {
            get
            {
                return _location;
            }

            set
            {
                _location = value;
            }
        }
        private StoreLocation _location = StoreLocation.CurrentUser;

        /// <summary>
        /// Gets the list of stores at this location.
        /// </summary>        
        public Hashtable StoreNames
        {
            get
            {
                Hashtable storeNames;
                //always try to get new names
                storeNames = new Hashtable(StringComparer.OrdinalIgnoreCase);

                // since there is no managed support to obtain store names,
                // we use pinvoke to get it ourselves.
                List<string> names = Crypt32Helpers.GetStoreNamesAtLocation(_location);
                foreach (string name in names)
                {
                    storeNames.Add(name, true);
                }

                return storeNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the X509StoreLocation class.
        /// </summary>        
        public X509StoreLocation(StoreLocation location)
        {
            Location = location;
        }
    }

    /// <summary>
    /// Defines the type of EKU string
    /// The structure contains friendly name and EKU oid
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct EnhancedKeyUsageRepresentation
    {
        /// <summary>
        /// Localized friendly name of EKU
        /// </summary>   
        private string _friendlyName;

        /// <summary>
        /// OID of EKU
        /// </summary>   
        private string _oid;

        /// <summary>
        /// constructor of an EnhancedKeyUsageRepresentation
        /// </summary>   

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Oid")]

        public EnhancedKeyUsageRepresentation(string inputFriendlyName, string inputOid)
        {
            _friendlyName = inputFriendlyName;
            _oid = inputOid;
        }

        /// <summary>
        /// value comparison
        /// </summary>   
        public bool Equals(EnhancedKeyUsageRepresentation keyUsage)
        {
            bool match = false;

            if (_oid != null && keyUsage._oid != null)
            {
                // OID strings only contain numbers and periods

                if (String.Equals(_oid, keyUsage._oid, StringComparison.Ordinal))
                {
                    match = true;
                }
            }
            else if (_oid == null && keyUsage._oid == null)
            {
                match = true;
            }
            return match;
        }

        /// <summary>
        /// get property of friendlyName
        /// </summary>
        public string FriendlyName
        {
            get
            {
                return _friendlyName;
            }
        }

        /// <summary>
        /// get property of oid
        /// </summary>
        public string ObjectId
        {
            get
            {
                return _oid;
            }
        }

        /// <summary>
        /// get display string
        /// </summary>
        public override string ToString()
        {
            return String.IsNullOrEmpty(_friendlyName) ?
                        _oid :
                        _friendlyName + " (" + _oid + ")";
        }
    }

    /// <summary>
    /// class for SendAsTrustedIssuer
    /// </summary>

    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    public sealed class SendAsTrustedIssuerProperty
    {
        /// <summary>
        /// get property of SendAsTrustedIssuer
        /// </summary> 

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool ReadSendAsTrustedIssuerProperty(X509Certificate2 cert)
        {
            bool fHasProperty = false;
            if (DownLevelHelper.TrustedIssuerSupported())
            {
                int propSize = 0;
                //try to get the property
                //it is fine if fail for not there
                if (Security.NativeMethods.CertGetCertificateContextProperty(
                                cert.Handle,
                                Security.NativeMethods.CertPropertyId.CERT_SEND_AS_TRUSTED_ISSUER_PROP_ID,
                                IntPtr.Zero,
                                ref propSize))
                {
                    //we have the property
                    fHasProperty = true;
                }
                else
                {
                    //if fail
                    int error = Marshal.GetLastWin32Error();
                    if (error != Security.NativeMethods.CRYPT_E_NOT_FOUND)
                    {
                        throw new System.ComponentModel.Win32Exception(error);
                    }
                }
            }

            return fHasProperty;
        }

        /// <summary>
        /// set property of SendAsTrustedIssuer
        /// </summary> 

        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void WriteSendAsTrustedIssuerProperty(X509Certificate2 cert, string certPath, bool addProperty)
        {
            if (DownLevelHelper.TrustedIssuerSupported())
            {
                IntPtr propertyPtr = IntPtr.Zero;
                Security.NativeMethods.CRYPT_DATA_BLOB dataBlob = new Security.NativeMethods.CRYPT_DATA_BLOB();
                dataBlob.cbData = 0;
                dataBlob.pbData = IntPtr.Zero;
                X509Certificate certFromStore = null;

                try
                {
                    if (certPath != null)
                    {
                        //try to open the store and get the cert out
                        //in case the store handle is already released
                        string[] pathElements = GetPathElements(certPath);

                        //certpath is in the format: Microsoft.Powershell.Security\
                        //Certificate::CurrentUser(LocalMachine)\my\HashID
                        //obtained pathElements[0] is Microsoft.Powershell.Security
                        //obtained pathElements[1] is Certificate::CurrentUser
                        //obtained pathElements[2] is MY
                        //obtained pathElements[3] is HashID

                        bool fUserContext = String.Equals(pathElements[1], "Certificate::CurrentUser", StringComparison.OrdinalIgnoreCase);

                        X509StoreLocation storeLocation =
                            new X509StoreLocation(fUserContext ? StoreLocation.CurrentUser : StoreLocation.LocalMachine);

                        //get certificate from the store pathElements[2]
                        X509NativeStore store = null;

                        store = new X509NativeStore(storeLocation, pathElements[2]);
                        store.Open(true); //including archival flag

                        IntPtr certContext = store.GetCertByName(pathElements[3]);

                        if (certContext != IntPtr.Zero)
                        {
                            certFromStore = new X509Certificate2(certContext);
                            store.FreeCert(certContext);
                        }
                    }

                    if (addProperty) //should add the property
                    {
                        propertyPtr = Marshal.AllocHGlobal(Marshal.SizeOf(dataBlob));
                        Marshal.StructureToPtr(dataBlob, propertyPtr, false);
                    }

                    //set property
                    if (!Security.NativeMethods.CertSetCertificateContextProperty(
                                certFromStore != null ? certFromStore.Handle : cert.Handle,
                                Security.NativeMethods.CertPropertyId.CERT_SEND_AS_TRUSTED_ISSUER_PROP_ID,
                                0,
                                propertyPtr))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    if (propertyPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(propertyPtr);
                    }
                }
            }
            else
            {
                throw Marshal.GetExceptionForHR(Security.NativeMethods.NTE_NOT_SUPPORTED);
            }
        }

        private static readonly char[] s_separators = new char[] { '/', '\\' };
        static private string[] GetPathElements(string path)
        {
            string[] allElts = path.Split(s_separators);
            string[] result = null;

            Stack<string> elts = new Stack<string>();

            foreach (string e in allElts)
            {
                if ((e == ".") || (e == String.Empty))
                {
                    continue;
                }
                else if (e == "..")
                {
                    if (elts.Count > 0)
                    {
                        elts.Pop();
                    }
                }
                else
                {
                    elts.Push(e);
                }
            }

            result = elts.ToArray();
            Array.Reverse(result);

            return result;
        }
    }
    /// <summary>
    /// class for ekulist
    /// </summary>   

    public sealed class EnhancedKeyUsageProperty
    {
        private List<EnhancedKeyUsageRepresentation> _ekuList = new List<EnhancedKeyUsageRepresentation>();


        /// <summary>
        /// get property of EKUList
        /// </summary> 
        public List<EnhancedKeyUsageRepresentation> EnhancedKeyUsageList
        {
            get
            {
                return _ekuList;
            }
        }

        /// <summary>
        /// constructor for  EnhancedKeyUsageProperty
        /// </summary> 
        public EnhancedKeyUsageProperty(X509Certificate2 cert)
        {
            if (DownLevelHelper.NativeFilteringSupported())
            {
                Collection<string> ekuCollection = System.Management.Automation.Internal.SecuritySupport.GetCertEKU(cert);

                foreach (string oidString in ekuCollection)
                {
                    if (!String.IsNullOrEmpty(oidString))
                    {
                        IntPtr stringAnsi = (IntPtr)Marshal.StringToHGlobalAnsi(oidString);

                        EnhancedKeyUsageRepresentation ekuString;
                        IntPtr oidPtr = Security.NativeMethods.CryptFindOIDInfo(
                                            Security.NativeConstants.CRYPT_OID_INFO_OID_KEY,
                                            stringAnsi,
                                            0);
                        if (oidPtr != IntPtr.Zero)
                        {
                            Security.NativeMethods.CRYPT_OID_INFO oidInfo =
                                ClrFacade.PtrToStructure<Security.NativeMethods.CRYPT_OID_INFO>(oidPtr);
                            ekuString = new EnhancedKeyUsageRepresentation(oidInfo.pwszName, oidString);
                        }
                        else //if oidInfo is not available
                        {
                            ekuString = new EnhancedKeyUsageRepresentation(null, oidString);
                        }

                        _ekuList.Add(ekuString);
                    }
                }
            }
        }
    }

    /// <summary>
    /// class for DNSNameList 
    /// </summary>   

    public sealed class DnsNameProperty
    {
        private List<DnsNameRepresentation> _dnsList = new List<DnsNameRepresentation>();

        /// <summary>
        /// get property of DnsNameList
        /// </summary> 
        public List<DnsNameRepresentation> DnsNameList
        {
            get
            {
                return _dnsList;
            }
        }

        /// <summary>
        /// constructor for EkuList
        /// </summary> 
        public DnsNameProperty(X509Certificate2 cert)
        {
            if (DownLevelHelper.NativeFilteringSupported())
            {
                if (cert != null)
                {
                    //need to get subject alternative name from the certificate context
                    _dnsList = GetCertNames(
                            cert.Handle,
                            Security.NativeMethods.AltNameType.CERT_ALT_NAME_DNS_NAME);
                }
            }
        }

        // Wrapper function for CCGetCertNameList and CCFreeStringArray 
        private List<DnsNameRepresentation> GetCertNames(IntPtr certHandle, Security.NativeMethods.AltNameType nameType)
        {
            DWORD cPunycodeName;
            IntPtr papwszPunycodeNames = IntPtr.Zero;
            IntPtr papwszUnicodeNames = IntPtr.Zero;
            List<DnsNameRepresentation> names = new List<DnsNameRepresentation>();
            int hr;

            if (certHandle != IntPtr.Zero)
            {
                hr = Security.NativeMethods.CCGetCertNameList(
                            certHandle,
                            nameType,
                            0,              // no conversion to Unicode
                            out cPunycodeName,
                            out papwszPunycodeNames);
                if (hr != Security.NativeConstants.S_OK)
                {
                    if (hr != Security.NativeMethods.CRYPT_E_NOT_FOUND)
                    {
                        throw Marshal.GetExceptionForHR(hr);
                    }
                    cPunycodeName = 0;
                }

                try
                {
                    if (0 < cPunycodeName)
                    {
                        DWORD cUnicodeName;

                        hr = Security.NativeMethods.CCGetCertNameList(
                                    certHandle,
                                    nameType,
                                    Security.NativeMethods.CryptDecodeFlags.CRYPT_DECODE_ENABLE_IA5CONVERSION_FLAG,
                                    out cUnicodeName,
                                    out papwszUnicodeNames);
                        if (hr != Security.NativeConstants.S_OK)
                        {
                            throw Marshal.GetExceptionForHR(hr);
                        }
                        if (cPunycodeName != cUnicodeName)
                        {
                            throw Marshal.GetExceptionForHR(
                                        Security.NativeMethods.E_INVALID_DATA);
                        }
                        for (int i = 0; i < cPunycodeName; i++)
                        {
                            names.Add(new DnsNameRepresentation(
                                Marshal.PtrToStringUni(Marshal.ReadIntPtr(papwszPunycodeNames, i * Marshal.SizeOf(papwszPunycodeNames))),
                                Marshal.PtrToStringUni(Marshal.ReadIntPtr(papwszUnicodeNames, i * Marshal.SizeOf(papwszUnicodeNames)))));
                        }
                    }
                }
                finally
                {
                    Security.NativeMethods.CCFreeStringArray(papwszPunycodeNames);
                    Security.NativeMethods.CCFreeStringArray(papwszUnicodeNames);
                }
            }

            return names;
        }
    }


    /// <summary>
    /// downlevel helper function to determine if the OS is WIN8 and above
    /// </summary>
    internal static class DownLevelHelper
    {
        private static bool s_isWin8Set = false;
        private static bool s_isWin8 = false;

        internal static bool IsWin8AndAbove()
        {
            if (!s_isWin8Set)
            {
#if CORECLR
                s_isWin8 = true;
#else
                System.OperatingSystem osInfo = System.Environment.OSVersion;
                PlatformID platform = osInfo.Platform;
                Version version = osInfo.Version;

                if (platform.Equals(PlatformID.Win32NT) &&
                    ((version.Major > 6) ||
                     (version.Major == 6 && version.Minor >= 2)))
                {
                    s_isWin8 = true;
                }
#endif
                s_isWin8Set = true;
            }
            return s_isWin8;
        }

        private static bool s_nativeFilteringSet = false;
        private static bool s_nativeFiltering = false;

        internal static bool NativeFilteringSupported()
        {
            if (!s_nativeFilteringSet)
            {
                if (IsWin8AndAbove() && Security.NativeMethods.IsSystem32DllPresent("certca.dll"))
                {
                    s_nativeFiltering = true;
                }
                s_nativeFilteringSet = true;
            }
            return s_nativeFiltering;
        }

        private static bool s_logChangesSet = false;
        private static bool s_logChanges = false;

        internal static bool LogCertChangeSupported()
        {
            if (!s_logChangesSet)
            {
                if (IsWin8AndAbove() && Security.NativeMethods.IsSystem32DllPresent("certenroll.dll"))
                {
                    s_logChanges = true;
                }
                s_logChangesSet = true;
            }
            return s_logChanges;
        }

        internal static bool TrustedIssuerSupported()
        {
            return IsWin8AndAbove();
        }

        internal static bool HashLookupSupported()
        {
            return IsWin8AndAbove();
        }
    }

    /// <summary>
    /// Check in UI is allowed
    /// </summary>
    internal static class DetectUIHelper
    {
#if CORECLR
        internal static IntPtr GetOwnerWindow(PSHost host)
        {
            return IntPtr.Zero;
        }
#else
        [SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static IntPtr hWnd = IntPtr.Zero;
        private static bool firstRun = true;

        internal static IntPtr GetOwnerWindow(PSHost host)
        {
            if (firstRun)
            {
                firstRun = false;

                if (IsUIAllowed(host))
                {
                    hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = Security.NativeMethods.GetConsoleWindow();
                    }

                    if (hWnd == IntPtr.Zero)
                    {
                        hWnd = Security.NativeMethods.GetDesktopWindow();
                    }
                }
            }

            return hWnd;
        }

        private static bool IsUIAllowed(PSHost host)
        {
            if (host.Name.Equals("ServerRemoteHost", StringComparison.OrdinalIgnoreCase))
                return false;

            uint SessionId;
            uint ProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            if (!Security.NativeMethods.ProcessIdToSessionId(ProcessId, out SessionId))
                return false;

            if (SessionId == 0)
                return false;

            if (!Environment.UserInteractive)
                return false;

            string[] args = Environment.GetCommandLineArgs();

            bool fRet = true;
            foreach (string arg in args)
            {
                const string NonInteractiveParamName = "-noninteractive";
                if (arg.Length >= 4 && NonInteractiveParamName.StartsWith(arg, StringComparison.OrdinalIgnoreCase))
                {
                    fRet = false;
                    break;
                }
            }

            return fRet;
        }
#endif
    }

    /// <summary>
    /// container for helper functions that use pinvoke into crypt32.dll
    /// </summary>        
    internal static class Crypt32Helpers
    {
        /// <summary>
        /// lock that guards access to the following static members
        /// -- storeNames
        /// </summary>
        private static object s_staticLock = new object();

        internal static List<string> storeNames = new List<string>();

        /// <summary>
        /// get a list of store names at the specified location
        /// </summary>        
        [ArchitectureSensitive]
        internal static List<string> GetStoreNamesAtLocation(StoreLocation location)
        {
            Security.NativeMethods.CertStoreFlags locationFlag =
                Security.NativeMethods.CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;

            switch (location)
            {
                case StoreLocation.CurrentUser:
                    locationFlag = Security.NativeMethods.CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
                    break;

                case StoreLocation.LocalMachine:
                    locationFlag = Security.NativeMethods.CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
                    break;

                default:
                    Diagnostics.Assert(false, "GetStoreNamesAtLocation: invalid location value");
                    break;
            }

            Security.NativeMethods.CertEnumSystemStoreCallBackProto callBack =
                new Security.NativeMethods.CertEnumSystemStoreCallBackProto(CertEnumSystemStoreCallBack);


            // Return a new list to avoid synchronization issues.

            List<string> names = new List<string>();
            lock (s_staticLock)
            {
                storeNames.Clear();

                Security.NativeMethods.CertEnumSystemStore(locationFlag, IntPtr.Zero,
                                                  IntPtr.Zero, callBack);
                foreach (string name in storeNames)
                {
                    names.Add(name);
                }
            }

            return names;
        }


        /// <summary>
        /// call back function used by CertEnumSystemStore
        ///
        /// Currently, there is no managed support for enumerating store
        /// names on a machine. We use the win32 function CertEnumSystemStore()
        /// to get a list of stores for a given context.
        ///
        /// Each time this callback is called, we add the passed store name
        /// to the list of stores
        /// </summary>
        internal static bool CertEnumSystemStoreCallBack(string storeName,
                                                          DWORD dwFlagsNotUsed,
                                                          IntPtr notUsed1,
                                                          IntPtr notUsed2,
                                                          IntPtr notUsed3)
        {
            storeNames.Add(storeName);
            return true;
        }
    }
}
