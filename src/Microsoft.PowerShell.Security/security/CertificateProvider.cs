// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

using Dbg = System.Management.Automation;
using DWORD = System.UInt32;
using Runspaces = System.Management.Automation.Runspaces;
using Security = System.Management.Automation.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the Certificate Provider dynamic parameters.
    /// We only support one dynamic parameter for Win 7 and earlier:
    /// CodeSigningCert
    /// If provided, we only return certificates valid for signing code or
    /// scripts.
    /// </summary>
    internal sealed class CertificateProviderDynamicParameters
    {
        /// <summary>
        /// Gets or sets a switch that controls whether we only return
        /// code signing certs.
        /// </summary>
        [Parameter]
        public SwitchParameter CodeSigningCert
        {
            get { return _codeSigningCert; }

            set { _codeSigningCert = value; }
        }

        private SwitchParameter _codeSigningCert = new();

        /// <summary>
        /// Gets or sets a filter that controls whether we only return
        /// data encipherment certs.
        /// </summary>
        [Parameter]
        public SwitchParameter DocumentEncryptionCert
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a filter that controls whether we only return
        /// server authentication certs.
        /// </summary>
        [Parameter]
        public SwitchParameter SSLServerAuthentication
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a filter by DNSName.
        /// Expected content is a single DNS Name that may start and/or end
        /// with '*': "contoso.com" or "*toso.c*".
        /// All WildcardPattern class features supported.
        /// </summary>
        [Parameter]
        public string DnsName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a filter by EKU.
        /// Expected content is one or more OID strings:
        /// "1.3.6.1.5.5.7.3.1", "*Server*", etc.
        /// For a cert to match, it must be valid for all listed OIDs.
        /// All WildcardPattern class features supported.
        /// </summary>
        [Parameter]
        public string[] Eku
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a filter by the number of valid days.
        /// Expected content is a non-negative integer.
        /// "0" matches all certs that have already expired.
        /// "1" matches all certs that are currently valid and will expire
        /// by next day (local time).
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public int ExpiringInDays
        {
            get;
            set;
        } = -1;
    }

    /// <summary>
    /// Defines the type of DNS string
    /// The structure contains punycode name and unicode name.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public readonly struct DnsNameRepresentation
    {
        /// <summary>
        /// Punycode version of DNS name.
        /// </summary>
        private readonly string _punycodeName;

        /// <summary>
        /// Unicode version of DNS name.
        /// </summary>
        private readonly string _unicodeName;

        /// <summary>
        /// Ambiguous constructor of a DnsNameRepresentation.
        /// </summary>
        public DnsNameRepresentation(string inputDnsName)
        {
            _punycodeName = inputDnsName;
            _unicodeName = inputDnsName;
        }

        /// <summary>
        /// Specific constructor of a DnsNameRepresentation.
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
        /// Value comparison.
        /// </summary>
        public bool Equals(DnsNameRepresentation dnsName)
        {
            bool match = false;

            if (_unicodeName != null && dnsName._unicodeName != null)
            {
                if (string.Equals(
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
        /// Get property of Punycode.
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
        /// Get property of Unicode.
        /// </summary>
        public string Unicode
        {
            get
            {
                return _unicodeName;
            }
        }

        /// <summary>
        /// Get display string.
        /// </summary>
        public override string ToString()
        {
            // Use case sensitive comparison here.
            // We don't ever expect to see the punycode and unicode strings
            // to differ only by upper/lower case.  If they do, that's really
            // a code bug, and the effect is to just display both strings.

            return string.Equals(_punycodeName, _unicodeName, StringComparison.Ordinal)
                ? _punycodeName
                : _unicodeName + " (" + _punycodeName + ")";
        }
    }

    /// <summary>
    /// Defines the Certificate Provider remove-item dynamic parameters.
    ///
    /// Currently, we only support one dynamic parameter: DeleteKey
    /// If provided, we will delete the private key when we remove a certificate.
    /// </summary>
    internal sealed class ProviderRemoveItemDynamicParameters
    {
        /// <summary>
        /// Switch that controls whether we should delete private key
        /// when remove a certificate.
        /// </summary>
        [Parameter]
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

        private SwitchParameter _deleteKey = new();
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

            if (handle != IntPtr.Zero)
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
    /// Defines the Certificate Provider store handle class.
    /// </summary>
    internal sealed class X509NativeStore
    {
        // #region tracer

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
                        // ThrowItemNotFound(storeLocation.ToString(), CertificateProviderItem.StoreLocation);
                        break;
                }

                IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                _storeName);
                if (hCertStore == IntPtr.Zero)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                _storeHandle = new CertificateStoreHandle();
                _storeHandle.Handle = hCertStore;

                // we only do CertControlStore for stores other than UserDS
                if (!string.Equals(
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

        public IntPtr GetFirstCert()
        {
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
                certContext = Security.NativeMethods.CertEnumCertificatesInStore(
                                                    _storeHandle.Handle,
                                                    certContext);
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
                        if (certContext == IntPtr.Zero)
                        {
                            break;
                        }

                        X509Certificate2 cert = new(certContext);
                        if (string.Equals(
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
        /// Native IntPtr store handle.
        /// </summary>
        public IntPtr StoreHandle
        {
            get
            {
                return _storeHandle.Handle;
            }
        }

        /// <summary>
        /// X509StoreLocation store location.
        /// </summary>
        public X509StoreLocation Location
        {
            get
            {
                return _storeLocation;
            }
        }

        /// <summary>
        /// String store name.
        /// </summary>
        public string StoreName
        {
            get
            {
                return _storeName;
            }
        }

        /// <summary>
        /// True if a real store is open.
        /// </summary>
        public bool Valid
        {
            get
            {
                return _valid;
            }
        }

        private bool _archivedCerts = false;
        private readonly X509StoreLocation _storeLocation = null;
        private readonly string _storeName = null;
        private CertificateStoreHandle _storeHandle = null;
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
        /// For example, cert:\CurrentUser.
        /// </summary>
        Store,

        /// <summary>
        /// A certificate store.
        /// For example, cert:\CurrentUser\My.
        /// </summary>
        StoreLocation
    }

    /// <summary>
    /// Defines the implementation of a Certificate Store Provider.  This provider
    /// allows for stateless namespace navigation of the computer's certificate
    /// store.
    /// </summary>
    [CmdletProvider("Certificate", ProviderCapabilities.ShouldProcess)]
    [OutputType(typeof(string), typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.ResolvePath)]
    [OutputType(typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.PushLocation)]
    [OutputType(typeof(Microsoft.PowerShell.Commands.X509StoreLocation), typeof(X509Certificate2), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(X509Store), typeof(X509Certificate2), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    public sealed class CertificateProvider : NavigationCmdletProvider, ICmdletProviderSupportsHelp
    {
        #region tracer

        /// <summary>
        /// Tracer for certificate provider.
        /// </summary>
        [TraceSource("CertificateProvider",
                      "The core command provider for certificates")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("CertificateProvider",
                      "The core command provider for certificates");

        #endregion tracer

        /// <summary>
        /// Indicate if we already have attempted to load the PKI module.
        /// </summary>
        private bool _hasAttemptedToLoadPkiModule = false;

        /// <summary>
        /// Lock that guards access to the following static members
        /// -- storeLocations
        /// -- pathCache.
        /// </summary>
        private static readonly object s_staticLock = new();

        /// <summary>
        /// List of store locations. They do not change once initialized.
        ///
        /// Synchronized on staticLock.
        /// </summary>
        private static List<X509StoreLocation> s_storeLocations = null;

        /// <summary>
        /// Cache that stores paths and their associated objects.
        ///
        /// key is full path to store-location/store/certificate
        /// value is X509StoreLocation/X509NativeStore/X509Certificate2 object
        ///
        /// Synchronized on staticLock.
        /// </summary>
        private static Hashtable s_pathCache = null;

        /// <summary>
        /// We allow either / or \ to be the path separator.
        /// </summary>
        private static readonly char[] s_pathSeparators = new char[] { '/', '\\' };

        /// <summary>
        /// Regex pattern that defines a valid cert path.
        /// </summary>
        private const string certPathPattern = @"^\\((?<StoreLocation>CurrentUser|LocalMachine)(\\(?<StoreName>[a-zA-Z]+)(\\(?<Thumbprint>[0-9a-f]{40}))?)?)?$";

        /// <summary>
        /// Cache the store handle to avoid repeated CertOpenStore calls.
        /// </summary>
        private static X509NativeStore s_storeCache = null;

        /// <summary>
        /// On demand create the Regex to avoid a hit to startup perf.
        /// </summary>
        /// <remarks>
        /// Note, its OK that staticLock is being used here because only
        /// IsValidPath is calling this static property so we shouldn't
        /// have any deadlocks due to other locked static members calling
        /// this property.
        /// </remarks>
        private static Regex s_certPathRegex = null;

        private static Regex CertPathRegex
        {
            get
            {
                lock (s_staticLock)
                {
                    if (s_certPathRegex == null)
                    {
                        const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
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
                    X509StoreLocation user = new(StoreLocation.CurrentUser);
                    s_storeLocations.Add(user);
                    AddItemToCache(nameof(StoreLocation.CurrentUser),
                                  user);

                    //
                    // create and cache LocalMachine store-location
                    //
                    X509StoreLocation machine = new(StoreLocation.LocalMachine);
                    s_storeLocations.Add(machine);
                    AddItemToCache(nameof(StoreLocation.LocalMachine),
                                   machine);

                    AddItemToCache(string.Empty, s_storeLocations);
                }
            }
        }

        /// <summary>
        /// Removes an item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the item to remove.
        /// </param>
        /// <param name="recurse">
        /// Recursively remove.
        /// </param>
        /// <returns>
        /// Nothing.
        /// </returns>
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

            bool fUserContext = string.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase);

            // isContainer = true means not a valid certificate

            // if source store is user root store and UI is not allowed
            // we raise invalid operation
            if (DetectUIHelper.GetOwnerWindow(Host) == IntPtr.Zero && fUserContext &&
                 string.Equals(pathElements[1], "ROOT", StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.UINotAllowed;
                const string errorId = "UINotAllowed";
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
                if (pathElements.Length == 2) // is a store
                {
                    // not support user context
                    if (fUserContext)
                    {
                        string message = CertificateProviderStrings.CannotDeleteUserStore;
                        const string errorId = "CannotDeleteUserStore";
                        ThrowInvalidOperation(errorId, message);
                    }

                    RemoveCertStore(pathElements[1], fDeleteKey, path);
                    return;
                }
                else // other container than a store
                {
                    string message = CertificateProviderStrings.CannotRemoveContainer;
                    const string errorId = "CannotRemoveContainer";
                    ThrowInvalidOperation(errorId, message);
                }
            }
            else // certificate
            {
                // do remove
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
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// <param name="recurse">
        /// Ignored.
        /// </param>
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
        /// <param name="path">
        /// The path of the item to move.
        /// </param>
        /// <param name="destination">
        /// The path of the destination.
        /// </param>
        /// <returns>
        /// Nothing.  Moved items are written to the context's pipeline.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        ///     destination is null or empty.
        /// </exception>
        protected override void MoveItem(
                                string path,
                                string destination)
        {
            // normalize path
            path = NormalizePath(path);
            destination = NormalizePath(destination);

            // get elements from the path
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
                const string errorId = "CannotMoveContainer";
                ThrowInvalidOperation(errorId, message);
            }

            if (destElements.Length != 2) // not a store
            {
                // if the destination leads to the same thumbprint
                if (destElements.Length == 3 &&
                   (string.Equals(pathElements[2], destElements[2], StringComparison.OrdinalIgnoreCase)))
                {
                    // in this case we think of destination path as valid
                    // and strip the thumbprint part
                    destination = Path.GetDirectoryName(destination);
                }
                else
                {
                    string message = CertificateProviderStrings.InvalidDestStore;
                    const string errorId = "InvalidDestStore";
                    ThrowInvalidOperation(errorId, message);
                }
            }

            // the second element is store location
            // we do not allow cross context move
            // we do not allow the destination store is the same as source

            if (!string.Equals(pathElements[0], destElements[0], StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.CannotMoveCrossContext;
                const string errorId = "CannotMoveCrossContext";
                ThrowInvalidOperation(errorId, message);
            }

            if (string.Equals(pathElements[1], destElements[1], StringComparison.OrdinalIgnoreCase))
            {
                string message = CertificateProviderStrings.CannotMoveToSameStore;
                const string errorId = "CannotMoveToSameStore";
                ThrowInvalidOperation(errorId, message);
            }

            // if source or destination store is user root store and UI is not allowed
            // we raise invalid operation
            if (DetectUIHelper.GetOwnerWindow(Host) == IntPtr.Zero)
            {
                if ((string.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(pathElements[1], "ROOT", StringComparison.OrdinalIgnoreCase)) ||
                     (string.Equals(destElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(destElements[1], "ROOT", StringComparison.OrdinalIgnoreCase)))
                {
                    string message = CertificateProviderStrings.UINotAllowed;
                    const string errorId = "UINotAllowed";
                    ThrowInvalidOperation(errorId, message);
                }
            }

            if (cert != null) // we get cert
            {
                // get destination store
                bool isDestContainer = false;
                object store = GetItemAtPath(destination, false, out isDestContainer);

                X509Certificate2 certificate = cert as X509Certificate2;
                X509NativeStore certstore = store as X509NativeStore;

                if (certstore != null)
                {
                    certstore.Open(true);

                    string action = CertificateProviderStrings.Action_Move;
                    string resource = string.Format(
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
        /// <remarks>
        /// New-Item doesn't go through the method "ItemExists". But for the
        /// CertificateProvider, New-Item can create an X509Store and return
        /// it, and the user can access the certificates within the store via its
        /// property "Certificates". We want the extra new properties of the
        /// X509Certificate2 objects to be shown to the user, so we also need
        /// to import the PKI module in this method, if we haven't tried it yet.
        /// </remarks>
        /// <param name="path">
        /// The path of the certificate store to create.
        /// </param>
        ///<param name="type">
        /// Ignored.
        /// Only support store.
        /// </param>
        /// <param name="value">
        /// Ignored
        /// </param>
        /// <returns>
        /// Nothing.  The new certificate store object is
        /// written to the context's pipeline.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        /// </exception>
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

            // get the elements from the path
            string[] pathElements = GetPathElements(path);

            // only support creating store
            if (pathElements.Length != 2)
            {
                string message = CertificateProviderStrings.CannotCreateItem;
                const string errorId = "CannotCreateItem";
                ThrowInvalidOperation(errorId, message);
            }

            bool fUserContext = string.Equals(pathElements[0], "CurrentUser", StringComparison.OrdinalIgnoreCase);

            // not support user context
            if (fUserContext)
            {
                string message = CertificateProviderStrings.CannotCreateUserStore;
                const string errorId = "CannotCreateUserStore";
                ThrowInvalidOperation(errorId, message);
            }

            const Security.NativeMethods.CertOpenStoreFlags StoreFlags =
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_CREATE_NEW_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG |
                    Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;

            // Create new store
            IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                pathElements[1]);
            if (hCertStore == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            else // free native store handle
            {
                bool fResult = false;
                fResult = Security.NativeMethods.CertCloseStore(hCertStore, 0);
            }

            X509Store outStore = new(pathElements[1], StoreLocation.LocalMachine);
            WriteItemObject(outStore, path, true);
        }

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Initializes the cert: drive.
        /// </summary>
        /// <returns>
        /// A collection that contains the PSDriveInfo object
        /// that represents the cert: drive.
        /// </returns>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            string providerDescription = CertificateProviderStrings.CertProvidername;

            PSDriveInfo drive = new(
                name: "Cert",
                provider: ProviderInfo,
                root: @"\",
                providerDescription,
                credential: null);

            Collection<PSDriveInfo> drives = new();
            drives.Add(drive);

            return drives;
        }

        /// <summary>
        /// Determines if the item at the given path is a store-location
        /// or store with items in it.
        /// </summary>
        /// <param name="path">
        /// The full path to the item.
        /// </param>
        /// <returns>
        /// True if the path refers to a store location, or store that contains
        /// certificates.  False otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
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
                        IntPtr certContext = store.GetFirstCert();
                        if (certContext != IntPtr.Zero)
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
        ///     cert:\CurrentUser\My\5F98EBBFE735CDDAE00E33E0FD69050EF9220254.
        /// </summary>
        /// <param name="path">
        /// The path of the item to check.
        /// </param>
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
        /// <param name="path">
        /// The path of the item to check.
        /// </param>
        /// <returns>
        /// True if a the store location, store, or certificate exists
        /// at the specified path.  False otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
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
                    if (e.InnerException is not CertificateProviderItemNotFoundException)
                    {
                        throw;
                    }
                }

                result = (bool)item;
            }

            s_tracer.WriteLine("result = {0}", result);
            return result;
        }

        /// <summary>
        /// Gets the store location, store, or certificate
        /// at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path of the item to retrieve.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null
        /// </exception>
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
        protected override void GetItem(string path)
        {
            bool isContainer = false;

            path = NormalizePath(path);
            object item = GetItemAtPath(path, false, out isContainer);
            CertificateFilterInfo filter = GetFilter();

            if (item != null)
            {
                if (!isContainer) // certificate
                {
                    // If the filter is null, output the certificate we got.
                    if (filter == null)
                    {
                        WriteItemObject(item, path, isContainer);
                    }
                    else
                    {
                        // The filter is non null. If the certificate
                        // satisfies the filter, output it. Otherwise, don't.
                        X509Certificate2 cert = item as X509Certificate2;
                        Dbg.Diagnostics.Assert(cert != null, "item should be a certificate");

                        if (MatchesFilter(cert, filter))
                        {
                            WriteItemObject(item, path, isContainer);
                        }
                    }
                }
                else  // container
                {
                    // The item is a container. If the filter is non null, we don't output it.
                    if (filter != null)
                    {
                        return;
                    }

                    X509StoreLocation storeLocation = item as X509StoreLocation;
                    if (storeLocation != null)  // store location
                    {
                        WriteItemObject(item, path, isContainer);
                    }
                    else // store
                    {
                        X509NativeStore store = item as X509NativeStore;
                        if (store != null)
                        {
                            // create X509Store
                            X509Store outStore = new(store.StoreName, store.Location.Location);
                            WriteItemObject(outStore, path, isContainer);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the parent of the given path.
        /// </summary>
        /// <param name="path">
        /// The path of which to get the parent.
        /// </param>
        /// <param name="root">
        /// The root of the drive.
        /// </param>
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
        /// <param name="path">
        /// The fully qualified path to the item.
        /// </param>
        /// <returns>
        /// The leaf element of the specified path.
        /// </returns>
        protected override string GetChildName(string path)
        {
            // Path for root is empty string
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
            Runspaces.Command importModuleCommand = new(commandInfo);

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
            catch (Exception)
            {
            }

            _hasAttemptedToLoadPkiModule = true;
        }

        private static string MyGetChildName(string path)
        {
            // Verify the parameters

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
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
        /// <param name="path">
        /// Ignored.
        /// </param>
        protected override void InvokeDefaultAction(string path)
        {
            path = NormalizePath(path);
            string action = CertificateProviderStrings.Action_Invoke;
            const string certmgr = "certmgr.msc";
            string certPath = System.IO.Path.Combine(
                System.Environment.ExpandEnvironmentVariables("%windir%"), "system32");

            if (ShouldProcess(path, action))
            {
                System.Diagnostics.Process.Start(System.IO.Path.Combine(certPath, certmgr));
            }
        }

        private static string EnsureDriveIsRooted(string path)
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
        }

        private static ErrorRecord CreateErrorRecord(string path,
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

            message = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                message, path);
            ErrorDetails ed = new(message);

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

            ErrorRecord er = new(
                e,
                "CertProviderItemNotFound",
                ErrorCategory.ObjectNotFound,
                targetObject: null);

            er.ErrorDetails = ed;

            return er;
        }

        private void ThrowErrorRemoting(int stat)
        {
            if (this.Host.Name.Equals("ServerRemoteHost", StringComparison.OrdinalIgnoreCase))
            {
                Exception e = new System.ComponentModel.Win32Exception(stat);
                string error = e.Message;
                string message = CertificateProviderStrings.RemoteErrorMessage;
                error += message;

                Exception e2 = new(error);
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
            ErrorRecord errorRecord = new(
                new InvalidOperationException(message),
                errorId,
                ErrorCategory.InvalidOperation,
                targetObject: null);
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

        private static string NormalizePath(string path)
        {
            if (path.Length > 0)
            {
                char lastChar = path[path.Length - 1];

                if ((lastChar == '/') || (lastChar == '\\'))
                {
                    path = path.Substring(0, path.Length - 1);
                }

                string[] elts = GetPathElements(path);

                path = string.Join("\\", elts);
            }

            return path;
        }

        private static string[] GetPathElements(string path)
        {
            string[] allElts = path.Split(s_pathSeparators);
            string[] result = null;

            Stack<string> elts = new();

            foreach (string e in allElts)
            {
                if ((e == ".") || (e == string.Empty))
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
        /// Delete private key.
        /// </summary>
        /// <param name="pProvInfo">Key prov info.</param>
        /// <returns>No return.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.Automation.Security.NativeMethods.NCryptSetProperty(System.IntPtr,System.String,System.Void*,System.Int32,System.Int32)")]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.Automation.Security.NativeMethods.NCryptFreeObject(System.IntPtr)")]
        private void DoDeleteKey(IntPtr pProvInfo)
        {
            IntPtr hProv = IntPtr.Zero;
            Security.NativeMethods.CRYPT_KEY_PROV_INFO keyProvInfo =
                Marshal.PtrToStructure<Security.NativeMethods.CRYPT_KEY_PROV_INFO>(pProvInfo);

            IntPtr hWnd = DetectUIHelper.GetOwnerWindow(Host);

            if (keyProvInfo.dwProvType != 0) // legacy
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
            else  // cng key
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

                    if ((cngKeyFlag & (uint)Security.NativeMethods.NCryptDeletKeyFlag.NCRYPT_SILENT_FLAG) != 0)
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
        /// the associated private key.
        /// </summary>
        /// <param name="storeName">The store name.</param>
        /// <param name="fDeleteKey">Boolean to specify whether or not to delete private key.</param>
        /// <param name = "sourcePath">Source path.</param>
        /// <returns>No return.</returns>
        private void RemoveCertStore(string storeName, bool fDeleteKey, string sourcePath)
        {
            // if recurse is true, remove every cert in the store
            IntPtr localName = Security.NativeMethods.CryptFindLocalizedName(storeName);
            string[] pathElements = GetPathElements(sourcePath);
            if (localName == IntPtr.Zero)//not find, we can remove
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
                IntPtr certContext = store.GetFirstCert();
                while (certContext != IntPtr.Zero)
                {
                    X509Certificate2 cert = new(certContext);
                    string certPath = sourcePath + cert.Thumbprint;
                    RemoveCertItem(cert, fDeleteKey, true, certPath);

                    certContext = store.GetNextCert(certContext);
                }
                // remove the cert store
                const Security.NativeMethods.CertOpenStoreFlags StoreFlags =
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_READONLY_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_STORE_DELETE_FLAG |
                        Security.NativeMethods.CertOpenStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;

                // delete store
                IntPtr hCertStore = Security.NativeMethods.CertOpenStore(
                                Security.NativeMethods.CertOpenStoreProvider.CERT_STORE_PROV_SYSTEM,
                                Security.NativeMethods.CertOpenStoreEncodingType.X509_ASN_ENCODING,
                                IntPtr.Zero,  // hCryptProv
                                StoreFlags,
                                storeName);
            }
            else
            {
                string message = string.Format(
                                        CultureInfo.CurrentCulture,
                                        CertificateProviderStrings.RemoveStoreTemplate,
                                        storeName);
                const string errorId = "CannotRemoveSystemStore";
                ThrowInvalidOperation(errorId, message);
            }
        }
        /// <summary>
        /// Delete the a single cert from the store; if -DeleteKey is specified, we also delete
        /// the associated private key.
        /// </summary>
        /// <param name="cert">An X509Certificate2 object.</param>
        /// <param name="fDeleteKey">Boolean to specify whether or not to delete private key.</param>
        /// <param name="fMachine">Machine context or user.</param>
        /// <param name = "sourcePath">Source path.</param>
        /// <returns>No return.</returns>
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

                string resource = string.Format(
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
        /// the associated private key.
        /// </summary>
        /// <param name="cert">An X509Certificate2 object.</param>
        /// <param name="fDeleteKey">Boolean to specify whether or not to delete private key.</param>
        /// <param name="fMachine">Machine context or user.</param>
        /// <param name = "sourcePath">Source path.</param>
        /// <returns>No return.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private void DoRemove(X509Certificate2 cert, bool fDeleteKey, bool fMachine, string sourcePath)
        {
            // get CERT_KEY_PROV_INFO_PROP_ID
            int provSize = 0;
            IntPtr pProvInfo = IntPtr.Zero;
            bool fHasPrivateKey = false;

            try
            {
                if (fDeleteKey)
                {
                    // it is fine if below call fails
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
                        // raise a verbose message
                        // we should not use WriteWarning here
                        string verboseNoPrivatekey = CertificateProviderStrings.VerboseNoPrivateKey;
                        WriteVerbose(verboseNoPrivatekey);
                    }
                }

                // do remove certificate
                // should not use the original handle

                if (!Security.NativeMethods.CertDeleteCertificateFromStore(
                            Security.NativeMethods.CertDuplicateCertificateContext(cert.Handle)))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                // commit the change to physical store
                if (sourcePath.Contains("UserDS"))
                {
                    Security.NativeMethods.CERT_CONTEXT context =
                        Marshal.PtrToStructure<Security.NativeMethods.CERT_CONTEXT>(cert.Handle);

                    CommitUserDS(context.hCertStore);
                }

                // TODO: Log Cert Delete

                // delete private key
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
        /// Commit store for UserDS store.
        /// </summary>
        /// <param name="storeHandle">An IntPtr for store handle.</param>
        /// <returns>No return.</returns>
        private static void CommitUserDS(IntPtr storeHandle)
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
        /// Delete the cert from the original store and add to the destination store.
        /// </summary>
        /// <param name="destination">Destination path.</param>
        /// <param name="cert">An X509Certificate2.</param>
        /// <param name="store">An X509NativeStore.</param>
        /// <param name="sourcePath">Source path.</param>
        /// <returns>No return.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
        private void DoMove(string destination, X509Certificate2 cert, X509NativeStore store, string sourcePath)
        {
            IntPtr dupCert = IntPtr.Zero;  // should not free this
            IntPtr outCert = IntPtr.Zero;

            // duplicate cert first
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

                // TODO: log cert move
            }

            // commit the change to physical store
            if (destination.Contains("UserDS"))
            {
                CommitUserDS(store.StoreHandle);
            }

            if (sourcePath.Contains("UserDS"))
            {
                Security.NativeMethods.CERT_CONTEXT context = Marshal.PtrToStructure<Security.NativeMethods.CERT_CONTEXT>(cert.Handle);

                CommitUserDS(context.hCertStore);
            }

            // get the output object
            X509Certificate2 outObj = new(outCert);
            string certName = GetCertName(outObj);
            string certPath = MakePath(destination, certName);
            WriteItemObject((object)outObj, certPath, false);
        }

        /// <summary>
        /// Fetches the store-location/store/certificate at the
        /// specified path.
        /// </summary>
        /// <param name="path">Path to the item.</param>
        /// <param name="test">True if this is to only for an ItemExists call. Returns True / False.</param>
        /// <param name="isContainer">Set to true if item exists and is a container.</param>
        /// <returns>Item at the path.</returns>
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
            isContainer = pathElements.Length <= 2;

            X509NativeStore store = null;

            //
            // handle invalid path depth
            //
            if (pathElements.Length > 3)
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
                        string storePath = GetParentPath(path, string.Empty);
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
                        if (certContext == IntPtr.Zero)
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
        }

        /// <summary>
        /// Gets the child items of a given store, or location.
        /// </summary>
        /// <param name="path">
        /// The full path of the store or location to enumerate.
        /// </param>
        /// <param name="recurse">
        /// If true, recursively enumerates the child items as well.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
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
        protected override void GetChildItems(string path, bool recurse)
        {
            path = NormalizePath(path);

            GetChildItemsOrNames(path, recurse, ReturnContainers.ReturnAllContainers, false, GetFilter());
        }

        /// <summary>
        /// Gets the child names of a given store, or location.
        /// </summary>
        /// <param name="path">
        /// The full path of the store or location to enumerate.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
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
        protected override void GetChildNames(
            string path,
            ReturnContainers returnContainers)
        {
            path = NormalizePath(path);
            GetChildItemsOrNames(path, false, returnContainers, true, GetFilter());
        }

        /// <summary>
        /// Determines if the item at the specified path is a store
        /// or location.
        /// </summary>
        /// <returns>
        /// True if the item at the specified path is a store or location.
        /// False otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Path is null or empty.
        /// </exception>
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
        }

        /// <summary>
        /// Gets the dynamic parameters for get-item on the Certificate
        /// Provider.  We currently support the following dynamic parameters:
        /// "CodeSigning," that returns only certificates good for signing
        /// code or scripts.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        protected override object GetItemDynamicParameters(string path)
        {
            return new CertificateProviderDynamicParameters();
        }

        /// <summary>
        /// Gets the dynamic parameters for get-childitem on the Certificate
        /// Provider.  We currently only support one dynamic parameter,
        /// "CodeSigning," that returns only certificates good for signing
        /// code or scripts.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item for which to get the dynamic parameters.
        /// </param>
        /// <param name="recurse">
        /// Ignored.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            return new CertificateProviderDynamicParameters();
        }

        #endregion DriveCmdletProvider overrides

        #region private members

        /// <summary>
        /// Helper function to get store-location/store/cert at
        /// the specified path.
        /// </summary>
        /// <param name="path">Path to the item.</param>
        /// <param name="recurse">Whether we need to recursively find all.</param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="returnNames">Whether we only need the names.</param>
        /// <param name="filter">Filter info.</param>
        /// <returns> Does not return a value.</returns>
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
        /// Get the name of the specified certificate.
        /// </summary>
        /// <param name="cert"></param>
        /// <returns>Cert name .</returns>
        /// <remarks> we use Thumbprint as the name  </remarks>
        private static string GetCertName(X509Certificate2 cert)
        {
            return cert.Thumbprint;
        }

        /// <summary>
        /// Get cert objects or their name at the specified path.
        /// </summary>
        /// <param name="path">Path to cert.</param>
        /// <param name="pathElements">Path elements.</param>
        /// <param name="returnNames">Whether we should return only the names (instead of objects).</param>
        /// <param name="filter">Filter info.</param>
        /// <returns>Does not return a value.</returns>
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
            IntPtr certContext = store.GetFirstCert();

            while (certContext != IntPtr.Zero)
            {
                X509Certificate2 cert = new(certContext);

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
                        PSObject myPsObj = new(cert);
                        thingToReturn = (object)myPsObj;
                    }

                    WriteItemObject(thingToReturn, certPath, false);
                }

                certContext = store.GetNextCert(certContext);
            }
        }

        /// <summary>
        /// Get X509StoreLocation object at path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>X509StoreLocation object.</returns>
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
        /// Get the X509NativeStore object at path.
        /// </summary>
        /// <param name="path">Path to store.</param>
        /// <param name="test">True if this should be a test for path existence. Returns True or False.</param>
        /// <param name="pathElements">Path elements.</param>
        /// <returns>X509NativeStore object.</returns>
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
        /// Gets the X509NativeStore at the specified path.
        /// Adds to cache if not already there.
        /// </summary>
        /// <param name="storePath">Path to the store.</param>
        /// <param name="storeName">Name of store (path leaf element).</param>
        /// <param name="storeLocation">Location of store (CurrentUser or LocalMachine).</param>
        /// <returns>X509NativeStore object.</returns>
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
                    !string.Equals(
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
        /// Gets X509NativeStore objects or their name at the specified path.
        /// </summary>
        /// <param name="path">Path to the store.</param>
        /// <param name="recurse">Recursively return all items if true.</param>
        /// <param name="returnNames"></param>
        /// <param name="filter">Filter info.</param>
        /// <returns> Does not return a value.</returns>
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
                    X509Store ManagedStore = new(store.StoreName, store.Location.Location);
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
                CertificateProviderDynamicParameters dp =
                    DynamicParameters as CertificateProviderDynamicParameters;
                if (dp != null)
                {
                    if (dp.CodeSigningCert)
                    {
                        filter = new CertificateFilterInfo();
                        filter.Purpose = CertificatePurpose.CodeSigning;
                    }

                    if (dp.DocumentEncryptionCert)
                    {
                        filter ??= new CertificateFilterInfo();
                        filter.Purpose = CertificatePurpose.DocumentEncryption;
                    }

                    if (dp.DnsName != null)
                    {
                        filter ??= new CertificateFilterInfo();
                        filter.DnsName = new WildcardPattern(dp.DnsName, WildcardOptions.IgnoreCase);
                    }

                    if (dp.Eku != null)
                    {
                        filter ??= new CertificateFilterInfo();
                        filter.Eku = new List<WildcardPattern>();
                        foreach (var pattern in dp.Eku)
                        {
                            filter.Eku.Add(new WildcardPattern(pattern, WildcardOptions.IgnoreCase));
                        }
                    }

                    if (dp.ExpiringInDays >= 0)
                    {
                        filter ??= new CertificateFilterInfo();
                        filter.Expiring = DateTime.Now.AddDays(dp.ExpiringInDays);
                    }

                    if (dp.SSLServerAuthentication)
                    {
                        filter ??= new CertificateFilterInfo();
                        filter.SSLServerAuthentication = true;
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

        private static bool MatchesFilter(X509Certificate2 cert, CertificateFilterInfo filter)
        {
            // No filter means, match everything
            if (filter == null)
            {
                return true;
            }

            if (filter.Expiring > DateTime.MinValue && !SecuritySupport.CertExpiresByTime(cert, filter.Expiring))
            {
                return false;
            }

            if (filter.DnsName != null && !CertContainsName(cert, filter.DnsName))
            {
                return false;
            }

            if (filter.Eku != null && !CertContainsEku(cert, filter.Eku))
            {
                return false;
            }

            if (filter.SSLServerAuthentication && !CertIsSSLServerAuthentication(cert))
            {
                return false;
            }

            switch (filter.Purpose)
            {
                case CertificatePurpose.CodeSigning:
                    return SecuritySupport.CertIsGoodForSigning(cert);

                case CertificatePurpose.DocumentEncryption:
                    return SecuritySupport.CertIsGoodForEncryption(cert);

                case CertificatePurpose.NotSpecified:
                case CertificatePurpose.All:
                    return true;

                default:
                    break;
            }

            return false;
        }

        /// <summary>
        /// Check if the specified certificate has the name in DNS name list.
        /// </summary>
        /// <param name="cert">Certificate object.</param>
        /// <param name="pattern">Wildcard pattern for DNS name to search.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertContainsName(X509Certificate2 cert, WildcardPattern pattern)
        {
            List<DnsNameRepresentation> list = (new DnsNameProperty(cert)).DnsNameList;
            foreach (DnsNameRepresentation dnsName in list)
            {
                if (pattern.IsMatch(dnsName.Unicode))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the specified certificate is a server authentication certificate.
        /// </summary>
        /// <param name="cert">Certificate object.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertIsSSLServerAuthentication(X509Certificate2 cert)
        {
            X509ExtensionCollection extentionList = cert.Extensions;
            foreach (var extension in extentionList)
            {
                if (extension is X509EnhancedKeyUsageExtension eku)
                {
                    foreach (Oid usage in eku.EnhancedKeyUsages)
                    {
                        if (usage.Value.Equals(CertificateFilterInfo.OID_PKIX_KP_SERVER_AUTH, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the specified certificate contains EKU matching all of these patterns.
        /// </summary>
        /// <param name="cert">Certificate object.</param>
        /// <param name="ekuPatterns">EKU patterns.</param>
        /// <returns>True on success, false otherwise.</returns>
        internal static bool CertContainsEku(X509Certificate2 cert, List<WildcardPattern> ekuPatterns)
        {
            X509ExtensionCollection extensionList = cert.Extensions;
            foreach (var extension in extensionList)
            {
                if (extension is X509EnhancedKeyUsageExtension eku)
                {
                    OidCollection enhancedKeyUsages = eku.EnhancedKeyUsages;
                    foreach (WildcardPattern ekuPattern in ekuPatterns)
                    {
                        const bool patternPassed = false;
                        foreach (var usage in enhancedKeyUsages)
                        {
                            if (ekuPattern.IsMatch(usage.Value) || ekuPattern.IsMatch(usage.FriendlyName))
                            {
                                return true;
                            }
                        }

                        if (!patternPassed)
                        {
                            return false;
                        }
                    }

                    return true;
                }
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
        /// Get provider-specific help.
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
                if (!string.IsNullOrEmpty(helpItemName))
                {
                    CmdletInfo.SplitCmdletName(helpItemName, out verb, out noun);
                }
                else
                {
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(verb) || string.IsNullOrEmpty(noun))
                {
                    return string.Empty;
                }

                //
                // Load the help file from the current UI culture subfolder of the module's root folder
                //
                XmlDocument document = new();

                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

                string fullHelpPath = Path.Combine(
                    this.ProviderInfo.ApplicationBase,
                    currentUICulture.ToString(),
                    this.ProviderInfo.HelpFile);
                XmlReaderSettings settings = new();
                settings.XmlResolver = null;
                using (XmlReader reader = XmlReader.Create(fullHelpPath, settings))
                {
                    document.Load(reader);
                }

                // Add "msh" and "command" namespaces from the MAML schema
                XmlNamespaceManager nsMgr = new(document.NameTable);
                nsMgr.AddNamespace("msh", HelpCommentsParser.mshURI);
                nsMgr.AddNamespace("command", HelpCommentsParser.commandURI);

                // Compose XPath query to select the appropriate node based on the cmdlet
                string xpathQuery = string.Format(
                    CultureInfo.InvariantCulture,
                    HelpCommentsParser.ProviderHelpCommandXPath,
                    string.Empty,
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
                return string.Empty;
            }
            catch (PathTooLongException)
            {
                return string.Empty;
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }
            catch (SecurityException)
            {
                return string.Empty;
            }
            catch (XPathException)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        #endregion
    }

    /// <summary>
    /// Defines a class to represent a store location in the certificate
    /// provider.  The two possible store locations are CurrentUser and
    /// LocalMachine.
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
        /// <see cref="System.Security.Cryptography.X509Certificates.StoreLocation"/>
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
                // always try to get new names
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
    /// The structure contains friendly name and EKU oid.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public readonly struct EnhancedKeyUsageRepresentation
    {
        /// <summary>
        /// Localized friendly name of EKU.
        /// </summary>
        private readonly string _friendlyName;

        /// <summary>
        /// OID of EKU.
        /// </summary>
        private readonly string _oid;

        /// <summary>
        /// Constructor of an EnhancedKeyUsageRepresentation.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Oid")]

        public EnhancedKeyUsageRepresentation(string inputFriendlyName, string inputOid)
        {
            _friendlyName = inputFriendlyName;
            _oid = inputOid;
        }

        /// <summary>
        /// Value comparison.
        /// </summary>
        public bool Equals(EnhancedKeyUsageRepresentation keyUsage)
        {
            bool match = false;

            if (_oid != null && keyUsage._oid != null)
            {
                // OID strings only contain numbers and periods

                if (string.Equals(_oid, keyUsage._oid, StringComparison.Ordinal))
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
        /// Get property of friendlyName.
        /// </summary>
        public string FriendlyName
        {
            get
            {
                return _friendlyName;
            }
        }

        /// <summary>
        /// Get property of oid.
        /// </summary>
        public string ObjectId
        {
            get
            {
                return _oid;
            }
        }

        /// <summary>
        /// Get display string.
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(_friendlyName) ?
                        _oid :
                        _friendlyName + " (" + _oid + ")";
        }
    }

    /// <summary>
    /// Class for SendAsTrustedIssuer.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors")]
    public sealed class SendAsTrustedIssuerProperty
    {
        /// <summary>
        /// Get property of SendAsTrustedIssuer.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool ReadSendAsTrustedIssuerProperty(X509Certificate2 cert)
        {
            bool fHasProperty = false;
            if (DownLevelHelper.TrustedIssuerSupported())
            {
                int propSize = 0;
                // try to get the property
                // it is fine if fail for not there
                if (Security.NativeMethods.CertGetCertificateContextProperty(
                                cert.Handle,
                                Security.NativeMethods.CertPropertyId.CERT_SEND_AS_TRUSTED_ISSUER_PROP_ID,
                                IntPtr.Zero,
                                ref propSize))
                {
                    // we have the property
                    fHasProperty = true;
                }
                else
                {
                    // if fail
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
        /// Set property of SendAsTrustedIssuer.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void WriteSendAsTrustedIssuerProperty(X509Certificate2 cert, string certPath, bool addProperty)
        {
            if (DownLevelHelper.TrustedIssuerSupported())
            {
                IntPtr propertyPtr = IntPtr.Zero;
                Security.NativeMethods.CRYPT_DATA_BLOB dataBlob = new();
                dataBlob.cbData = 0;
                dataBlob.pbData = IntPtr.Zero;
                X509Certificate certFromStore = null;

                try
                {
                    if (certPath != null)
                    {
                        // try to open the store and get the cert out
                        // in case the store handle is already released
                        string[] pathElements = GetPathElements(certPath);

                        // certpath is in the format: Microsoft.Powershell.Security\
                        // Certificate::CurrentUser(LocalMachine)\my\HashID
                        // obtained pathElements[0] is Microsoft.Powershell.Security
                        // obtained pathElements[1] is Certificate::CurrentUser
                        // obtained pathElements[2] is MY
                        // obtained pathElements[3] is HashID

                        bool fUserContext = string.Equals(pathElements[1], "Certificate::CurrentUser", StringComparison.OrdinalIgnoreCase);

                        X509StoreLocation storeLocation =
                            new(fUserContext ? StoreLocation.CurrentUser : StoreLocation.LocalMachine);

                        // get certificate from the store pathElements[2]
                        X509NativeStore store = null;

                        store = new X509NativeStore(storeLocation, pathElements[2]);
                        store.Open(true); // including archival flag

                        IntPtr certContext = store.GetCertByName(pathElements[3]);

                        if (certContext != IntPtr.Zero)
                        {
                            certFromStore = new X509Certificate2(certContext);
                            store.FreeCert(certContext);
                        }
                    }

                    if (addProperty) // should add the property
                    {
                        propertyPtr = Marshal.AllocHGlobal(Marshal.SizeOf(dataBlob));
                        Marshal.StructureToPtr(dataBlob, propertyPtr, false);
                    }

                    // set property
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
                Marshal.ThrowExceptionForHR(Security.NativeMethods.NTE_NOT_SUPPORTED);
            }
        }

        private static readonly char[] s_separators = new char[] { '/', '\\' };

        private static string[] GetPathElements(string path)
        {
            string[] allElts = path.Split(s_separators);
            string[] result = null;

            Stack<string> elts = new();

            foreach (string e in allElts)
            {
                if ((e == ".") || (e == string.Empty))
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
    /// Class for ekulist.
    /// </summary>
    public sealed class EnhancedKeyUsageProperty
    {
        private readonly List<EnhancedKeyUsageRepresentation> _ekuList = new();

        /// <summary>
        /// Get property of EKUList.
        /// </summary>
        public List<EnhancedKeyUsageRepresentation> EnhancedKeyUsageList
        {
            get
            {
                return _ekuList;
            }
        }

        /// <summary>
        /// Constructor for EnhancedKeyUsageProperty.
        /// </summary>
        public EnhancedKeyUsageProperty(X509Certificate2 cert)
        {
            foreach (X509Extension extension in cert.Extensions)
            {
                // Filter to the OID for EKU
                if (extension.Oid.Value == "2.5.29.37")
                {
                    X509EnhancedKeyUsageExtension ext = extension as X509EnhancedKeyUsageExtension;
                    if (ext != null)
                    {
                        OidCollection oids = ext.EnhancedKeyUsages;
                        foreach (Oid oid in oids)
                        {
                            EnhancedKeyUsageRepresentation ekuString = new(oid.FriendlyName, oid.Value);
                            _ekuList.Add(ekuString);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Class for DNSNameList.
    /// </summary>
    public sealed class DnsNameProperty
    {
        private readonly List<DnsNameRepresentation> _dnsList = new();
        private readonly System.Globalization.IdnMapping idnMapping = new();

        private const string dnsNamePrefix = "DNS Name=";
        private const string distinguishedNamePrefix = "CN=";

        /// <summary>
        /// Get property of DnsNameList.
        /// </summary>
        public List<DnsNameRepresentation> DnsNameList
        {
            get
            {
                return _dnsList;
            }
        }

        /// <summary>
        /// Constructor for DnsNameProperty.
        /// </summary>
        public DnsNameProperty(X509Certificate2 cert)
        {
            string name;
            string unicodeName;
            DnsNameRepresentation dnsName;
            _dnsList = new List<DnsNameRepresentation>();

            // extract DNS name from subject distinguish name
            // if it exists and does not contain a comma
            // a comma, indicates it is not a DNS name
            if (cert.Subject.StartsWith(distinguishedNamePrefix, System.StringComparison.OrdinalIgnoreCase) &&
                !cert.Subject.Contains(','))
            {
                name = cert.Subject.Substring(distinguishedNamePrefix.Length);
                try
                {
                    unicodeName = idnMapping.GetUnicode(name);
                }
                catch (System.ArgumentException)
                {
                    // The name is not valid punyCode, assume it's valid ascii.
                    unicodeName = name;
                }

                dnsName = new DnsNameRepresentation(name, unicodeName);
                _dnsList.Add(dnsName);
            }

            foreach (X509Extension extension in cert.Extensions)
            {
                // Filter to the OID for Subject Alternative Name
                if (extension.Oid.Value == "2.5.29.17")
                {
                    string[] names = extension.Format(true).Split(Environment.NewLine);
                    foreach (string nameLine in names)
                    {
                        // Get the part after 'DNS Name='
                        if (nameLine.StartsWith(dnsNamePrefix, System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            name = nameLine.Substring(dnsNamePrefix.Length);
                            try
                            {
                                unicodeName = idnMapping.GetUnicode(name);
                            }
                            catch (System.ArgumentException)
                            {
                                // The name is not valid punyCode, assume it's valid ascii.
                                unicodeName = name;
                            }

                            dnsName = new DnsNameRepresentation(name, unicodeName);

                            // Only add the name if it is not the same as an existing name.
                            if (!_dnsList.Contains(dnsName))
                            {
                                _dnsList.Add(dnsName);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Downlevel helper function to determine if the OS is WIN8 and above.
    /// </summary>
    internal static class DownLevelHelper
    {
        private static bool s_isWin8Set = false;
        private static bool s_isWin8 = false;

        internal static bool IsWin8AndAbove()
        {
            if (!s_isWin8Set)
            {
                System.OperatingSystem osInfo = System.Environment.OSVersion;
                PlatformID platform = osInfo.Platform;
                Version version = osInfo.Version;

                if (platform.Equals(PlatformID.Win32NT) &&
                    ((version.Major > 6) ||
                     (version.Major == 6 && version.Minor >= 2)))
                {
                    s_isWin8 = true;
                }

                s_isWin8Set = true;
            }

            return s_isWin8;
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
    /// Check in UI is allowed.
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
    /// Container for helper functions that use pinvoke into crypt32.dll.
    /// </summary>
    internal static class Crypt32Helpers
    {
        /// <summary>
        /// Lock that guards access to the following static members
        /// -- storeNames.
        /// </summary>
        private static readonly object s_staticLock = new();

        internal static readonly List<string> storeNames = new();

        /// <summary>
        /// Get a list of store names at the specified location.
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

            Security.NativeMethods.CertEnumSystemStoreCallBackProto callBack = new(CertEnumSystemStoreCallBack);

            // Return a new list to avoid synchronization issues.

            List<string> names = new();
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
        /// Call back function used by CertEnumSystemStore
        ///
        /// Currently, there is no managed support for enumerating store
        /// names on a machine. We use the win32 function CertEnumSystemStore()
        /// to get a list of stores for a given context.
        ///
        /// Each time this callback is called, we add the passed store name
        /// to the list of stores.
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
#endif // !UNIX
