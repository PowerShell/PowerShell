//---------------------------------------------------------------------
// <copyright file="ComponentInstallation.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// Represents an instance of a registered component.
    /// </summary>
    internal class ComponentInstallation : InstallationPart
    {
        /// <summary>
        /// Gets the set of installed components for all products.
        /// </summary>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumcomponents.asp">MsiEnumComponents</a>
        /// </p></remarks>
        public static IEnumerable<ComponentInstallation> AllComponents
        {
            get
            {
                StringBuilder buf = new StringBuilder(40);
                for (uint i = 0; true; i++)
                {
                    uint ret = NativeMethods.MsiEnumComponents(i, buf);
                    if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                    if (ret != 0)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                    yield return new ComponentInstallation(buf.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the set of installed components for products in the indicated context.
        /// </summary>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/dd407947.aspx">MsiEnumComponentsEx</a>
        /// </p></remarks>
        public static IEnumerable<ComponentInstallation> Components(string szUserSid, UserContexts dwContext)
        {
            uint pcchSid = 32;
            StringBuilder szSid = new StringBuilder((int)pcchSid);
            StringBuilder buf = new StringBuilder(40);
            UserContexts installedContext;
            for (uint i = 0; true; i++)
            {
                uint  ret = NativeMethods.MsiEnumComponentsEx(szUserSid, dwContext, i, buf, out installedContext, szSid, ref pcchSid);
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    szSid.EnsureCapacity((int) ++pcchSid);
                    ret = NativeMethods.MsiEnumComponentsEx(szUserSid, dwContext, i, buf, out installedContext, szSid, ref pcchSid);
                }
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                yield return new ComponentInstallation(buf.ToString(), szSid.ToString(), installedContext);
            }
        }
        private static string GetProductCode(string component)
        {
            StringBuilder buf = new StringBuilder(40);
            uint ret = NativeMethods.MsiGetProductCode(component, buf);
            if (ret != 0)
            {
                return null;
            }

            return buf.ToString();
        }

        private static string GetProductCode(string component, string szUserSid, UserContexts dwContext)
        {
            // TODO: We really need what would be MsiGetProductCodeEx, or at least a reasonable facsimile thereof (something that restricts the search to the context defined by szUserSid & dwContext)
            return GetProductCode(component);
        }

        /// <summary>
        /// Creates a new ComponentInstallation, automatically detecting the
        /// product that the component is a part of.
        /// </summary>
        /// <param name="componentCode">component GUID</param>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproductcode.asp">MsiGetProductCode</a>
        /// </p></remarks>
        public ComponentInstallation(string componentCode)
            : this(componentCode, ComponentInstallation.GetProductCode(componentCode))
        {
        }

        /// <summary>
        /// Creates a new ComponentInstallation, automatically detecting the
        /// product that the component is a part of.
        /// </summary>
        /// <param name="componentCode">component GUID</param>
        /// <param name="szUserSid">context user SID</param>
        /// <param name="dwContext">user contexts</param>
        public ComponentInstallation(string componentCode, string szUserSid, UserContexts dwContext)
            : this(componentCode, ComponentInstallation.GetProductCode(componentCode, szUserSid, dwContext), szUserSid, dwContext)
        {
        }

        /// <summary>
        /// Creates a new ComponentInstallation for a component installed by a
        /// specific product.
        /// </summary>
        /// <param name="componentCode">component GUID</param>
        /// <param name="productCode">ProductCode GUID</param>
        public ComponentInstallation(string componentCode, string productCode)
            : this(componentCode, productCode, null, UserContexts.None)
        {
        }

        /// <summary>
        /// Creates a new ComponentInstallation for a component installed by a
        /// specific product.
        /// </summary>
        /// <param name="componentCode">component GUID</param>
        /// <param name="productCode">ProductCode GUID</param>
        /// <param name="szUserSid">context user SID</param>
        /// <param name="dwContext">user contexts</param>
        public ComponentInstallation(string componentCode, string productCode, string szUserSid, UserContexts dwContext)
            : base(componentCode, productCode, szUserSid, dwContext)
        {
            if (string.IsNullOrWhiteSpace(componentCode))
            {
                throw new ArgumentNullException("componentCode");
            }
        }

        /// <summary>
        /// Gets the component code (GUID) of the component.
        /// </summary>
        public string ComponentCode
        {
            get
            {
                return this.Id;
            }
        }

        /// <summary>
        /// Gets all client products of a specified component.
        /// </summary>
        /// <returns>enumeration over all client products of the component</returns>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Because clients are not ordered, any new component has an arbitrary index.
        /// This means that the property may return clients in any order.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumclients.asp">MsiEnumClients</a>,
        /// <a href="http://msdn.microsoft.com/library/dd407946.aspx">MsiEnumClientsEx</a>
        /// </p></remarks>
        public IEnumerable<ProductInstallation> ClientProducts
        {
            get
            {
                StringBuilder buf = new StringBuilder(40);
                for (uint i = 0; true; i++)
                {
                    uint chSid = 0;
                    UserContexts installedContext;
                    uint ret = this.Context == UserContexts.None ?
                            NativeMethods.MsiEnumClients(this.ComponentCode, i, buf) :
                            NativeMethods.MsiEnumClientsEx(this.ComponentCode, this.UserSid, this.Context, i, buf, out installedContext, null, ref chSid);
                    if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                    else if (ret == (uint) NativeMethods.Error.UNKNOWN_COMPONENT) break;
                    if (ret != 0)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                    yield return new ProductInstallation(buf.ToString());
                }
            }
        }

        /// <summary>
        /// Gets the installed state of a component.
        /// </summary>
        /// <returns>the installed state of the component, or InstallState.Unknown
        /// if this component is not part of a product</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetcomponentpath.asp">MsiGetComponentPath</a>,
        /// <a href="http://msdn.microsoft.com/library/dd408006.aspx">MsiGetComponentPathEx</a>
        /// </p></remarks>
        public override InstallState State
        {
            get
            {
                if (this.ProductCode != null)
                {
                    uint bufSize = 0;
                    int installState = this.Context == UserContexts.None ?
                        NativeMethods.MsiGetComponentPath(
                            this.ProductCode, this.ComponentCode, null, ref bufSize) :
                        NativeMethods.MsiGetComponentPathEx(
                            this.ProductCode, this.ComponentCode, this.UserSid, this.Context, null, ref bufSize);
                    return (InstallState) installState;
                }
                else
                {
                    return InstallState.Unknown;
                }
            }
        }

        /// <summary>
        /// Gets the full path to an installed component. If the key path for the component is a
        /// registry key then the registry key is returned.
        /// </summary>
        /// <returns>The file or registry keypath to the component, or null if the component is not available.</returns>
        /// <exception cref="ArgumentException">An unknown product or component was specified</exception>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// If the component is a registry key, the registry roots are represented numerically.
        /// For example, a registry path of "HKEY_CURRENT_USER\SOFTWARE\Microsoft" would be returned
        /// as "01:\SOFTWARE\Microsoft". The registry roots returned are defined as follows:
        /// HKEY_CLASSES_ROOT=00, HKEY_CURRENT_USER=01, HKEY_LOCAL_MACHINE=02, HKEY_USERS=03,
        /// HKEY_PERFORMANCE_DATA=04
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetcomponentpath.asp">MsiGetComponentPath</a>,
        /// <a href="http://msdn.microsoft.com/library/dd408006.aspx">MsiGetComponentPathEx</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msilocatecomponent.asp">MsiLocateComponent</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Path
        {
            get
            {
                StringBuilder buf = new StringBuilder(256);
                uint bufSize = (uint) buf.Capacity;
                InstallState installState;

                if (this.ProductCode != null)
                {
                    installState = (this.Context == UserContexts.None) ?
                        (InstallState) NativeMethods.MsiGetComponentPath(
                            this.ProductCode, this.ComponentCode, buf, ref bufSize) :
                        (InstallState) NativeMethods.MsiGetComponentPathEx(
                            this.ProductCode, this.ComponentCode, this.UserSid, this.Context, buf, ref bufSize);
                    if (installState == InstallState.MoreData)
                    {
                        buf.Capacity = (int) ++bufSize;
                        installState = (this.Context == UserContexts.None) ?
                            (InstallState) NativeMethods.MsiGetComponentPath(
                                this.ProductCode, this.ComponentCode, buf, ref bufSize) :
                            (InstallState) NativeMethods.MsiGetComponentPathEx(
                                this.ProductCode, this.ComponentCode, this.UserSid, this.Context, buf, ref bufSize);
                    }
                }
                else
                {
                    installState = (InstallState) NativeMethods.MsiLocateComponent(
                        this.ComponentCode, buf, ref bufSize);
                    if (installState == InstallState.MoreData)
                    {
                        buf.Capacity = (int) ++bufSize;
                        installState = (InstallState) NativeMethods.MsiLocateComponent(
                            this.ComponentCode, buf, ref bufSize);
                    }
                }

                switch (installState)
                {
                    case InstallState.Local:
                    case InstallState.Source:
                    case InstallState.SourceAbsent:
                        return buf.ToString();

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Gets the set of registered qualifiers for the component.
        /// </summary>
        /// <returns>Enumeration of the qualifiers for the component.</returns>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Because qualifiers are not ordered, any new qualifier has an arbitrary index,
        /// meaning the function can return qualifiers in any order.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumcomponentqualifiers.asp">MsiEnumComponentQualifiers</a>
        /// </p></remarks>
        public IEnumerable<ComponentInstallation.Qualifier> Qualifiers
        {
            get
            {
                StringBuilder qualBuf = new StringBuilder(255);
                StringBuilder dataBuf = new StringBuilder(255);
                for (uint i = 0; ; i++)
                {
                    uint qualBufSize = (uint) qualBuf.Capacity;
                    uint dataBufSize = (uint) dataBuf.Capacity;
                    uint ret = NativeMethods.MsiEnumComponentQualifiers(
                        this.ComponentCode, i, qualBuf, ref qualBufSize, dataBuf, ref dataBufSize);
                    if (ret == (uint) NativeMethods.Error.MORE_DATA)
                    {
                        qualBuf.Capacity = (int) ++qualBufSize;
                        dataBuf.Capacity = (int) ++dataBufSize;
                        ret = NativeMethods.MsiEnumComponentQualifiers(
                            this.ComponentCode, i, qualBuf, ref qualBufSize, dataBuf, ref dataBufSize);
                    }

                    if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS ||
                        ret == (uint) NativeMethods.Error.UNKNOWN_COMPONENT)
                    {
                        break;
                    }

                    if (ret != 0)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }

                    yield return new ComponentInstallation.Qualifier(
                        qualBuf.ToString(), dataBuf.ToString());
                }
            }
        }

        /// <summary>
        /// Holds data about a component qualifier.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumcomponentqualifiers.asp">MsiEnumComponentQualifiers</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
        public struct Qualifier
        {
            private string qualifierCode;
            private string data;

            internal Qualifier(string qualifierCode, string data)
            {
                this.qualifierCode = qualifierCode;
                this.data = data;
            }

            /// <summary>
            /// Gets the qualifier code.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string QualifierCode
            {
                get
                {
                    return this.qualifierCode;
                }
            }

            /// <summary>
            /// Gets the qualifier data.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string Data
            {
                get
                {
                    return this.data;
                }
            }
        }
    }
}
