//---------------------------------------------------------------------
// <copyright file="InstallerUtils.cs" company="Microsoft Corporation">
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
    using System.Resources;
    using System.Runtime.InteropServices;
    using System.Text;

    internal static partial class Installer
    {
        /// <summary>
        /// Gets the current version of the installer.
        /// </summary>
        public static Version Version
        {
            get
            {
                // TODO: Use the extended form of version info to get the 4th component of the version.
                uint[] dllVersionInfo = new uint[5];
                dllVersionInfo[0] = 20;
                int hr = NativeMethods.DllGetVersion(dllVersionInfo);
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                return new Version((int) dllVersionInfo[1], (int) dllVersionInfo[2], (int) dllVersionInfo[3]);
            }
        }

        internal static ResourceManager ErrorResources
        {
            get
            {
                if (errorResources == null)
                {
                    errorResources = new ResourceManager(typeof(Installer).Namespace + ".Errors", typeof(Installer).Assembly);
                }
                return errorResources;
            }
        }

        /// <summary>
        /// Gets a Windows Installer error message in the system default language.
        /// </summary>
        /// <param name="errorNumber">The error number.</param>
        /// <returns>The message string, or null if the error message is not found.</returns>
        /// <remarks><p>
        /// The returned string may have tokens such as [2] and [3] that are meant to be substituted
        /// with context-specific values.
        /// </p><p>
        /// Error numbers greater than 2000 refer to MSI "internal" errors, and are always
        /// returned in English.
        /// </p></remarks>
        public static string GetErrorMessage(int errorNumber)
        {
            return Installer.GetErrorMessage(errorNumber, null);
        }

        /// <summary>
        /// Gets a Windows Installer error message in a specified language.
        /// </summary>
        /// <param name="errorNumber">The error number.</param>
        /// <param name="culture">The locale for the message.</param>
        /// <returns>The message string, or null if the error message or locale is not found.</returns>
        /// <remarks><p>
        /// The returned string may have tokens such as [2] and [3] that are meant to be substituted
        /// with context-specific values.
        /// </p><p>
        /// Error numbers greater than 2000 refer to MSI "internal" errors, and are always
        /// returned in English.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static string GetErrorMessage(int errorNumber, CultureInfo culture)
        {
            if (culture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            string msg = Installer.ErrorResources.GetString(
                errorNumber.ToString(CultureInfo.InvariantCulture.NumberFormat),
                culture);
            if (msg == null)
            {
                string msiMsgModule = Path.Combine(
                    Environment.SystemDirectory, "msimsg.dll");
                msg = Installer.GetMessageFromModule(
                    msiMsgModule, errorNumber, culture);
            }
            return msg;
        }

        private static string GetMessageFromModule(
            string modulePath, int errorNumber, CultureInfo culture)
        {
            const uint LOAD_LIBRARY_AS_DATAFILE = 2;
            const int RT_RCDATA = 10;

            IntPtr msgModule = NativeMethods.LoadLibraryEx(
                modulePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
            if (msgModule == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                // On pre-Vista systems, the messages are stored as RCDATA resources.

                int lcid = (culture == CultureInfo.InvariantCulture) ?
                    0 : culture.LCID;
                IntPtr resourceInfo = NativeMethods.FindResourceEx(
                    msgModule,
                    new IntPtr(RT_RCDATA),
                    new IntPtr(errorNumber),
                    (ushort) lcid);
                if (resourceInfo != IntPtr.Zero)
                {
                    IntPtr resourceData = NativeMethods.LoadResource(
                        msgModule, resourceInfo);
                    IntPtr resourcePtr = NativeMethods.LockResource(resourceData);

                    if (lcid == 0)
                    {
                        string msg = Marshal.PtrToStringAnsi(resourcePtr);
                        return msg;
                    }
                    else
                    {
                        int len = 0;
                        while (Marshal.ReadByte(resourcePtr, len) != 0)
                        {
                            len++;
                        }
                        byte[] msgBytes = new byte[len + 1];
                        Marshal.Copy(resourcePtr, msgBytes, 0, msgBytes.Length);
                        Encoding encoding = Encoding.GetEncoding(
                            culture.TextInfo.ANSICodePage);
                        string msg = encoding.GetString(msgBytes);
                        return msg;
                    }
                }
                else
                {
                    // On Vista (and above?), the messages are stored in the module message table.
                    // They're actually in MUI files, and the redirection happens automatically here.

                    const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
                    const uint FORMAT_MESSAGE_FROM_HMODULE   = 0x00000800;
                    const uint MESSAGE_OFFSET = 20000; // Not documented, but observed on Vista

                    StringBuilder buf = new StringBuilder(1024);
                    uint formatCount = NativeMethods.FormatMessage(
                        FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_IGNORE_INSERTS,
                        msgModule,
                        (uint) errorNumber + MESSAGE_OFFSET,
                        (ushort) lcid,
                        buf,
                        (uint) buf.Capacity,
                        IntPtr.Zero);

                    return formatCount != 0 ? buf.ToString().Trim() : null;
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(msgModule);
            }
        }

        /// <summary>
        /// Gets a formatted Windows Installer error message in the system default language.
        /// </summary>
        /// <param name="errorRecord">Error record containing the error number in the first field, and
        /// error-specific parameters in the other fields.</param>
        /// <returns>The message string, or null if the error message is not found.</returns>
        /// <remarks><p>
        /// Error numbers greater than 2000 refer to MSI "internal" errors, and are always
        /// returned in English.
        /// </p></remarks>
        public static string GetErrorMessage(Record errorRecord) { return Installer.GetErrorMessage(errorRecord, null); }

        /// <summary>
        /// Gets a formatted Windows Installer error message in a specified language.
        /// </summary>
        /// <param name="errorRecord">Error record containing the error number in the first field, and
        /// error-specific parameters in the other fields.</param>
        /// <param name="culture">The locale for the message.</param>
        /// <returns>The message string, or null if the error message or locale is not found.</returns>
        /// <remarks><p>
        /// Error numbers greater than 2000 refer to MSI "internal" errors, and are always
        /// returned in English.
        /// </p></remarks>
        public static string GetErrorMessage(Record errorRecord, CultureInfo culture)
        {
            if (errorRecord == null)
            {
                throw new ArgumentNullException("errorRecord");
            }
            int errorNumber;
            if (errorRecord.FieldCount < 1 || (errorNumber = (int) errorRecord.GetInteger(1)) == 0)
            {
                throw new ArgumentOutOfRangeException("errorRecord");
            }

            string msg = Installer.GetErrorMessage(errorNumber, culture);
            if (msg != null)
            {
                errorRecord.FormatString = msg;
                msg = errorRecord.ToString((IFormatProvider)null);
            }
            return msg;
        }

        /// <summary>
        /// Gets the version string of the path specified using the format that the installer
        /// expects to find it in in the database.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Version string in the "#.#.#.#" format, or an empty string if the file
        /// does not contain version information</returns>
        /// <exception cref="FileNotFoundException">the file does not exist or could not be read</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfileversion.asp">MsiGetFileVersion</a>
        /// </p></remarks>
        public static string GetFileVersion(string path)
        {
            StringBuilder version = new StringBuilder(20);
            uint verBufSize = 0, langBufSize = 0;
            uint ret = NativeMethods.MsiGetFileVersion(path, version, ref verBufSize, null, ref langBufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                version.Capacity = (int) ++verBufSize;
                ret = NativeMethods.MsiGetFileVersion(path, version, ref verBufSize, null, ref langBufSize);
            }

            if (ret != 0 && ret != (uint) NativeMethods.Error.FILE_INVALID)
            {
                if (ret == (uint) NativeMethods.Error.FILE_NOT_FOUND ||
                   ret == (uint) NativeMethods.Error.ACCESS_DENIED)
                {
                    throw new FileNotFoundException(null, path);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
            return version.ToString();
        }

        /// <summary>
        /// Gets the language string of the path specified using the format that the installer
        /// expects to find them in in the database.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Language string in the form of a decimal language ID, or an empty string if the file
        /// does not contain a language ID</returns>
        /// <exception cref="FileNotFoundException">the file does not exist or could not be read</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfileversion.asp">MsiGetFileVersion</a>
        /// </p></remarks>
        public static string GetFileLanguage(string path)
        {
            StringBuilder language = new StringBuilder("", 10);
            uint verBufSize = 0, langBufSize = 0;
            uint ret = NativeMethods.MsiGetFileVersion(path, null, ref verBufSize, language, ref langBufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                language.Capacity = (int) ++langBufSize;
                ret = NativeMethods.MsiGetFileVersion(path, null, ref verBufSize, language, ref langBufSize);
            }

            if (ret != 0 && ret != (uint) NativeMethods.Error.FILE_INVALID)
            {
                if (ret == (uint) NativeMethods.Error.FILE_NOT_FOUND ||
                    ret == (uint) NativeMethods.Error.ACCESS_DENIED)
                {
                    throw new FileNotFoundException(null, path);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
            return language.ToString();
        }

        /// <summary>
        /// Gets a 128-bit hash of the specified file.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="hash">Integer array of length 4 which receives the
        /// four 32-bit parts of the hash value.</param>
        /// <exception cref="FileNotFoundException">the file does not exist or
        /// could not be read</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfilehash.asp">MsiGetFileHash</a>
        /// </p></remarks>
        public static void GetFileHash(string path, int[] hash)
        {
            if (hash == null)
            {
                throw new ArgumentNullException("hash");
            }

            uint[] tempHash = new uint[5];
            tempHash[0] = 20;
            uint ret = NativeMethods.MsiGetFileHash(path, 0, tempHash);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.FILE_NOT_FOUND ||
                    ret == (uint) NativeMethods.Error.ACCESS_DENIED)
                {
                    throw new FileNotFoundException(null, path);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                hash[i] = unchecked ((int) tempHash[i + 1]);
            }
        }

        /// <summary>
        /// Examines a shortcut and returns its product, feature name, and component if available.
        /// </summary>
        /// <param name="shortcut">Full path to a shortcut</param>
        /// <returns>ShortcutTarget structure containing target product code, feature, and component code</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetshortcuttarget.asp">MsiGetShortcutTarget</a>
        /// </p></remarks>
        public static ShortcutTarget GetShortcutTarget(string shortcut)
        {
            StringBuilder productBuf = new StringBuilder(40);
            StringBuilder featureBuf = new StringBuilder(40);
            StringBuilder componentBuf = new StringBuilder(40);

            uint ret = NativeMethods.MsiGetShortcutTarget(shortcut, productBuf, featureBuf, componentBuf);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return new ShortcutTarget(
                productBuf.Length > 0 ? productBuf.ToString() : null,
                featureBuf.Length > 0 ? featureBuf.ToString() : null,
                componentBuf.Length > 0 ? componentBuf.ToString() : null);
        }

        /// <summary>
        /// Verifies that the given file is an installation package.
        /// </summary>
        /// <param name="packagePath">Path to the package</param>
        /// <returns>True if the file is an installation package; false otherwise.</returns>
        /// <exception cref="FileNotFoundException">the specified package file does not exist</exception>
        /// <exception cref="InstallerException">the package file could not be opened</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiverifypackage.asp">MsiVerifyPackage</a>
        /// </p></remarks>
        public static bool VerifyPackage(string packagePath)
        {
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentNullException("packagePath");
            }

            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException(null, packagePath);
            }

            uint ret = NativeMethods.MsiVerifyPackage(packagePath);
            if (ret == (uint) NativeMethods.Error.INSTALL_PACKAGE_INVALID)
            {
                return false;
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return true;
        }

        /// <summary>
        /// [MSI 4.0] Gets the list of files that can be updated by one or more patches.
        /// </summary>
        /// <param name="productCode">ProductCode (GUID) of the product which is
        /// the target of the patches</param>
        /// <param name="patches">list of file paths of one or more patches to be
        /// analyzed</param>
        /// <returns>List of absolute paths of files that can be updated when the
        /// patches are applied on this system.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetpatchfilelist.asp">MsiGetPatchFileList</a>
        /// </p></remarks>
        public static IList<string> GetPatchFileList(string productCode, IList<string> patches)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                throw new ArgumentNullException("productCode");
            }

            if (patches == null || patches.Count == 0)
            {
                throw new ArgumentNullException("patches");
            }

            StringBuilder patchList = new StringBuilder();
            foreach (string patch in patches)
            {
                if (patch != null)
                {
                    if (patchList.Length != 0)
                    {
                        patchList.Append(';');
                    }

                    patchList.Append(patch);
                }
            }

            if (patchList.Length == 0)
            {
                throw new ArgumentNullException("patches");
            }

            IntPtr phFileRecords;
            uint cFiles;

            uint ret = NativeMethods.MsiGetPatchFileList(
                productCode,
                patchList.ToString(),
                out cFiles,
                out phFileRecords);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }

            List<string> files = new List<string>();

            for (uint i = 0; i < cFiles; i++)
            {
                int hFileRec = Marshal.ReadInt32(phFileRecords, (int) i);

                using (Record fileRec = new Record(hFileRec, true, null))
                {
                    files.Add(fileRec.GetString(1));
                }
            }

            return files;
        }
    }
}
