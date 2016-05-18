//---------------------------------------------------------------------
// <copyright file="ProductInstallation.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Represents a unique instance of a product that
    /// is either advertised, installed or unknown.
    /// </summary>
    internal class ProductInstallation : Installation
    {
        /// <summary>
        /// Gets the set of all products with a specified upgrade code. This method lists the
        /// currently installed and advertised products that have the specified UpgradeCode
        /// property in their Property table.
        /// </summary>
        /// <param name="upgradeCode">Upgrade code of related products</param>
        /// <returns>Enumeration of product codes</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumrelatedproducts.asp">MsiEnumRelatedProducts</a>
        /// </p></remarks>
        public static IEnumerable<ProductInstallation> GetRelatedProducts(string upgradeCode)
        {
            StringBuilder buf = new StringBuilder(40);
            for (uint i = 0; true; i++)
            {
                uint ret = NativeMethods.MsiEnumRelatedProducts(upgradeCode, 0, i, buf);
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                yield return new ProductInstallation(buf.ToString());
            }
        }

        /// <summary>
        /// Enumerates all product installations on the system.
        /// </summary>
        /// <returns>An enumeration of product objects.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumproducts.asp">MsiEnumProducts</a>,
        /// </p></remarks>
        public static IEnumerable<ProductInstallation> AllProducts
        {
            get
            {
                return GetProducts(null, null, UserContexts.All);
            }
        }

        /// <summary>
        /// Enumerates product installations based on certain criteria.
        /// </summary>
        /// <param name="productCode">ProductCode (GUID) of the product instances to be enumerated. Only
        /// instances of products within the scope of the context specified by the
        /// <paramref name="userSid"/> and <paramref name="context"/> parameters will be
        /// enumerated. This parameter may be set to null to enumerate all products in the specified
        /// context.</param>
        /// <param name="userSid">Specifies a security identifier (SID) that restricts the context
        /// of enumeration. A SID value other than s-1-1-0 is considered a user SID and restricts
        /// enumeration to the current user or any user in the system. The special SID string
        /// s-1-1-0 (Everyone) specifies enumeration across all users in the system. This parameter
        /// can be set to null to restrict the enumeration scope to the current user. When
        /// <paramref name="context"/> is set to the machine context only,
        /// <paramref name="userSid"/> must be null.</param>
        /// <param name="context">Specifies the user context.</param>
        /// <returns>An enumeration of product objects for enumerated product instances.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumproductsex.asp">MsiEnumProductsEx</a>
        /// </p></remarks>
        public static IEnumerable<ProductInstallation> GetProducts(
            string productCode, string userSid, UserContexts context)
        {
            StringBuilder buf = new StringBuilder(40);
            UserContexts targetContext;
            StringBuilder targetSidBuf = new StringBuilder(40);
            for (uint i = 0; ; i++)
            {
                uint targetSidBufSize = (uint) targetSidBuf.Capacity;
                uint ret = NativeMethods.MsiEnumProductsEx(
                    productCode,
                    userSid,
                    context,
                    i,
                    buf,
                    out targetContext,
                    targetSidBuf,
                    ref targetSidBufSize);
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    targetSidBuf.Capacity = (int) ++targetSidBufSize;
                    ret = NativeMethods.MsiEnumProductsEx(
                        productCode,
                        userSid,
                        context,
                        i,
                        buf,
                        out targetContext,
                        targetSidBuf,
                        ref targetSidBufSize);
                }

                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                {
                    break;
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                yield return new ProductInstallation(
                    buf.ToString(),
                    targetSidBuf.ToString(),
                    targetContext);
            }
        }

        private IDictionary<string, string> properties;

        /// <summary>
        /// Creates a new object for accessing information about a product installation on the current system.
        /// </summary>
        /// <param name="productCode">ProductCode (GUID) of the product.</param>
        /// <remarks><p>
        /// All available user contexts will be queried.
        /// </p></remarks>
        public ProductInstallation(string productCode)
            : this(productCode, null, UserContexts.All)
        {
        }

        /// <summary>
        /// Creates a new object for accessing information about a product installation on the current system.
        /// </summary>
        /// <param name="productCode">ProductCode (GUID) of the product.</param>
        /// <param name="userSid">The specific user, when working in a user context.  This
        /// parameter may be null to indicate the current user.  The parameter must be null
        /// when working in a machine context.</param>
        /// <param name="context">The user context. The calling process must have administrative
        /// privileges to get information for a product installed for a user other than the
        /// current user.</param>
        public ProductInstallation(string productCode, string userSid, UserContexts context)
            : base(productCode, userSid, context)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                throw new ArgumentNullException("productCode");
            }
        }

        internal ProductInstallation(IDictionary<string, string> properties)
            : base(properties["ProductCode"], null, UserContexts.None)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Gets the set of published features for the product.
        /// </summary>
        /// <returns>Enumeration of published features for the product.</returns>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Because features are not ordered, any new feature has an arbitrary index, meaning
        /// this property can return features in any order.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumfeatures.asp">MsiEnumFeatures</a>
        /// </p></remarks>
        public IEnumerable<FeatureInstallation> Features
        {
            get
            {
                StringBuilder buf = new StringBuilder(256);
                for (uint i = 0; ; i++)
                {
                    uint ret = NativeMethods.MsiEnumFeatures(this.ProductCode, i, buf, null);

                    if (ret != 0)
                    {
                        break;
                    }

                    yield return new FeatureInstallation(buf.ToString(), this.ProductCode);
                }
            }
        }

        /// <summary>
        /// Gets the ProductCode (GUID) of the product.
        /// </summary>
        public string ProductCode
        {
            get { return this.InstallationCode; }
        }

        /// <summary>
        /// Gets a value indicating whether this product is installed on the current system.
        /// </summary>
        public override bool IsInstalled
        {
            get
            {
                return (this.State == InstallState.Default);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this product is advertised on the current system.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsAdvertised
        {
            get
            {
                return (this.State == InstallState.Advertised);
            }
        }

        /// <summary>
        /// Checks whether the product is installed with elevated privileges. An application is called
        /// a "managed application" if elevated (system) privileges are used to install the application.
        /// </summary>
        /// <returns>True if the product is elevated; false otherwise</returns>
        /// <remarks><p>
        /// Note that this property does not take into account policies such as AlwaysInstallElevated,
        /// but verifies that the local system owns the product's registry data.
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsElevated
        {
            get
            {
                bool isElevated;
                uint ret = NativeMethods.MsiIsProductElevated(this.ProductCode, out isElevated);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                return isElevated;
            }
        }

        /// <summary>
        /// Gets the source list of this product installation.
        /// </summary>
        internal override SourceList SourceList
        {
            get
            {
                return this.properties == null ? base.SourceList : null;
            }
        }

        internal InstallState State
        {
            get
            {
                if (this.properties != null)
                {
                    return InstallState.Unknown;
                }
                else
                {
                    int installState = NativeMethods.MsiQueryProductState(this.ProductCode);
                    return (InstallState) installState;
                }
            }
        }

        internal override int InstallationType
        {
            get
            {
                const int MSICODE_PRODUCT = 0x00000000;
                return MSICODE_PRODUCT;
            }
        }

        /// <summary>
        /// The support link.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string HelpLink
        {
            get
            {
                return this["HelpLink"];
            }
        }

        /// <summary>
        /// The support telephone.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string HelpTelephone
        {
            get
            {
                return this["HelpTelephone"];
            }
        }

        /// <summary>
        /// Date and time the product was installed.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime InstallDate
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(
                        this["InstallDate"], "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// The installed product name.
        /// </summary>
        public string ProductName
        {
            get
            {
                return this["InstalledProductName"];
            }
        }

        /// <summary>
        /// The installation location.
        /// </summary>
        public string InstallLocation
        {
            get
            {
                return this["InstallLocation"];
            }
        }

        /// <summary>
        /// The installation source.
        /// </summary>
        public string InstallSource
        {
            get
            {
                return this["InstallSource"];
            }
        }

        /// <summary>
        /// The local cached package.
        /// </summary>
        public string LocalPackage
        {
            get
            {
                return this["LocalPackage"];
            }
        }

        /// <summary>
        /// The publisher.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Publisher
        {
            get
            {
                return this["Publisher"];
            }
        }

        /// <summary>
        /// URL about information.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Uri UrlInfoAbout
        {
            get
            {
                string value = this["URLInfoAbout"];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        return new Uri(value);
                    }
                    catch (UriFormatException) { }
                }

                return null;
            }
        }

        /// <summary>
        /// The URL update information.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Uri UrlUpdateInfo
        {
            get
            {
                string value = this["URLUpdateInfo"];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        return new Uri(value);
                    }
                    catch (UriFormatException) { }
                }

                return null;
            }
        }

        /// <summary>
        /// The product version.
        /// </summary>
        public Version ProductVersion
        {
            get
            {
                string ver = this["VersionString"];
                return ProductInstallation.ParseVersion(ver);
            }
        }

        /// <summary>
        /// The product identifier.
        /// </summary>
        /// <remarks><p>
        /// For more information, see
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/productid.asp">ProductID</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ProductId
        {
            get
            {
                return this["ProductID"];
            }
        }

        /// <summary>
        /// The company that is registered to use the product.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string RegCompany
        {
            get
            {
                return this["RegCompany"];
            }
        }

        /// <summary>
        /// The owner who is registered to use the product.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string RegOwner
        {
            get
            {
                return this["RegOwner"];
            }
        }

        /// <summary>
        /// Transforms.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedTransforms
        {
            get
            {
                return this["Transforms"];
            }
        }

        /// <summary>
        /// Product language.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedLanguage
        {
            get
            {
                return this["Language"];
            }
        }

        /// <summary>
        /// Human readable product name.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedProductName
        {
            get
            {
                return this["ProductName"];
            }
        }

        /// <summary>
        /// True if the product is advertised per-machine;
        /// false if it is per-user or not advertised.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool AdvertisedPerMachine
        {
            get
            {
                return this["AssignmentType"] == "1";
            }
        }

        /// <summary>
        /// Identifier of the package that a product is installed from.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedPackageCode
        {
            get
            {
                return this["PackageCode"];
            }
        }

        /// <summary>
        /// Version of the advertised product.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Version AdvertisedVersion
        {
            get
            {
                string ver = this["Version"];
                return ProductInstallation.ParseVersion(ver);
            }
        }

        /// <summary>
        /// Primary icon for the package.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedProductIcon
        {
            get
            {
                return this["ProductIcon"];
            }
        }

        /// <summary>
        /// Name of the installation package for the advertised product.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string AdvertisedPackageName
        {
            get
            {
                return this["PackageName"];
            }
        }

        /// <summary>
        /// True if the advertised product can be serviced by
        /// non-administrators without elevation.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool PrivilegedPatchingAuthorized
        {
            get
            {
                return this["AuthorizedLUAApp"] == "1";
            }
        }

        /// <summary>
        /// Gets information about an installation of a product.
        /// </summary>
        /// <param name="propertyName">Name of the property being retrieved.</param>
        /// <exception cref="ArgumentOutOfRangeException">An unknown product or property was requested</exception>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproductinfo.asp">MsiGetProductInfo</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproductinfoex.asp">MsiGetProductInfoEx</a>
        /// </p></remarks>
        public override string this[string propertyName]
        {
            get
            {
                if (this.properties != null)
                {
                    string value = null;
                    this.properties.TryGetValue(propertyName, out value);
                    return value;
                }
                else
                {
                    StringBuilder buf = new StringBuilder(40);
                    uint bufSize = (uint) buf.Capacity;
                    uint ret;

                    if (this.Context == UserContexts.UserManaged ||
                        this.Context == UserContexts.UserUnmanaged ||
                        this.Context == UserContexts.Machine)
                    {
                        ret = NativeMethods.MsiGetProductInfoEx(
                            this.ProductCode,
                            this.UserSid,
                            this.Context,
                            propertyName,
                            buf,
                            ref bufSize);
                        if (ret == (uint) NativeMethods.Error.MORE_DATA)
                        {
                            buf.Capacity = (int) ++bufSize;
                            ret = NativeMethods.MsiGetProductInfoEx(
                                this.ProductCode,
                                this.UserSid,
                                this.Context,
                                propertyName,
                                buf,
                                ref bufSize);
                        }
                    }
                    else
                    {
                        ret = NativeMethods.MsiGetProductInfo(
                            this.ProductCode,
                            propertyName,
                            buf,
                            ref bufSize);
                        if (ret == (uint) NativeMethods.Error.MORE_DATA)
                        {
                            buf.Capacity = (int) ++bufSize;
                            ret = NativeMethods.MsiGetProductInfo(
                                this.ProductCode,
                                propertyName,
                                buf,
                                ref bufSize);
                        }
                    }

                    if (ret != 0)
                    {
                        return null;
                    }

                    return buf.ToString();
                }
            }
        }

        /// <summary>
        /// Gets the installed state for a product feature.
        /// </summary>
        /// <param name="feature">The feature being queried; identifier from the
        /// Feature table</param>
        /// <returns>Installation state of the feature for the product instance: either
        /// <see cref="InstallState.Local"/>, <see cref="InstallState.Source"/>,
        /// or <see cref="InstallState.Advertised"/>.</returns>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiqueryfeaturestate.asp">MsiQueryFeatureState</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiqueryfeaturestateex.asp">MsiQueryFeatureStateEx</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState GetFeatureState(string feature)
        {
            if (this.properties != null)
            {
                return InstallState.Unknown;
            }
            else
            {
                int installState;
                uint ret = NativeMethods.MsiQueryFeatureStateEx(
                    this.ProductCode,
                    this.UserSid,
                    this.Context,
                    feature,
                    out installState);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                return (InstallState) installState;
            }
        }

        /// <summary>
        /// Gets the installed state for a product component.
        /// </summary>
        /// <param name="component">The component being queried; GUID of the component
        /// as found in the ComponentId column of the Component table.</param>
        /// <returns>Installation state of the component for the product instance: either
        /// <see cref="InstallState.Local"/> or <see cref="InstallState.Source"/>.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiquerycomponnetstate.asp">MsiQueryComponentState</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState GetComponentState(string component)
        {
            if (this.properties != null)
            {
                return InstallState.Unknown;
            }
            else
            {
                int installState;
                uint ret = NativeMethods.MsiQueryComponentState(
                    this.ProductCode,
                    this.UserSid,
                    this.Context,
                    component,
                    out installState);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                return (InstallState) installState;
            }
        }

        /// <summary>
        /// Obtains and stores the user information and product ID from an installation wizard.
        /// </summary>
        /// <remarks><p>
        /// This method is typically called by an application during the first run of the application. The application
        /// first gets the <see cref="ProductInstallation.ProductId"/> or <see cref="ProductInstallation.RegOwner"/>.
        /// If those properties are missing, the application calls CollectUserInfo.
        /// CollectUserInfo opens the product's installation package and invokes a wizard sequence that collects
        /// user information. Upon completion of the sequence, user information is registered. Since this API requires
        /// an authored user interface, the user interface level should be set to full by calling
        /// <see cref="Installer.SetInternalUI(InstallUIOptions)"/> as <see cref="InstallUIOptions.Full"/>.
        /// </p><p>
        /// The CollectUserInfo method invokes a FirstRun dialog from the product installation database.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msicollectuserinfo.asp">MsiCollectUserInfo</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void CollectUserInfo()
        {
            if (this.properties == null)
            {
                uint ret = NativeMethods.MsiCollectUserInfo(this.InstallationCode);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        /// <summary>
        /// Some products might write some invalid/nonstandard version strings to the registry.
        /// This method tries to get the best data it can.
        /// </summary>
        /// <param name="ver">Version string retrieved from the registry.</param>
        /// <returns>Version object, or null if the version string is completely invalid.</returns>
        private static Version ParseVersion(string ver)
        {
            if (ver != null)
            {
                int dotCount = 0;
                for (int i = 0; i < ver.Length; i++)
                {
                    char c = ver[i];
                    if (c == '.') dotCount++;
                    else if (!Char.IsDigit(c))
                    {
                        ver = ver.Substring(0, i);
                        break;
                    }
                }

                if (ver.Length > 0)
                {
                    if (dotCount == 0)
                    {
                        ver = ver + ".0";
                    }
                    else if (dotCount > 3)
                    {
                        string[] verSplit = ver.Split('.');
                        ver = String.Join(".", verSplit, 0, 4);
                    }

                    try
                    {
                        return new Version(ver);
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            return null;
        }
    }
}
