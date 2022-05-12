// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;
using System.Runtime.InteropServices;
using DWORD = System.UInt32;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the possible status when validating integrity of catalog.
    /// </summary>
    public enum CatalogValidationStatus
    {
        /// <summary>
        /// Status when catalog is not tampered.
        /// </summary>
        Valid,

        /// <summary>
        /// Status when catalog is tampered.
        /// </summary>
        ValidationFailed
    }

    /// <summary>
    /// Object returned by Catalog Cmdlets.
    /// </summary>
    public class CatalogInformation
    {
        /// <summary>
        /// Status of catalog.
        /// </summary>
        public CatalogValidationStatus Status { get; set; }

        /// <summary>
        /// Hash Algorithm used to calculate the hashes of files in Catalog.
        /// </summary>
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Dictionary mapping files relative paths to their hash values found from Catalog.
        /// </summary>
        public Dictionary<string, string> CatalogItems { get; set; }

        /// <summary>
        /// Dictionary mapping files relative paths to their hash values.
        /// </summary>
        public Dictionary<string, string> PathItems { get; set; }

        /// <summary>
        /// Signature for the catalog.
        /// </summary>
        public Signature Signature { get; set; }
    }

    /// <summary>
    /// Helper functions for Windows Catalog functionality.
    /// </summary>
    internal static class CatalogHelper
    {
        // Catalog Version is (0X100 = 256) for Catalog Version 1
        private const int catalogVersion1 = 256;

        // Catalog Version is (0X200 = 512) for Catalog Version 2
        private const int catalogVersion2 = 512;

        // Hash Algorithms supported by Windows Catalog
        private const string HashAlgorithmSHA1 = "SHA1";
        private const string HashAlgorithmSHA256 = "SHA256";
        private static PSCmdlet _cmdlet = null;

        /// <summary>
        /// Find out the Version of Catalog by reading its Meta data. We can have either version 1 or version 2 catalog.
        /// </summary>
        /// <param name="catalogHandle">Handle to open catalog file.</param>
        /// <returns>Version of the catalog.</returns>
        private static int GetCatalogVersion(IntPtr catalogHandle)
        {
            int catalogVersion = -1;

            IntPtr catalogData = NativeMethods.CryptCATStoreFromHandle(catalogHandle);
            NativeMethods.CRYPTCATSTORE catalogInfo = Marshal.PtrToStructure<NativeMethods.CRYPTCATSTORE>(catalogData);

            if (catalogInfo.dwPublicVersion == catalogVersion2)
            {
                catalogVersion = 2;
            }
            // One Windows 7 this API sent version information as decimal 1 not hex (0X100 = 256)
            // so we are checking for that value as well. Reason we are not checking for version 2 above in
            // this scenario because catalog version 2 is not supported on win7.
            else if ((catalogInfo.dwPublicVersion == catalogVersion1) || (catalogInfo.dwPublicVersion == 1))
            {
                catalogVersion = 1;
            }
            else
            {
                // catalog version we don't understand
                Exception exception = new InvalidOperationException(StringUtil.Format(CatalogStrings.UnKnownCatalogVersion,
                                      catalogVersion1.ToString("X"),
                                      catalogVersion2.ToString("X")));

                ErrorRecord errorRecord = new ErrorRecord(exception, "UnKnownCatalogVersion", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }

            return catalogVersion;
        }

        /// <summary>
        /// HashAlgorithm used by the Catalog. It is based on the version of Catalog.
        /// </summary>
        /// <param name="catalogVersion">Path of the output catalog file.</param>
        /// <returns>Version of the catalog.</returns>
        private static string GetCatalogHashAlgorithm(int catalogVersion)
        {
            string hashAlgorithm = string.Empty;

            if (catalogVersion == 1)
            {
                hashAlgorithm = HashAlgorithmSHA1;
            }
            else if (catalogVersion == 2)
            {
                hashAlgorithm = HashAlgorithmSHA256;
            }
            else
            {
                // version we don't understand
                Exception exception = new InvalidOperationException(StringUtil.Format(CatalogStrings.UnKnownCatalogVersion,
                                      "1.0",
                                      "2.0"));

                ErrorRecord errorRecord = new ErrorRecord(exception, "UnKnownCatalogVersion", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }

            return hashAlgorithm;
        }

        /// <summary>
        /// Generate the Catalog Definition File representing files and folders.
        /// </summary>
        /// <param name="Path">Path of expected output .cdf file.</param>
        /// <param name="catalogFilePath">Path of the output catalog file.</param>
        /// <param name="cdfFilePath">Path of the catalog definition file.</param>
        /// <param name="catalogVersion">Version of catalog.</param>
        /// <param name="hashAlgorithm">Hash method used to generate hashes for the Catalog.</param>
        /// <returns>HashSet for the relative Path for files in Catalog.</returns>
        internal static string GenerateCDFFile(Collection<string> Path, string catalogFilePath, string cdfFilePath, int catalogVersion, string hashAlgorithm)
        {
            HashSet<string> relativePaths = new HashSet<string>();

            string cdfHeaderContent = string.Empty;
            string cdfFilesContent = string.Empty;
            int catAttributeCount = 0;

            // First create header and files section for the catalog then write in file
            cdfHeaderContent += "[CatalogHeader]" + Environment.NewLine;
            cdfHeaderContent += @"Name=" + catalogFilePath + Environment.NewLine;
            cdfHeaderContent += "CatalogVersion=" + catalogVersion + Environment.NewLine;
            cdfHeaderContent += "HashAlgorithms=" + hashAlgorithm + Environment.NewLine;
            cdfFilesContent += "[CatalogFiles]" + Environment.NewLine;

            foreach (string catalogFile in Path)
            {
                if (System.IO.Directory.Exists(catalogFile))
                {
                    var directoryItems = Directory.EnumerateFiles(catalogFile, "*.*", SearchOption.AllDirectories);
                    foreach (string fileItem in directoryItems)
                    {
                        ProcessFileToBeAddedInCatalogDefinitionFile(new FileInfo(fileItem), new DirectoryInfo(catalogFile), ref relativePaths, ref cdfHeaderContent, ref cdfFilesContent, ref catAttributeCount);
                    }
                }
                else if (System.IO.File.Exists(catalogFile))
                {
                    ProcessFileToBeAddedInCatalogDefinitionFile(new FileInfo(catalogFile), null, ref relativePaths, ref cdfHeaderContent, ref cdfFilesContent, ref catAttributeCount);
                }
            }

            using (System.IO.StreamWriter fileWriter = new System.IO.StreamWriter(new FileStream(cdfFilePath, FileMode.Create)))
            {
                fileWriter.WriteLine(cdfHeaderContent);
                fileWriter.WriteLine();
                fileWriter.WriteLine(cdfFilesContent);
            }

            return cdfFilePath;
        }

        /// <summary>
        /// Get file attribute (Relative path in our case) from catalog.
        /// </summary>
        /// <param name="fileToHash">File to hash.</param>
        /// <param name="dirInfo">Directory information about file needed to calculate relative file path.</param>
        /// <param name="relativePaths">Working set of relative paths of all files.</param>
        /// <param name="cdfHeaderContent">Content to be added in CatalogHeader section of cdf File.</param>
        /// <param name="cdfFilesContent">Content to be added in CatalogFiles section of cdf File.</param>
        /// <param name="catAttributeCount">Indicating the current no of catalog header level attributes.</param>
        /// <returns>Void.</returns>
        internal static void ProcessFileToBeAddedInCatalogDefinitionFile(FileInfo fileToHash, DirectoryInfo dirInfo, ref HashSet<string> relativePaths, ref string cdfHeaderContent, ref string cdfFilesContent, ref int catAttributeCount)
        {
            string relativePath = string.Empty;

            if (dirInfo != null)
            {
                // Relative path of the file is the path inside the containing folder excluding folder Name
                relativePath = fileToHash.FullName.AsSpan(dirInfo.FullName.Length).TrimStart('\\').ToString();
            }
            else
            {
                relativePath = fileToHash.Name;
            }

            if (!relativePaths.Contains(relativePath))
            {
                relativePaths.Add(relativePath);
                if (fileToHash.Length != 0)
                {
                    cdfFilesContent += "<HASH>" + fileToHash.FullName + "=" + fileToHash.FullName + Environment.NewLine;
                    cdfFilesContent += "<HASH>" + fileToHash.FullName + "ATTR1=0x10010001:FilePath:" + relativePath + Environment.NewLine;
                }
                else
                {
                    // zero length files are added as catalog level attributes because they can not be hashed
                    cdfHeaderContent += "CATATTR" + (++catAttributeCount) + "=0x10010001:FilePath:" + relativePath + Environment.NewLine;
                }
            }
            else
            {
                // If Files have same relative paths we can not distinguish them for
                // Validation. So failing.
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.FoundDuplicateFilesRelativePath, relativePath)), "FoundDuplicateFilesRelativePath", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }
        }
        /// <summary>
        /// Generate the Catalog file for Input Catalog Definition File.
        /// </summary>
        /// <param name="cdfFilePath">Path to the Input .cdf file.</param>
        internal static void GenerateCatalogFile(string cdfFilePath)
        {
            string pwszFilePath = cdfFilePath;
            NativeMethods.CryptCATCDFOpenCallBack catOpenCallBack = new NativeMethods.CryptCATCDFOpenCallBack(ParseErrorCallback);

            // Open CDF File
            IntPtr resultCDF = NativeMethods.CryptCATCDFOpen(pwszFilePath, catOpenCallBack);

            // navigate CDF header and files sections
            if (resultCDF != IntPtr.Zero)
            {
                // First navigate all catalog level attributes entries first, they represent zero size files
                IntPtr catalogAttr = IntPtr.Zero;
                do
                {
                    catalogAttr = NativeMethods.CryptCATCDFEnumCatAttributes(resultCDF, catalogAttr, catOpenCallBack);

                    if (catalogAttr != IntPtr.Zero)
                    {
                        string filePath = ProcessFilePathAttributeInCatalog(catalogAttr);
                        _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.AddFileToCatalog, filePath, filePath));
                    }
                } while (catalogAttr != IntPtr.Zero);

                // navigate all the files hash entries in the .cdf file
                IntPtr memberInfo = IntPtr.Zero;
                try
                {
                    IntPtr memberFile = IntPtr.Zero;
                    NativeMethods.CryptCATCDFEnumMembersByCDFTagExErrorCallBack memberCallBack = new NativeMethods.CryptCATCDFEnumMembersByCDFTagExErrorCallBack(ParseErrorCallback);
                    string fileName = string.Empty;
                    do
                    {
                        memberFile = NativeMethods.CryptCATCDFEnumMembersByCDFTagEx(resultCDF, memberFile, memberCallBack, ref memberInfo, true, IntPtr.Zero);
                        fileName = Marshal.PtrToStringUni(memberFile);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            IntPtr memberAttr = IntPtr.Zero;
                            string fileRelativePath = string.Empty;
                            do
                            {
                                memberAttr = NativeMethods.CryptCATCDFEnumAttributesWithCDFTag(resultCDF, memberFile, memberInfo, memberAttr, memberCallBack);

                                if (memberAttr != IntPtr.Zero)
                                {
                                    fileRelativePath = ProcessFilePathAttributeInCatalog(memberAttr);
                                    if (!string.IsNullOrEmpty(fileRelativePath))
                                    {
                                        // Found the attribute we are looking for
                                        // Filename we read from the above API has <Hash> appended to its name as per CDF file tags convention
                                        // Truncating that Information from the string.
                                        string itemName = fileName.Substring(6);
                                        _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.AddFileToCatalog, itemName, fileRelativePath));
                                        break;
                                    }
                                }
                            } while (memberAttr != IntPtr.Zero);
                        }
                    } while (fileName != null);
                }
                finally
                {
                    NativeMethods.CryptCATCDFClose(resultCDF);
                }
            }
            else
            {
                // If we are not able to open CDF file we can not continue generating catalog
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(CatalogStrings.UnableToOpenCatalogDefinitionFile), "UnableToOpenCatalogDefinitionFile", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }
        }

        /// <summary>
        /// To generate Catalog for the folder.
        /// </summary>
        /// <param name="Path">Path to folder or File.</param>
        /// <param name="catalogFilePath">Catalog File Path.</param>
        /// <param name="catalogVersion">Catalog File Path.</param>
        /// <param name="cmdlet">Instance of cmdlet calling this method.</param>
        /// <returns>True if able to generate .cat file or false.</returns>
        internal static FileInfo GenerateCatalog(PSCmdlet cmdlet, Collection<string> Path, string catalogFilePath, int catalogVersion)
        {
            _cmdlet = cmdlet;
            string hashAlgorithm = GetCatalogHashAlgorithm(catalogVersion);

            if (!string.IsNullOrEmpty(hashAlgorithm))
            {
                // Generate Path for Catalog Definition File
                string cdfFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                cdfFilePath += ".cdf";
                try
                {
                    cdfFilePath = GenerateCDFFile(Path, catalogFilePath, cdfFilePath, catalogVersion, hashAlgorithm);

                    if (!File.Exists(cdfFilePath))
                    {
                        // If we are not able to generate catalog definition file we can not continue generating catalog
                        // throw PSTraceSource.NewInvalidOperationException("catalog", CatalogStrings.CatalogDefinitionFileNotGenerated);
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(CatalogStrings.CatalogDefinitionFileNotGenerated), "CatalogDefinitionFileNotGenerated", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                    }

                    GenerateCatalogFile(cdfFilePath);
                    if (File.Exists(catalogFilePath))
                    {
                        return new FileInfo(catalogFilePath);
                    }
                }
                finally
                {
                    File.Delete(cdfFilePath);
                }
            }

            return null;
        }

        /// <summary>
        /// Get file attribute (Relative path in our case) from catalog.
        /// </summary>
        /// <param name="memberAttrInfo">Pointer to current attribute of catalog member.</param>
        /// <returns>Value of the attribute.</returns>
        internal static string ProcessFilePathAttributeInCatalog(IntPtr memberAttrInfo)
        {
            string relativePath = string.Empty;

            NativeMethods.CRYPTCATATTRIBUTE currentMemberAttr = Marshal.PtrToStructure<NativeMethods.CRYPTCATATTRIBUTE>(memberAttrInfo);

            // check if this is the attribute we are looking for
            // catalog generated other way not using New-FileCatalog can have attributes we don't understand
            if (currentMemberAttr.pwszReferenceTag.Equals("FilePath", StringComparison.OrdinalIgnoreCase))
            {
                // find the size for the current attribute value and then allocate buffer and copy from byte array
                int attrValueSize = (int)currentMemberAttr.cbValue;
                byte[] attrValue = new byte[attrValueSize];
                Marshal.Copy(currentMemberAttr.pbValue, attrValue, 0, attrValueSize);
                relativePath = System.Text.Encoding.Unicode.GetString(attrValue);
                relativePath = relativePath.TrimEnd('\0');
            }

            return relativePath;
        }

        /// <summary>
        /// Make a hash for the file.
        /// </summary>
        /// <param name="filePath">Path of the file.</param>
        /// <param name="hashAlgorithm">Used to calculate Hash.</param>
        /// <returns>HashValue for the file.</returns>
        internal static string CalculateFileHash(string filePath, string hashAlgorithm)
        {
            string hashValue = string.Empty;
            IntPtr catAdmin = IntPtr.Zero;

            // To get handle to the hash algorithm to be used to calculate hashes
            if (!NativeMethods.CryptCATAdminAcquireContext2(ref catAdmin, IntPtr.Zero, hashAlgorithm, IntPtr.Zero, 0))
            {
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToAcquireHashAlgorithmContext, hashAlgorithm)), "UnableToAcquireHashAlgorithmContext", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }

            const DWORD GENERIC_READ = 0x80000000;
            const DWORD OPEN_EXISTING = 3;
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            // Open the file that is to be hashed for reading and get its handle
            IntPtr fileHandle = NativeMethods.CreateFile(filePath, GENERIC_READ, 0, 0, OPEN_EXISTING, 0, IntPtr.Zero);
            if (fileHandle != INVALID_HANDLE_VALUE)
            {
                try
                {
                    DWORD hashBufferSize = 0;
                    IntPtr hashBuffer = IntPtr.Zero;

                    // Call first time to get the size of expected buffer to hold new hash value
                    if (!NativeMethods.CryptCATAdminCalcHashFromFileHandle2(catAdmin, fileHandle, ref hashBufferSize, hashBuffer, 0))
                    {
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToCreateFileHash, filePath)), "UnableToCreateFileHash", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                    }

                    int size = (int)hashBufferSize;
                    hashBuffer = Marshal.AllocHGlobal(size);
                    try
                    {
                        // Call second time to actually get the hash value
                        if (!NativeMethods.CryptCATAdminCalcHashFromFileHandle2(catAdmin, fileHandle, ref hashBufferSize, hashBuffer, 0))
                        {
                            ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToCreateFileHash, filePath)), "UnableToCreateFileHash", ErrorCategory.InvalidOperation, null);
                            _cmdlet.ThrowTerminatingError(errorRecord);
                        }

                        byte[] hashBytes = new byte[size];
                        Marshal.Copy(hashBuffer, hashBytes, 0, size);
                        hashValue = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
                    }
                    finally
                    {
                        if (hashBuffer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(hashBuffer);
                        }
                    }
                }
                finally
                {
                    NativeMethods.CryptCATAdminReleaseContext(catAdmin, 0);
                    NativeMethods.CloseHandle(fileHandle);
                }
            }
            else
            {
                // If we are not able to open file that is to be hashed we can not continue with catalog validation
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToReadFileToHash, filePath)), "UnableToReadFileToHash", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }

            return hashValue;
        }

        /// <summary>
        /// Make list of hashes for given Catalog File.
        /// </summary>
        /// <param name="catalogFilePath">Path to the folder having catalog file.</param>
        /// <param name="excludedPatterns"></param>
        /// <param name="catalogVersion">The version of input catalog we read from catalog meta data after opening it.</param>
        /// <returns>Dictionary mapping files relative paths to HashValues.</returns>
        internal static Dictionary<string, string> GetHashesFromCatalog(string catalogFilePath, WildcardPattern[] excludedPatterns, out int catalogVersion)
        {
            IntPtr resultCatalog = NativeMethods.CryptCATOpen(catalogFilePath, 0, IntPtr.Zero, 1, 0);
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            Dictionary<string, string> catalogHashes = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            catalogVersion = 0;

            if (resultCatalog != INVALID_HANDLE_VALUE)
            {
                try
                {
                    IntPtr catAttrInfo = IntPtr.Zero;

                    // First traverse all catalog level attributes to get information about zero size file.
                    do
                    {
                        catAttrInfo = NativeMethods.CryptCATEnumerateCatAttr(resultCatalog, catAttrInfo);

                        // If we found attribute it is a file information retrieve its relative path
                        // and add it to catalog hash collection if its not in excluded files criteria
                        if (catAttrInfo != IntPtr.Zero)
                        {
                            string relativePath = ProcessFilePathAttributeInCatalog(catAttrInfo);
                            if (!string.IsNullOrEmpty(relativePath))
                            {
                                ProcessCatalogFile(relativePath, string.Empty, excludedPatterns, ref catalogHashes);
                            }
                        }
                    } while (catAttrInfo != IntPtr.Zero);

                    catalogVersion = GetCatalogVersion(resultCatalog);

                    IntPtr memberInfo = IntPtr.Zero;
                    // Next Navigate all members in Catalog files and get their relative paths and hashes
                    do
                    {
                        memberInfo = NativeMethods.CryptCATEnumerateMember(resultCatalog, memberInfo);
                        if (memberInfo != IntPtr.Zero)
                        {
                            NativeMethods.CRYPTCATMEMBER currentMember = Marshal.PtrToStructure<NativeMethods.CRYPTCATMEMBER>(memberInfo);
                            NativeMethods.SIP_INDIRECT_DATA pIndirectData = Marshal.PtrToStructure<NativeMethods.SIP_INDIRECT_DATA>(currentMember.pIndirectData);

                            // For Catalog version 2 CryptoAPI puts hashes of file attributes(relative path in our case) in Catalog as well
                            // We validate those along with file hashes so we are skipping duplicate entries
                            if (!((catalogVersion == 2) && (pIndirectData.DigestAlgorithm.pszObjId.Equals(new Oid("SHA1").Value, StringComparison.OrdinalIgnoreCase))))
                            {
                                string relativePath = string.Empty;
                                IntPtr memberAttrInfo = IntPtr.Zero;
                                do
                                {
                                    memberAttrInfo = NativeMethods.CryptCATEnumerateAttr(resultCatalog, memberInfo, memberAttrInfo);

                                    if (memberAttrInfo != IntPtr.Zero)
                                    {
                                        relativePath = ProcessFilePathAttributeInCatalog(memberAttrInfo);
                                        if (!string.IsNullOrEmpty(relativePath))
                                        {
                                            break;
                                        }
                                    }
                                }
                                while (memberAttrInfo != IntPtr.Zero);

                                // If we did not find any Relative Path for the item in catalog we should quit
                                // This catalog must not be valid for our use as catalogs generated using New-FileCatalog
                                // always contains relative file Paths
                                if (string.IsNullOrEmpty(relativePath))
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToOpenCatalogFile, catalogFilePath)), "UnableToOpenCatalogFile", ErrorCategory.InvalidOperation, null);
                                    _cmdlet.ThrowTerminatingError(errorRecord);
                                }

                                ProcessCatalogFile(relativePath, currentMember.pwszReferenceTag, excludedPatterns, ref catalogHashes);
                            }
                        }
                    } while (memberInfo != IntPtr.Zero);
                }
                finally
                {
                    NativeMethods.CryptCATClose(resultCatalog);
                }
            }
            else
            {
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToOpenCatalogFile, catalogFilePath)), "UnableToOpenCatalogFile", ErrorCategory.InvalidOperation, null);
                _cmdlet.ThrowTerminatingError(errorRecord);
            }

            return catalogHashes;
        }

        /// <summary>
        /// Process file in path for its relative paths.
        /// </summary>
        /// <param name="relativePath">Relative path of file found in catalog.</param>
        /// <param name="fileHash">Hash of file found in catalog.</param>
        /// <param name="excludedPatterns">Skip file from validation if it matches these patterns.</param>
        /// <param name="catalogHashes">Collection of hashes of catalog.</param>
        /// <returns>Void.</returns>
        internal static void ProcessCatalogFile(string relativePath, string fileHash, WildcardPattern[] excludedPatterns, ref Dictionary<string, string> catalogHashes)
        {
            // Found the attribute we are looking for
            _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.FoundFileHashInCatalogItem, relativePath, fileHash));

            // Only add the file for validation if it does not meet exclusion criteria
            if (!CheckExcludedCriteria((new FileInfo(relativePath)).Name, excludedPatterns))
            {
                // Add relativePath mapping to hashvalue for each file
                catalogHashes.Add(relativePath, fileHash);
            }
            else
            {
                // Verbose about skipping file from catalog
                _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.SkipValidationOfCatalogFile, relativePath));
            }
        }
        /// <summary>
        /// Process file in path for its relative paths.
        /// </summary>
        /// <param name="fileToHash">File to hash.</param>
        /// <param name="dirInfo">Directory information about file needed to calculate relative file path.</param>
        /// <param name="hashAlgorithm">Used to calculate Hash.</param>
        /// <param name="excludedPatterns">Skip file if it matches these patterns.</param>
        /// <param name="fileHashes">Collection of hashes of files.</param>
        /// <returns>Void.</returns>
        internal static void ProcessPathFile(FileInfo fileToHash, DirectoryInfo dirInfo, string hashAlgorithm, WildcardPattern[] excludedPatterns, ref Dictionary<string, string> fileHashes)
        {
            string relativePath = string.Empty;
            string exclude = string.Empty;

            if (dirInfo != null)
            {
                // Relative path of the file is the path inside the containing folder excluding folder Name
                relativePath = fileToHash.FullName.AsSpan(dirInfo.FullName.Length).TrimStart('\\').ToString();
                exclude = fileToHash.Name;
            }
            else
            {
                relativePath = fileToHash.Name;
                exclude = relativePath;
            }

            if (!CheckExcludedCriteria(exclude, excludedPatterns))
            {
                string fileHash = string.Empty;

                if (fileToHash.Length != 0)
                {
                    fileHash = CalculateFileHash(fileToHash.FullName, hashAlgorithm);
                }

                if (fileHashes.TryAdd(relativePath, fileHash))
                {
                    _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.FoundFileInPath, relativePath, fileHash));
                }
                else
                {
                    ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.FoundDuplicateFilesRelativePath, relativePath)), "FoundDuplicateFilesRelativePath", ErrorCategory.InvalidOperation, null);
                    _cmdlet.ThrowTerminatingError(errorRecord);
                }
            }
            else
            {
                // Verbose about skipping file from path
                _cmdlet.WriteVerbose(StringUtil.Format(CatalogStrings.SkipValidationOfPathFile, relativePath));
            }
        }

        /// <summary>
        /// Generate the hashes of all the files in given folder.
        /// </summary>
        /// <param name="folderPaths">Path to folder or File.</param>
        /// <param name="catalogFilePath">Catalog file path it should be skipped when calculating the hashes.</param>
        /// <param name="hashAlgorithm">Used to calculate Hash.</param>
        /// <param name="excludedPatterns"></param>
        /// <returns>Dictionary mapping file relative paths to hashes..</returns>
        internal static Dictionary<string, string> CalculateHashesFromPath(Collection<string> folderPaths, string catalogFilePath, string hashAlgorithm, WildcardPattern[] excludedPatterns)
        {
            // Create a HashTable of file Hashes
            Dictionary<string, string> fileHashes = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string folderPath in folderPaths)
            {
                if (System.IO.Directory.Exists(folderPath))
                {
                    var directoryItems = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories);
                    foreach (string fileItem in directoryItems)
                    {
                        // if its the catalog file we are validating we will skip it
                        if (string.Equals(fileItem, catalogFilePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        ProcessPathFile(new FileInfo(fileItem), new DirectoryInfo(folderPath), hashAlgorithm, excludedPatterns, ref fileHashes);
                    }
                }
                else if (System.IO.File.Exists(folderPath))
                {
                    ProcessPathFile(new FileInfo(folderPath), null, hashAlgorithm, excludedPatterns, ref fileHashes);
                }
            }

            return fileHashes;
        }

        /// <summary>
        /// Compare Dictionary objects.
        /// </summary>
        /// <param name="catalogItems">Hashes extracted from Catalog.</param>
        /// <param name="pathItems">Hashes created from folders path.</param>
        /// <returns>True if both collections are same.</returns>
        internal static bool CompareDictionaries(Dictionary<string, string> catalogItems, Dictionary<string, string> pathItems)
        {
            bool Status = true;

            List<string> relativePathsFromFolder = pathItems.Keys.ToList();
            List<string> relativePathsFromCatalog = catalogItems.Keys.ToList();

            // Find entires those are not in both list lists. These should be empty lists for success
            // Hashes in Catalog should be exact similar to the ones from folder
            List<string> relativePathsNotInFolder = relativePathsFromFolder.Except(relativePathsFromCatalog, StringComparer.CurrentCultureIgnoreCase).ToList();
            List<string> relativePathsNotInCatalog = relativePathsFromCatalog.Except(relativePathsFromFolder, StringComparer.CurrentCultureIgnoreCase).ToList();

            // Found extra hashes in Folder
            if ((relativePathsNotInFolder.Count != 0) || (relativePathsNotInCatalog.Count != 0))
            {
                Status = false;
            }

            foreach (KeyValuePair<string, string> item in catalogItems)
            {
                string catalogHashValue = (string)catalogItems[item.Key];
                if (pathItems.ContainsKey(item.Key))
                {
                    string folderHashValue = (string)pathItems[item.Key];
                    if (folderHashValue.Equals(catalogHashValue))
                    {
                        continue;
                    }
                    else
                    {
                        Status = false;
                    }
                }
            }

            return Status;
        }
        /// <summary>
        /// To Validate the Integrity of Catalog.
        /// </summary>
        /// <param name="catalogFolders">Folder for which catalog is created.</param>
        /// <param name="catalogFilePath">File Name of the Catalog.</param>
        /// <param name="excludedPatterns"></param>
        /// <param name="cmdlet">Instance of cmdlet calling this method.</param>
        /// <returns>Information about Catalog.</returns>
        internal static CatalogInformation ValidateCatalog(PSCmdlet cmdlet, Collection<string> catalogFolders, string catalogFilePath, WildcardPattern[] excludedPatterns)
        {
            _cmdlet = cmdlet;
            int catalogVersion = 0;
            Dictionary<string, string> catalogHashes = GetHashesFromCatalog(catalogFilePath, excludedPatterns, out catalogVersion);
            string hashAlgorithm = GetCatalogHashAlgorithm(catalogVersion);

            if (!string.IsNullOrEmpty(hashAlgorithm))
            {
                Dictionary<string, string> fileHashes = CalculateHashesFromPath(catalogFolders, catalogFilePath, hashAlgorithm, excludedPatterns);
                CatalogInformation catalog = new CatalogInformation();
                catalog.CatalogItems = catalogHashes;
                catalog.PathItems = fileHashes;
                bool status = CompareDictionaries(catalogHashes, fileHashes);
                if (status)
                {
                    catalog.Status = CatalogValidationStatus.Valid;
                }
                else
                {
                    catalog.Status = CatalogValidationStatus.ValidationFailed;
                }

                catalog.HashAlgorithm = hashAlgorithm;
                catalog.Signature = SignatureHelper.GetSignature(catalogFilePath, null);
                return catalog;
            }

            return null;
        }

        /// <summary>
        /// Check if file meets the skip validation criteria.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="excludedPatterns"></param>
        /// <returns>True if match is found else false.</returns>
        internal static bool CheckExcludedCriteria(string filename, WildcardPattern[] excludedPatterns)
        {
            if (excludedPatterns != null)
            {
                foreach (WildcardPattern patternItem in excludedPatterns)
                {
                    if (patternItem.IsMatch(filename))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Call back when error is thrown by catalog API's.
        /// </summary>
        private static void ParseErrorCallback(DWORD dwErrorArea, DWORD dwLocalError, string pwszLine)
        {
            switch (dwErrorArea)
            {
                case NativeConstants.CRYPTCAT_E_AREA_HEADER: break;
                case NativeConstants.CRYPTCAT_E_AREA_MEMBER: break;
                case NativeConstants.CRYPTCAT_E_AREA_ATTRIBUTE: break;
                default: break;
            }

            switch (dwLocalError)
            {
                case NativeConstants.CRYPTCAT_E_CDF_MEMBER_FILE_PATH:
                    {
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToFindFileNameOrPathForCatalogMember, pwszLine)), "UnableToFindFileNameOrPathForCatalogMember", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                        break;
                    }
                case NativeConstants.CRYPTCAT_E_CDF_MEMBER_INDIRECTDATA:
                    {
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToCreateFileHash, pwszLine)), "UnableToCreateFileHash", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                        break;
                    }
                case NativeConstants.CRYPTCAT_E_CDF_MEMBER_FILENOTFOUND:
                    {
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.UnableToFindFileToHash, pwszLine)), "UnableToFindFileToHash", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                        break;
                    }
                case NativeConstants.CRYPTCAT_E_CDF_BAD_GUID_CONV: break;
                case NativeConstants.CRYPTCAT_E_CDF_ATTR_TYPECOMBO: break;
                case NativeConstants.CRYPTCAT_E_CDF_ATTR_TOOFEWVALUES: break;
                case NativeConstants.CRYPTCAT_E_CDF_UNSUPPORTED: break;
                case NativeConstants.CRYPTCAT_E_CDF_DUPLICATE:
                    {
                        ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(CatalogStrings.FoundDuplicateFileMemberInCatalog, pwszLine)), "FoundDuplicateFileMemberInCatalog", ErrorCategory.InvalidOperation, null);
                        _cmdlet.ThrowTerminatingError(errorRecord);
                        break;
                    }
                case NativeConstants.CRYPTCAT_E_CDF_TAGNOTFOUND: break;
                default: break;
            }
        }
    }
}

#endif
