//---------------------------------------------------------------------
// <copyright file="InstallerAdvertise.cs" company="Microsoft Corporation">
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
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;

    internal static partial class Installer
    {
        /// <summary>
        /// Advertises a product to the local computer.
        /// </summary>
        /// <param name="packagePath">Path to the package of the product being advertised</param>
        /// <param name="perUser">True if the product is user-assigned; false if it is machine-assigned.</param>
        /// <param name="transforms">Semi-colon delimited list of transforms to be applied. This parameter may be null.</param>
        /// <param name="locale">The language to use if the source supports multiple languages</param>
        /// <exception cref="FileNotFoundException">the specified package file does not exist</exception>
        /// <seealso cref="GenerateAdvertiseScript(string,string,string,int,ProcessorArchitecture,bool)"/>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproduct.asp">MsiAdvertiseProduct</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproductex.asp">MsiAdvertiseProductEx</a>
        /// </p></remarks>
        public static void AdvertiseProduct(string packagePath, bool perUser, string transforms, int locale)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentNullException("packagePath");
            }

            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException(null, packagePath);
            }

            uint ret = NativeMethods.MsiAdvertiseProduct(packagePath, new IntPtr(perUser ? 1 : 0), transforms, (ushort) locale);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Generates an advertise script. The method enables the installer to write to a
        /// script the registry and shortcut information used to assign or publish a product.
        /// </summary>
        /// <param name="packagePath">Path to the package of the product being advertised</param>
        /// <param name="scriptFilePath">path to script file to be created with the advertise information</param>
        /// <param name="transforms">Semi-colon delimited list of transforms to be applied. This parameter may be null.</param>
        /// <param name="locale">The language to use if the source supports multiple languages</param>
        /// <exception cref="FileNotFoundException">the specified package file does not exist</exception>
        /// <seealso cref="AdvertiseProduct"/>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproduct.asp">MsiAdvertiseProduct</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproductex.asp">MsiAdvertiseProductEx</a>
        /// </p></remarks>
        public static void GenerateAdvertiseScript(string packagePath, string scriptFilePath, string transforms, int locale)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentNullException("packagePath");
            }

            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException(null, packagePath);
            }

            uint ret = NativeMethods.MsiAdvertiseProduct(packagePath, scriptFilePath, transforms, (ushort) locale);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Generates an advertise script. The method enables the installer to write to a
        /// script the registry and shortcut information used to assign or publish a product.
        /// </summary>
        /// <param name="packagePath">Path to the package of the product being advertised</param>
        /// <param name="scriptFilePath">path to script file to be created with the advertise information</param>
        /// <param name="transforms">Semi-colon delimited list of transforms to be applied. This parameter may be null.</param>
        /// <param name="locale">The language to use if the source supports multiple languages</param>
        /// <param name="processor">Targeted processor architecture.</param>
        /// <param name="instance">True to install multiple instances through product code changing transform.
        /// Advertises a new instance of the product. Requires that the <paramref name="transforms"/> parameter
        /// includes the instance transform that changes the product code.</param>
        /// <seealso cref="AdvertiseProduct"/>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproduct.asp">MsiAdvertiseProduct</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiadvertiseproductex.asp">MsiAdvertiseProductEx</a>
        /// </p></remarks>
        public static void GenerateAdvertiseScript(
            string packagePath,
            string scriptFilePath,
            string transforms,
            int locale,
            ProcessorArchitecture processor,
            bool instance)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentNullException("packagePath");
            }

            if (string.IsNullOrWhiteSpace(scriptFilePath))
            {
                throw new ArgumentNullException("scriptFilePath");
            }

            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException(null, packagePath);
            }

            uint platform = 0;
            switch (processor)
            {
                case ProcessorArchitecture.X86: platform = (uint) 1; break;
                case ProcessorArchitecture.IA64: platform = (uint) 2; break;
                case ProcessorArchitecture.Amd64: platform = (uint) 4; break;
            }

            uint ret = NativeMethods.MsiAdvertiseProductEx(
                packagePath,
                scriptFilePath,
                transforms,
                (ushort) locale,
                platform,
                instance ? (uint) 1 : 0);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Copies an advertise script file to the local computer.
        /// </summary>
        /// <param name="scriptFile">Path to a script file generated by
        /// <see cref="GenerateAdvertiseScript(string,string,string,int,ProcessorArchitecture,bool)"/></param>
        /// <param name="flags">Flags controlling advertisement</param>
        /// <param name="removeItems">True if specified items are to be removed instead of being created</param>
        /// <remarks><p>
        /// The process calling this function must be running under the LocalSystem account. To advertise an
        /// application for per-user installation to a targeted user, the thread that calls this function must
        /// impersonate the targeted user. If the thread calling this function is not impersonating a targeted
        /// user, the application is advertised to all users for installation with elevated privileges.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "flags")]
        public static void AdvertiseScript(string scriptFile, int flags, bool removeItems)
        {
            uint ret = NativeMethods.MsiAdvertiseScript(scriptFile, (uint) flags, IntPtr.Zero, removeItems);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Processes an advertise script file into the specified locations.
        /// </summary>
        /// <param name="scriptFile">Path to a script file generated by
        /// <see cref="GenerateAdvertiseScript(string,string,string,int,ProcessorArchitecture,bool)"/></param>
        /// <param name="iconFolder">An optional path to a folder in which advertised icon files and transform
        /// files are located. If this parameter is null, no icon or transform files are written.</param>
        /// <param name="shortcuts">True if shortcuts should be created</param>
        /// <param name="removeItems">True if specified items are to be removed instead of created</param>
        /// <remarks><p>
        /// The process calling this function must be running under the LocalSystem account. To advertise an
        /// application for per-user installation to a targeted user, the thread that calls this function must
        /// impersonate the targeted user. If the thread calling this function is not impersonating a targeted
        /// user, the application is advertised to all users for installation with elevated privileges.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprocessadvertisescript.asp">MsiProcessAdvertiseScript</a>
        /// </p></remarks>
        public static void ProcessAdvertiseScript(string scriptFile, string iconFolder, bool shortcuts, bool removeItems)
        {
            uint ret = NativeMethods.MsiProcessAdvertiseScript(scriptFile, iconFolder, IntPtr.Zero, shortcuts, removeItems);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Gets product information for an installer script file.
        /// </summary>
        /// <param name="scriptFile">Path to a script file generated by
        /// <see cref="GenerateAdvertiseScript(string,string,string,int,ProcessorArchitecture,bool)"/></param>
        /// <returns>ProductInstallation stub with advertise-related properties filled in.</returns>
        /// <exception cref="ArgumentOutOfRangeException">An invalid product property was requested</exception>
        /// <remarks><p>
        /// Only the following properties will be filled in in the returned object:<ul>
        /// <li><see cref="ProductInstallation.ProductCode"/></li>
        /// <li><see cref="ProductInstallation.AdvertisedLanguage"/></li>
        /// <li><see cref="ProductInstallation.AdvertisedVersion"/></li>
        /// <li><see cref="ProductInstallation.AdvertisedProductName"/></li>
        /// <li><see cref="ProductInstallation.AdvertisedPackageName"/></li>
        /// </ul>Other properties will be null.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetproductinfofromscript.asp">MsiGetProductInfoFromScript</a>
        /// </p></remarks>
        public static ProductInstallation GetProductInfoFromScript(string scriptFile)
        {
            if (string.IsNullOrWhiteSpace(scriptFile))
            {
                throw new ArgumentNullException("scriptFile");
            }
            StringBuilder productCodeBuf = new StringBuilder(40);
            ushort lang;
            uint ver;
            StringBuilder productNameBuf = new StringBuilder(100);
            StringBuilder packageNameBuf = new StringBuilder(40);
            uint productCodeBufSize = (uint) productCodeBuf.Capacity;
            uint productNameBufSize = (uint) productNameBuf.Capacity;
            uint packageNameBufSize = (uint) packageNameBuf.Capacity;
            uint ret = NativeMethods.MsiGetProductInfoFromScript(
                scriptFile,
                productCodeBuf,
                out lang,
                out ver,
                productNameBuf,
                ref productNameBufSize,
                packageNameBuf,
                ref packageNameBufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                productCodeBuf.Capacity = (int) ++productCodeBufSize;
                productNameBuf.Capacity = (int) ++productNameBufSize;
                packageNameBuf.Capacity = (int) ++packageNameBufSize;
                ret = NativeMethods.MsiGetProductInfoFromScript(
                    scriptFile,
                    productCodeBuf,
                    out lang,
                    out ver,
                    productNameBuf,
                    ref productNameBufSize,
                    packageNameBuf,
                    ref packageNameBufSize);
            }

            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            uint verPart1 = ver >> 24;
            uint verPart2 = (ver & 0x00FFFFFF) >> 16;
            uint verPart3 = ver & 0x0000FFFF;
            Version version = new Version((int) verPart1, (int) verPart2, (int) verPart3);

            IDictionary<string, string> props = new Dictionary<string, string>();
            props["ProductCode"] = productCodeBuf.ToString();
            props["Language"] = lang.ToString(CultureInfo.InvariantCulture);
            props["Version"] = version.ToString();
            props["ProductName"] = productNameBuf.ToString();
            props["PackageName"] = packageNameBuf.ToString();
            return new ProductInstallation(props);
        }
    }
}
