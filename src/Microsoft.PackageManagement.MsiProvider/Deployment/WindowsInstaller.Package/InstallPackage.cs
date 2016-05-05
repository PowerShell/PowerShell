//---------------------------------------------------------------------
// <copyright file="InstallPackage.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Package
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using Archivers.Internal.Compression;
    using Archivers.Internal.Compression.Cab;
    using FileAttributes = WindowsInstaller.FileAttributes;

    /// <summary>
/// Handles status messages generated when operations are performed on an
/// <see cref="InstallPackage"/> or <see cref="PatchPackage"/>.
/// </summary>
/// <example>
/// <c>installPackage.Message += new InstallPackageMessageHandler(Console.WriteLine);</c>
/// </example>
public delegate void InstallPackageMessageHandler(string format, params object[] args);

/// <summary>
/// Provides access to powerful build, maintenance, and analysis operations on an
/// installation package (.MSI or .MSM).
/// </summary>
internal class InstallPackage : Database
{
    private string cabName;
    private string cabMsg;

    /// <summary>
    /// Creates a new InstallPackage object.  The file source directory and working
    /// directory are the same as the location as the package file.
    /// </summary>
    /// <param name="packagePath">Path to the install package to be created or opened</param>
    /// <param name="openMode">Open mode for the database</param>
    public InstallPackage(string packagePath, DatabaseOpenMode openMode)
        : this(packagePath, openMode, null, null)
    {
    }
    /// <summary>
    /// Creates a new InstallPackage object, specifying an alternate file source
    /// directory and/or working directory.
    /// </summary>
    /// <param name="packagePath">Path to the install package to be created or opened</param>
    /// <param name="openMode">Open mode for the database</param>
    /// <param name="sourceDir">Location to obtain source files and cabinets when extracting
    /// or updating files in the working directory. This is often the location of an original
    /// copy of the package that is not meant to be modified. If this parameter is null, it
    /// defaults to the directory of <paramref name="packagePath"/>.</param>
    /// <param name="workingDir">Location where files will be extracted to/updated from. Also
    /// the location where a temporary folder is created during some operations. If this
    /// parameter is null, it defaults to the directory of <paramref name="packagePath"/>.</param>
    /// <remarks>If the source location is different than the working directory, then
    /// no files will be modified at the source location.
    /// </remarks>
    public InstallPackage(string packagePath, DatabaseOpenMode openMode,
        string sourceDir, string workingDir) : base(packagePath, openMode)
    {
        this.sourceDir  = (sourceDir  != null ? sourceDir  : Path.GetDirectoryName(packagePath));
        this.workingDir = (workingDir != null ? workingDir : Path.GetDirectoryName(packagePath));
        this.compressionLevel = CompressionLevel.Normal;

        this.DeleteOnClose(this.TempDirectory);
    }

    /// <summary>
    /// Handle this event to receive status messages when operations are performed
    /// on the install package.
    /// </summary>
    /// <example>
    /// <c>installPackage.Message += new InstallPackageMessageHandler(Console.WriteLine);</c>
    /// </example>
    public event InstallPackageMessageHandler Message;

    /// <summary>
    /// Sends a message to the <see cref="Message"/> event-handler.
    /// </summary>
    /// <param name="format">Message string, containing 0 or more format items</param>
    /// <param name="args">Items to be formatted</param>
    protected void LogMessage(string format, params object[] args)
    {
        if(this.Message != null)
        {
            this.Message(format, args);
        }
    }

    /// <summary>
    /// Gets or sets the location to obtain source files and cabinets when
    /// extracting or updating files in the working directory. This is often
    /// the location of an original copy of the package that is not meant
    /// to be modified.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string SourceDirectory
    {
        get { return this.sourceDir; }
        set { this.sourceDir = value; }
    }
    private string sourceDir;

    /// <summary>
    /// Gets or sets the location where files will be extracted to/updated from. Also
    /// the location where a temporary folder is created during some operations.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string WorkingDirectory
    {
        get { return this.workingDir; }
        set { this.workingDir = value; }
    }
    private string workingDir;

    private const string TEMP_DIR_NAME = "WITEMP";

    private string TempDirectory
    {
        get { return Path.Combine(this.WorkingDirectory, TEMP_DIR_NAME); }
    }

    /// <summary>
    /// Gets the list of file keys that have the specified long file name.
    /// </summary>
    /// <param name="longFileName">File name to search for (case-insensitive)</param>
    /// <returns>Array of file keys, or a 0-length array if none are found</returns>
    [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string[] FindFiles(string longFileName)
    {
        if (longFileName == null) {
            throw new ArgumentNullException("longFileName");
        }
        longFileName = longFileName.ToLowerInvariant();
        ArrayList fileList = new ArrayList();
        foreach(KeyValuePair<string, InstallPath> entry in this.Files)
        {
            if(((InstallPath) entry.Value).TargetName.ToLowerInvariant()
               == longFileName)
            {
                fileList.Add(entry.Key);
            }
        }
        return (string[]) fileList.ToArray(typeof(string));
    }

    /// <summary>
    /// Gets the list of file keys whose long file names match a specified
    /// regular-expression search pattern.
    /// </summary>
    /// <param name="pattern">Regular expression search pattern</param>
    /// <returns>Array of file keys, or a 0-length array if none are found</returns>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string[] FindFiles(Regex pattern)
    {
        if (pattern== null) {
            throw new ArgumentNullException("pattern");
        }
        ArrayList fileList = new ArrayList();
        foreach (KeyValuePair<string, InstallPath> entry in this.Files)
        {
            if(pattern.IsMatch(((InstallPath) entry.Value).TargetName))
            {
                fileList.Add(entry.Key);
            }
        }
        return (string[]) fileList.ToArray(typeof(string));
    }

    /// <summary>
    /// Extracts all files to the <see cref="WorkingDirectory"/>. The files are extracted
    /// to the relative directory matching their <see cref="InstallPath.SourcePath"/>.
    /// </summary>
    /// <remarks>If any files have the uncompressed attribute, they will be copied
    /// from the <see cref="SourceDirectory"/>.</remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void ExtractFiles()
    {
        this.ExtractFiles(null);
    }
    /// <summary>
    /// Extracts a specified list of files to the <see cref="WorkingDirectory"/>. The files
    /// are extracted to the relative directory matching their <see cref="InstallPath.SourcePath"/>.
    /// </summary>
    /// <param name="fileKeys">List of file key strings to extract</param>
    /// <remarks>If any files have the uncompressed attribute, they will be copied
    /// from the <see cref="SourceDirectory"/>.</remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void ExtractFiles(ICollection<string> fileKeys)
    {
        this.ProcessFilesByMediaDisk(fileKeys,
            new ProcessFilesOnOneMediaDiskHandler(this.ExtractFilesOnOneMediaDisk));
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    private bool IsMergeModule()
    {
        return this.CountRows("Media", "`LastSequence` >= 0") == 0 &&
            this.CountRows("_Streams", "`Name` = 'MergeModule.CABinet'") != 0;
    }

    private delegate void ProcessFilesOnOneMediaDiskHandler(string mediaCab,
        InstallPathMap compressedFileMap, InstallPathMap uncompressedFileMap);

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    private void ProcessFilesByMediaDisk(ICollection<string> fileKeys,
        ProcessFilesOnOneMediaDiskHandler diskHandler)
    {
        if(this.IsMergeModule())
        {
            InstallPathMap files = new InstallPathMap();
            foreach(string fileKey in this.Files.Keys)
            {
                if(fileKeys == null || fileKeys.Contains(fileKey))
                {
                    files[fileKey] = this.Files[fileKey];
                }
            }
            diskHandler("#MergeModule.CABinet", files, new InstallPathMap());
        }
        else
        {
            bool defaultCompressed = ((this.SummaryInfo.WordCount & 0x2) != 0);

            View fileView = null, mediaView = null;
            Record fileRec = null;
            try
            {
                fileView = this.OpenView("SELECT `File`, `Attributes`, `Sequence` " +
                    "FROM `File` ORDER BY `Sequence`");
                mediaView = this.OpenView("SELECT `DiskId`, `LastSequence`, `Cabinet` " +
                    "FROM `Media` ORDER BY `DiskId`");
                fileView.Execute();
                mediaView.Execute();

                int currentMediaDiskId = -1;
                int currentMediaMaxSequence = -1;
                string currentMediaCab = null;
                InstallPathMap compressedFileMap = new InstallPathMap();
                InstallPathMap uncompressedFileMap = new InstallPathMap();

                while((fileRec = fileView.Fetch()) != null)
                {
                    string fileKey = (string) fileRec[1];

                    if(fileKeys == null || fileKeys.Contains(fileKey))
                    {
                        int fileAttributes = fileRec.GetInteger(2);
                        int fileSequence = fileRec.GetInteger(3);

                        InstallPath fileInstallPath = this.Files[fileKey];
                        if(fileInstallPath == null)
                        {
                            this.LogMessage("Could not get install path for source file: {0}", fileKey);
                            throw new InstallerException("Could not get install path for source file: " + fileKey);
                        }

                        if(fileSequence > currentMediaMaxSequence)
                        {
                            if(currentMediaDiskId != -1)
                            {
                                diskHandler(currentMediaCab,
                                    compressedFileMap, uncompressedFileMap);
                                compressedFileMap.Clear();
                                uncompressedFileMap.Clear();
                            }

                            while(fileSequence > currentMediaMaxSequence)
                            {
                                Record mediaRec = mediaView.Fetch();
                                if(mediaRec == null)
                                {
                                    currentMediaDiskId = -1;
                                    break;
                                }
                                using(mediaRec)
                                {
                                    currentMediaDiskId = mediaRec.GetInteger(1);
                                    currentMediaMaxSequence = mediaRec.GetInteger(2);
                                    currentMediaCab = (string) mediaRec[3];
                                }
                            }
                            if(fileSequence > currentMediaMaxSequence) break;
                        }

                        if((fileAttributes & (int) FileAttributes.Compressed) != 0)
                        {
                            compressedFileMap[fileKey] = fileInstallPath;
                        }
                        else if ((fileAttributes & (int) FileAttributes.NonCompressed) != 0)
                        {
                            // Non-compressed files are located
                            // in the same directory as the MSI, without any path.
                            uncompressedFileMap[fileKey] = new InstallPath(fileInstallPath.SourceName);
                        }
                        else if(defaultCompressed)
                        {
                            compressedFileMap[fileKey] = fileInstallPath;
                        }
                        else
                        {
                            uncompressedFileMap[fileKey] = fileInstallPath;
                        }
                    }
                    fileRec.Close();
                    fileRec = null;
                }
                if(currentMediaDiskId != -1)
                {
                    diskHandler(currentMediaCab,
                        compressedFileMap, uncompressedFileMap);
                }
            }
            finally
            {
                if (fileRec != null) fileRec.Close();
                if (fileView != null) fileView.Close();
                if (mediaView != null) mediaView.Close();
            }
        }
    }

    [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
    private void ExtractFilesOnOneMediaDisk(string mediaCab,
        InstallPathMap compressedFileMap, InstallPathMap uncompressedFileMap)
    {
        if(compressedFileMap.Count > 0)
        {
            string cabFile = null;
            if(mediaCab.StartsWith("#", StringComparison.Ordinal))
            {
                mediaCab = mediaCab.Substring(1);

                using(View streamView = this.OpenView("SELECT `Name`, `Data` FROM `_Streams` " +
                      "WHERE `Name` = '{0}'", mediaCab))
                {
                    streamView.Execute();
                    Record streamRec = streamView.Fetch();
                    if(streamRec == null)
                    {
                        this.LogMessage("Stream not found: {0}", mediaCab);
                        throw new InstallerException("Stream not found: " + mediaCab);
                    }
                    using(streamRec)
                    {
                        this.LogMessage("extract cab {0}", mediaCab);
                        Directory.CreateDirectory(this.TempDirectory);
                        cabFile = Path.Combine(this.TempDirectory,
                            Path.GetFileNameWithoutExtension(mediaCab) + ".cab");
                        streamRec.GetStream("Data", cabFile);
                    }
                }
            }
            else
            {
                cabFile = Path.Combine(this.SourceDirectory, mediaCab);
            }

            this.cabName = mediaCab;
            this.cabMsg = "extract {0}\\{1} {2}";
            new CabInfo(cabFile).UnpackFileSet(compressedFileMap.SourcePaths, this.WorkingDirectory,
                this.CabinetProgress);
            ClearReadOnlyAttribute(this.WorkingDirectory, compressedFileMap.Values);
        }
        foreach(InstallPath fileInstallPath in uncompressedFileMap.Values)
        {
            string sourcePath = Path.Combine(this.SourceDirectory, fileInstallPath.SourcePath);
            string extractPath = Path.Combine(this.WorkingDirectory, fileInstallPath.SourcePath);
            if(Path.GetFullPath(sourcePath).ToLowerInvariant() !=
                Path.GetFullPath(extractPath).ToLowerInvariant())
            {
                if(!File.Exists(sourcePath))
                {
                    this.LogMessage("Error: Uncompressed file not found: {0}", sourcePath);
                    throw new FileNotFoundException("Uncompressed file not found.", sourcePath);
                }
                else
                {
                    this.LogMessage("copy {0} {1}", sourcePath, extractPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(extractPath));
                    File.Copy(sourcePath, extractPath, true);
                }
            }
            else
            {
                if(!File.Exists(extractPath))
                {
                    this.LogMessage("Error: Uncompressed file not found: {0}", extractPath);
                    throw new FileNotFoundException("Uncompressed file not found.", extractPath);
                }
            }
        }
    }

    private void CabinetProgress(object sender, ArchiveProgressEventArgs e)
    {
        switch(e.ProgressType)
        {
            case ArchiveProgressType.StartFile:
            {
                string filePath = e.CurrentFileName;
                if(this.filePathMap != null)
                {
                    InstallPath fileInstallPath = this.Files[e.CurrentFileName];
                    if(fileInstallPath != null)
                    {
                        filePath = fileInstallPath.SourcePath;
                    }
                }
                this.LogMessage(this.cabMsg, this.cabName, e.CurrentFileName,
                    Path.Combine(this.WorkingDirectory, filePath));
            }
            break;
        }
    }

    /// <summary>
    /// Updates the install package with new files from the <see cref="WorkingDirectory"/>.  The
    /// files must be in the relative directory matching their <see cref="InstallPath.SourcePath"/>.
    /// This method re-compresses and packages the files if necessary, and also updates the
    /// following data: File.FileSize, File.Version, File.Language, MsiFileHash.HashPart*
    /// </summary>
    /// <remarks>
    /// The cabinet compression level used during re-cabbing can be configured with the
    /// <see cref="CompressionLevel"/> property.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void UpdateFiles()
    {
        this.UpdateFiles(null);
    }
    /// <summary>
    /// Updates the install package with new files from the <see cref="WorkingDirectory"/>.  The
    /// files must be in the relative directory matching their <see cref="InstallPath.SourcePath"/>.
    /// This method re-compresses and packages the files if necessary, and also updates the
    /// following data: File.FileSize, File.Version, File.Language, MsiFileHash.HashPart?.
    /// </summary>
    /// <param name="fileKeys">List of file key strings to update</param>
    /// <remarks>
    /// This method does not change the media structure of the package, so it may require extracting
    /// and re-compressing a large cabinet just to update one file.
    /// <p>The cabinet compression level used during re-cabbing can be configured with the
    /// <see cref="CompressionLevel"/> property.</p>
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void UpdateFiles(ICollection<string> fileKeys)
    {
        this.ProcessFilesByMediaDisk(fileKeys,
            new ProcessFilesOnOneMediaDiskHandler(this.UpdateFilesOnOneMediaDisk));
    }

    private void UpdateFilesOnOneMediaDisk(string mediaCab,
        InstallPathMap compressedFileMap, InstallPathMap uncompressedFileMap)
    {
        if(compressedFileMap.Count > 0)
        {
            string cabFile = null;
            bool cabFileIsTemp = false;
            if(mediaCab.StartsWith("#", StringComparison.Ordinal))
            {
                cabFileIsTemp = true;
                mediaCab = mediaCab.Substring(1);

                using(View streamView = this.OpenView("SELECT `Name`, `Data` FROM `_Streams` " +
                      "WHERE `Name` = '{0}'", mediaCab))
                {
                    streamView.Execute();
                    Record streamRec = streamView.Fetch();
                    if(streamRec == null)
                    {
                        this.LogMessage("Stream not found: {0}", mediaCab);
                        throw new InstallerException("Stream not found: " + mediaCab);
                    }
                    using(streamRec)
                    {
                        this.LogMessage("extract cab {0}", mediaCab);
                        Directory.CreateDirectory(this.TempDirectory);
                        cabFile = Path.Combine(this.TempDirectory,
                            Path.GetFileNameWithoutExtension(mediaCab) + ".cab");
                        streamRec.GetStream("Data", cabFile);
                    }
                }
            }
            else
            {
                cabFile = Path.Combine(this.SourceDirectory, mediaCab);
            }

            CabInfo cab = new CabInfo(cabFile);
            ArrayList fileKeyList = new ArrayList();
            foreach (CabFileInfo fileInCab in cab.GetFiles())
            {
                string fileKey = fileInCab.Name;
                if(this.Files[fileKey] != null)
                {
                    fileKeyList.Add(fileKey);
                }
            }
            string[] fileKeys = (string[]) fileKeyList.ToArray(typeof(string));

            Directory.CreateDirectory(this.TempDirectory);

            ArrayList remainingFileKeys = new ArrayList(fileKeys);
            foreach(string fileKey in fileKeys)
            {
                InstallPath fileInstallPath = compressedFileMap[fileKey];
                if(fileInstallPath != null)
                {
                    UpdateFileStats(fileKey, fileInstallPath);

                    string filePath = Path.Combine(this.WorkingDirectory, fileInstallPath.SourcePath);
                    this.LogMessage("copy {0} {1}", filePath, fileKey);
                    File.Copy(filePath, Path.Combine(this.TempDirectory, fileKey), true);
                    remainingFileKeys.Remove(fileKey);
                }
            }

            if(remainingFileKeys.Count > 0)
            {
                this.cabName = mediaCab;
                this.cabMsg = "extract {0}\\{1}";
                string[] remainingFileKeysArray = (string[]) remainingFileKeys.ToArray(typeof(string));
                cab.UnpackFiles(remainingFileKeysArray, this.TempDirectory, remainingFileKeysArray,
                    this.CabinetProgress);
            }

            ClearReadOnlyAttribute(this.TempDirectory, fileKeys);

            if(!cabFileIsTemp)
            {
                cab = new CabInfo(Path.Combine(this.WorkingDirectory, mediaCab));
            }
            this.cabName = mediaCab;
            this.cabMsg = "compress {0}\\{1}";
            cab.PackFiles(this.TempDirectory, fileKeys, fileKeys,
                this.CompressionLevel, this.CabinetProgress);

            if(cabFileIsTemp)
            {
                Record streamRec = new Record(1);
                streamRec.SetStream(1, cabFile);
                this.Execute(String.Format(CultureInfo.InvariantCulture,
                    "UPDATE `_Streams` SET `Data` = ? WHERE `Name` = '{0}'", mediaCab),
                    streamRec);
            }
        }

        foreach (KeyValuePair<string, InstallPath> entry in uncompressedFileMap)
        {
            UpdateFileStats((string) entry.Key, (InstallPath) entry.Value);
        }
    }

    private void UpdateFileStats(string fileKey, InstallPath fileInstallPath)
    {
        string filePath = Path.Combine(this.WorkingDirectory, fileInstallPath.SourcePath);
        if(!File.Exists(filePath))
        {
            this.LogMessage("Updated source file not found: {0}", filePath);
            throw new FileNotFoundException("Updated source file not found: " + filePath);
        }

        this.LogMessage("updatestats {0}", fileKey);

        string version = Installer.GetFileVersion(filePath);
        string language = Installer.GetFileLanguage(filePath);
        long size = new FileInfo(filePath).Length;

        this.Execute("UPDATE `File` SET `Version` = '{0}', `Language` = '{1}', " +
            "`FileSize` = {2} WHERE `File` = '{3}'", version, language, size, fileKey);

        if ((version == null || version.Length == 0) && this.Tables.Contains("MsiFileHash"))
        {
            int[] hash = new int[4];
            Installer.GetFileHash(filePath, hash);
            this.Execute("DELETE FROM `MsiFileHash` WHERE `File_` = '{0}'", fileKey);
            this.Execute("INSERT INTO `MsiFileHash` (`File_`, `Options`, `HashPart1`, `HashPart2`, " +
                "`HashPart3`, `HashPart4`) VALUES ('" + fileKey + "', 0, {0}, {1}, {2}, {3})",
                hash[0], hash[1], hash[2], hash[3]);
        }
    }

    /// <summary>
    /// Consolidates a package by combining and re-compressing all files into a single
    /// internal or external cabinet.
    /// </summary>
    /// <param name="mediaCabinet"></param>
    /// <remarks>If an installation package was built from many merge modules, this
    /// method can somewhat decrease package size, complexity, and installation time.
    /// <p>This method will also convert a package with all or mostly uncompressed
    /// files into a package where all files are compressed.</p>
    /// <p>If the package contains any not-yet-applied binary file patches (for
    /// example, a package generated by a call to <see cref="ApplyPatch"/>) then
    /// this method will apply the patches before compressing the updated files.</p>
    /// <p>This method edits the database summary information and the File, Media
    /// and Patch tables as necessary to maintain a valid installation package.</p>
    /// <p>The cabinet compression level used during re-cabbing can be configured with the
    /// <see cref="CompressionLevel"/> property.</p>
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void Consolidate(string mediaCabinet)
    {
        this.LogMessage("Consolidating package");

        Directory.CreateDirectory(this.TempDirectory);

        this.LogMessage("Extracting/preparing files");
        this.ProcessFilesByMediaDisk(null,
            new ProcessFilesOnOneMediaDiskHandler(this.PrepareOneMediaDiskForConsolidation));

        this.LogMessage("Applying any file patches");
        ApplyFilePatchesForConsolidation();

        this.LogMessage("Clearing PatchPackage, Patch, MsiPatchHeaders tables");
        if (this.Tables.Contains("PatchPackage"))
        {
            this.Execute("DELETE FROM `PatchPackage` WHERE `PatchId` <> ''");
        }
        if (this.Tables.Contains("Patch"))
        {
            this.Execute("DELETE FROM `Patch` WHERE `File_` <> ''");
        }
        if (this.Tables.Contains("MsiPatchHeaders"))
        {
            this.Execute("DELETE FROM `MsiPatchHeaders` WHERE `StreamRef` <> ''");
        }

        this.LogMessage("Resequencing files");
        ArrayList files = new ArrayList();
        using(View fileView = this.OpenView("SELECT `File`, `Attributes`, `Sequence` " +
              "FROM `File` ORDER BY `Sequence`"))
        {
            fileView.Execute();

            foreach (Record fileRec in fileView) using(fileRec)
            {
                files.Add(fileRec[1]);
                int fileAttributes = fileRec.GetInteger(2);
                fileAttributes &= ~(int) (FileAttributes.Compressed
                    | FileAttributes.NonCompressed | FileAttributes.PatchAdded);
                fileRec[2] = fileAttributes;
                fileRec[3] = files.Count;
                fileView.Update(fileRec);
            }
        }

        if (mediaCabinet== null) {
            throw new ArgumentNullException("mediaCabinet");
        }

        bool internalCab = false;
        if(mediaCabinet.StartsWith("#", StringComparison.Ordinal))
        {
            internalCab = true;
            mediaCabinet = mediaCabinet.Substring(1);
        }

        this.LogMessage("Cabbing files");
        string[] fileKeys = (string[]) files.ToArray(typeof(string));
        string cabPath = Path.Combine(internalCab ? this.TempDirectory
            : this.WorkingDirectory, mediaCabinet);
        this.cabName = mediaCabinet;
        this.cabMsg = "compress {0}\\{1}";
        new CabInfo(cabPath).PackFiles(this.TempDirectory, fileKeys,
            fileKeys, this.CompressionLevel, this.CabinetProgress);

        this.DeleteEmbeddedCabs();

        if(internalCab)
        {
            this.LogMessage("Inserting cab stream into MSI");
            Record cabRec = new Record(1);
            cabRec.SetStream(1, cabPath);
            this.Execute("INSERT INTO `_Streams` (`Name`, `Data`) VALUES ('" + mediaCabinet + "', ?)", cabRec);
        }

        this.LogMessage("Inserting cab media record into MSI");
        this.Execute("DELETE FROM `Media` WHERE `DiskId` <> 0");
        this.Execute("INSERT INTO `Media` (`DiskId`, `LastSequence`, `Cabinet`) " +
            "VALUES (1, " + files.Count + ", '" + (internalCab ? "#" : "") + mediaCabinet + "')");


        this.LogMessage("Setting compressed flag on package summary info");
        this.SummaryInfo.WordCount = this.SummaryInfo.WordCount | 2;
        this.SummaryInfo.Persist();
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    private void DeleteEmbeddedCabs()
    {
        using (View view = this.OpenView("SELECT `Cabinet` FROM `Media` WHERE `Cabinet` <> ''"))
        {
            view.Execute();

            foreach (Record rec in view) using(rec)
            {
                string cab = rec.GetString(1);
                if(cab.StartsWith("#", StringComparison.Ordinal))
                {
                    cab = cab.Substring(1);
                    this.LogMessage("Deleting embedded cab stream: {0}", cab);
                    this.Execute("DELETE FROM `_Streams` WHERE `Name` = '{0}'", cab);
                }
            }
        }
    }

    private void PrepareOneMediaDiskForConsolidation(string mediaCab,
        InstallPathMap compressedFileMap, InstallPathMap uncompressedFileMap)
    {
        if(compressedFileMap.Count > 0)
        {
            string cabFile = null;
            if(mediaCab.StartsWith("#", StringComparison.Ordinal))
            {
                mediaCab = mediaCab.Substring(1);

                using (View streamView = this.OpenView("SELECT `Name`, `Data` FROM `_Streams` " +
                      "WHERE `Name` = '{0}'", mediaCab))
                {
                    streamView.Execute();
                    Record streamRec = streamView.Fetch();
                    if(streamRec == null)
                    {
                        this.LogMessage("Stream not found: {0}", mediaCab);
                        throw new InstallerException("Stream not found: " + mediaCab);
                    }
                    using(streamRec)
                    {
                        this.LogMessage("extract cab {0}", mediaCab);
                        cabFile = Path.Combine(this.TempDirectory,
                            Path.GetFileNameWithoutExtension(mediaCab) + ".cab");
                        streamRec.GetStream("Data", cabFile);
                    }
                }
            }
            else
            {
                cabFile = Path.Combine(this.SourceDirectory, mediaCab);
            }
            string[] fileKeys = new string[compressedFileMap.Keys.Count];
            compressedFileMap.Keys.CopyTo(fileKeys, 0);
            this.cabName = mediaCab;
            this.cabMsg = "extract {0}\\{1}";
            new CabInfo(cabFile).UnpackFiles(fileKeys, this.TempDirectory, fileKeys,
                this.CabinetProgress);
            ClearReadOnlyAttribute(this.TempDirectory, fileKeys);
        }
        foreach (KeyValuePair<string, InstallPath> entry in uncompressedFileMap)
        {
            string fileKey = (string) entry.Key;
            InstallPath fileInstallPath = (InstallPath) entry.Value;

            string filePath = Path.Combine(this.SourceDirectory, fileInstallPath.SourcePath);
            this.LogMessage("copy {0} {1}", filePath, fileKey);
            File.Copy(filePath, Path.Combine(this.TempDirectory, fileKey));
        }
    }

    private void ClearReadOnlyAttribute(string baseDirectory, IEnumerable filePaths)
    {
        foreach(object filePath in filePaths)
        {
            string fullFilePath = Path.Combine(baseDirectory, filePath.ToString());
            if (File.Exists(fullFilePath))
            {
                System.IO.FileAttributes fileAttributes = File.GetAttributes(fullFilePath);
                if ((fileAttributes & System.IO.FileAttributes.ReadOnly) != 0)
                {
                    fileAttributes &= ~System.IO.FileAttributes.ReadOnly;
                    File.SetAttributes(fullFilePath, fileAttributes);
                }
            }
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    private void ApplyFilePatchesForConsolidation()
    {
        if(this.Tables.Contains("Patch"))
        {
            using(View patchView = this.OpenView("SELECT `File_`, `Sequence` " +
                  "FROM `Patch` ORDER BY `Sequence`"))
            {
                patchView.Execute();
                Hashtable extractedPatchCabs = new Hashtable();

                foreach (Record patchRec in patchView) using(patchRec)
                {
                    string fileKey = (string) patchRec[1];
                    int sequence = patchRec.GetInteger(2);
                    this.LogMessage("patch {0}", fileKey);

                    string tempPatchFile = Path.Combine(this.TempDirectory, fileKey + ".pat");
                    ExtractFilePatch(fileKey, sequence, tempPatchFile, extractedPatchCabs);
                    string filePath = Path.Combine(this.TempDirectory, fileKey);
                    string oldFilePath = filePath + ".old";
                    if(File.Exists(oldFilePath)) File.Delete(oldFilePath);
                    File.Move(filePath, oldFilePath);
                    Type.GetType("Microsoft.Deployment.WindowsInstaller.FilePatch")
                        .GetMethod("ApplyPatchToFile",
                        new Type[] { typeof(string), typeof(string), typeof(string) })
                        .Invoke(null, new object[] { tempPatchFile, oldFilePath, filePath });
                }
            }
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    private void ExtractFilePatch(string fileKey, int sequence, string extractPath,
        IDictionary extractedCabs)
    {
        string mediaCab = null;
        using(View mediaView = this.OpenView("SELECT `DiskId`, `LastSequence`, `Cabinet` " +
              "FROM `Media` ORDER BY `DiskId`"))
        {
            mediaView.Execute();

            foreach (Record mediaRec in mediaView) using(mediaRec)
            {
                int mediaMaxSequence = mediaRec.GetInteger(2);
                if(mediaMaxSequence >= sequence)
                {
                    mediaCab = mediaRec.GetString(3);
                    break;
                }
            }
        }

        if(mediaCab == null || mediaCab.Length == 0)
        {
            this.LogMessage("Could not find cabinet for file patch: {0}", fileKey);
            throw new InstallerException("Could not find cabinet for file patch: " + fileKey);
        }

        if(!mediaCab.StartsWith("#", StringComparison.Ordinal))
        {
            this.LogMessage("Error: Patch cabinet {0} must be embedded", mediaCab);
            throw new InstallerException("Patch cabinet " + mediaCab + " must be embedded.");
        }
        mediaCab = mediaCab.Substring(1);

        string cabFile = (string) extractedCabs[mediaCab];
        if(cabFile == null)
        {
            using(View streamView = this.OpenView("SELECT `Name`, `Data` FROM `_Streams` " +
                  "WHERE `Name` = '{0}'", mediaCab))
            {
                streamView.Execute();
                Record streamRec = streamView.Fetch();
                if(streamRec == null)
                {
                    this.LogMessage("Stream not found: {0}", mediaCab);
                    throw new InstallerException("Stream not found: " + mediaCab);
                }
                using(streamRec)
                {
                    this.LogMessage("extract cab {0}", mediaCab);
                    Directory.CreateDirectory(this.TempDirectory);
                    cabFile = Path.Combine(this.TempDirectory,
                        Path.GetFileNameWithoutExtension(mediaCab) + ".cab");
                    streamRec.GetStream("Data", cabFile);
                }
            }
            extractedCabs[mediaCab] = cabFile;
        }

        this.LogMessage("extract patch {0}\\{1}", mediaCab, fileKey);
        new CabInfo(cabFile).UnpackFile(fileKey, extractPath);
    }

    /// <summary>
    /// Rebuilds the cached directory structure information accessed by the
    /// <see cref="Directories"/> and <see cref="Files"/> properties. This
    /// should be done after modifying the File, Component, or Directory
    /// tables, or else the cached information may no longer be accurate.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void UpdateDirectories()
    {
        this.dirPathMap = null;
        this.filePathMap = InstallPathMap.BuildFilePathMap(this,
            InstallPathMap.BuildComponentPathMap(this, this.Directories), false);
    }

    /// <summary>
    /// Gets a mapping from Directory keys to source/target paths.
    /// </summary>
    /// <remarks>
    /// If the Directory table is modified, this mapping
    /// will be outdated until you call <see cref="UpdateDirectories"/>.
    /// </remarks>
    public InstallPathMap Directories
    {
        get
        {
            if(this.dirPathMap == null)
            {
                this.dirPathMap = InstallPathMap.BuildDirectoryPathMap(this, false);
            }
            return this.dirPathMap;
        }
    }
    private InstallPathMap dirPathMap;

    /// <summary>
    /// Gets a mapping from File keys to source/target paths.
    /// </summary>
    /// <remarks>
    /// If the File, Component, or Directory tables are modified, this mapping
    /// may be outdated until you call <see cref="UpdateDirectories"/>.
    /// </remarks>
    public InstallPathMap Files
    {
        get
        {
            if(this.filePathMap == null)
            {
                this.filePathMap = InstallPathMap.BuildFilePathMap(this,
                    InstallPathMap.BuildComponentPathMap(this, this.Directories), false);
            }
            return this.filePathMap;
        }
    }
    private InstallPathMap filePathMap;

    /// <summary>
    /// Gets or sets the compression level used by <see cref="UpdateFiles()"/>
    /// and <see cref="Consolidate"/>.
    /// </summary>
    /// <remarks>
    /// If the Directory table is modified, this mapping will be outdated
    /// until you close and reopen the install package.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public CompressionLevel CompressionLevel
    {
        get { return this.compressionLevel; }
        set { this.compressionLevel = value; }
    }
    private CompressionLevel compressionLevel;

    /// <summary>
    /// Applies a patch package to the database, resulting in an installation package that
    /// has the patch built-in.
    /// </summary>
    /// <param name="patchPackage">The patch package to be applied</param>
    /// <param name="transform">Optional name of the specific transform to apply.
    /// This parameter is usually left null, which causes the patch to be searched for
    /// a transform that is valid to apply to this database.</param>
    /// <remarks>
    /// If the patch contains any binary file patches, they will not immediately be applied
    /// to the target files, though they will at installation time.
    /// <p>After calling this method you can use <see cref="Consolidate"/> to apply
    /// the file patches immediately and also discard any outdated files from the package.</p>
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public void ApplyPatch(PatchPackage patchPackage, string transform)
    {
        if(patchPackage == null) throw new ArgumentNullException("patchPackage");

        this.LogMessage("Applying patch file {0} to database {1}",
            patchPackage.FilePath, this.FilePath);

        if(transform == null)
        {
            this.LogMessage("No transform specified; searching for valid patch transform");
            string[] validTransforms = patchPackage.GetValidTransforms(this);
            if(validTransforms.Length == 0)
            {
                this.LogMessage("No valid patch transform was found");
                throw new InvalidOperationException("No valid patch transform was found.");
            }
            transform = validTransforms[0];
        }
        this.LogMessage("Patch transform = {0}", transform);

        string patchPrefix = Path.GetFileNameWithoutExtension(patchPackage.FilePath) + "_";

        string specialTransform = "#" + transform;
        Directory.CreateDirectory(this.TempDirectory);
        this.LogMessage("Extracting substorage {0}", transform);
        string transformFile = Path.Combine(this.TempDirectory,
            patchPrefix + Path.GetFileNameWithoutExtension(transform) + ".mst");
        patchPackage.ExtractTransform(transform, transformFile);
        this.LogMessage("Extracting substorage {0}", specialTransform);
        string specialTransformFile = Path.Combine(this.TempDirectory,
            patchPrefix + Path.GetFileNameWithoutExtension(specialTransform) + ".mst");
        patchPackage.ExtractTransform(specialTransform, specialTransformFile);

        if (this.Tables.Contains("Patch") && !this.Tables["Patch"].Columns.Contains("_StreamRef"))
        {
            if(this.CountRows("Patch") > 0)
            {
                this.LogMessage("Warning: non-empty Patch table exists without StreamRef_ column; " +
                    "patch transform may fail");
            }
            else
            {
                this.Execute("DROP TABLE `Patch`");
                this.Execute("CREATE TABLE `Patch` (`File_` CHAR(72) NOT NULL, " +
                    "`Sequence` INTEGER NOT NULL, `PatchSize` LONG NOT NULL, " +
                    "`Attributes` INTEGER NOT NULL, `Header` OBJECT, `StreamRef_` CHAR(72)  " +
                    "PRIMARY KEY `File_`, `Sequence`)");
            }
        }

        this.LogMessage("Applying transform {0} to database", transform);
        this.ApplyTransform(transformFile);
        this.LogMessage("Applying transform {0} to database", specialTransform);
        this.ApplyTransform(specialTransformFile);

        if (this.Tables.Contains("MsiPatchHeaders") && this.CountRows("MsiPatchHeaders") > 0 &&
            (!this.Tables.Contains("Patch") || this.CountRows("Patch", "`StreamRef_` <> ''") == 0))
        {
            this.LogMessage("Error: patch transform failed because of missing Patch.StreamRef_ column");
            throw new InstallerException("Patch transform failed because of missing Patch.StreamRef_ column");
        }

        IList<int> mediaIds = this.ExecuteIntegerQuery("SELECT `Media_` FROM `PatchPackage` " +
            "WHERE `PatchId` = '{0}'", patchPackage.PatchCode);
        if (mediaIds.Count == 0)
        {
            this.LogMessage("Warning: PatchPackage Media record not found -- " +
                "skipping inclusion of patch cabinet");
        }
        else
        {
            int patchMediaDiskId = mediaIds[0];
            IList<string> patchCabinets = this.ExecuteStringQuery("SELECT `Cabinet` FROM `Media` " +
                "WHERE `DiskId` = {0}", patchMediaDiskId);
            if(patchCabinets.Count == 0)
            {
                this.LogMessage("Patch cabinet record not found");
                throw new InstallerException("Patch cabinet record not found.");
            }
            string patchCabinet = patchCabinets[0];
            this.LogMessage("Patch cabinet = {0}", patchCabinet);
            if(!patchCabinet.StartsWith("#", StringComparison.Ordinal))
            {
                this.LogMessage("Error: Patch cabinet must be embedded");
                throw new InstallerException("Patch cabinet must be embedded.");
            }
            patchCabinet = patchCabinet.Substring(1);

            string renamePatchCabinet = patchPrefix + patchCabinet;

            const int HIGH_DISKID = 30000; // Must not collide with other patch media DiskIDs
            int renamePatchMediaDiskId = HIGH_DISKID;
            while (this.CountRows("Media", "`DiskId` = " + renamePatchMediaDiskId) > 0) renamePatchMediaDiskId++;

            // Since the patch cab is now embedded in the MSI, it shouldn't have a separate disk prompt/source
            this.LogMessage("Renaming the patch media record");
            int lastSeq = Convert.ToInt32(this.ExecuteScalar("SELECT `LastSequence` FROM `Media` WHERE `DiskId` = {0}", patchMediaDiskId),CultureInfo.InvariantCulture);
            this.Execute("DELETE FROM `Media` WHERE `DiskId` = {0}", patchMediaDiskId);
            this.Execute("INSERT INTO `Media` (`DiskId`, `LastSequence`, `Cabinet`) VALUES ({0}, '{1}', '#{2}')",
                renamePatchMediaDiskId, lastSeq, renamePatchCabinet);
            this.Execute("UPDATE `PatchPackage` SET `Media_` = {0} WHERE `PatchId` = '{1}'", renamePatchMediaDiskId, patchPackage.PatchCode);

            this.LogMessage("Copying patch cabinet: {0}", patchCabinet);
            string patchCabFile = Path.Combine(this.TempDirectory,
                Path.GetFileNameWithoutExtension(patchCabinet) + ".cab");
            using(View streamView = patchPackage.OpenView("SELECT `Name`, `Data` FROM `_Streams` " +
                  "WHERE `Name` = '{0}'", patchCabinet))
            {
                streamView.Execute();
                Record streamRec = streamView.Fetch();
                if(streamRec == null)
                {
                    this.LogMessage("Error: Patch cabinet not found");
                    throw new InstallerException("Patch cabinet not found.");
                }
                using(streamRec)
                {
                    streamRec.GetStream(2, patchCabFile);
                }
            }
            using(Record patchCabRec = new Record(2))
            {
                patchCabRec[1] = patchCabinet;
                patchCabRec.SetStream(2, patchCabFile);
                this.Execute("INSERT INTO `_Streams` (`Name`, `Data`) VALUES (?, ?)", patchCabRec);
            }

            this.LogMessage("Ensuring PatchFiles action exists in InstallExecuteSequence table");
            if (this.Tables.Contains("InstallExecuteSequence"))
            {
                if(this.CountRows("InstallExecuteSequence", "`Action` = 'PatchFiles'") == 0)
                {
                    IList<int> installFilesSeqList = this.ExecuteIntegerQuery("SELECT `Sequence` " +
                        "FROM `InstallExecuteSequence` WHERE `Action` = 'InstallFiles'");
                    short installFilesSeq = (short) (installFilesSeqList.Count != 0 ?
                        installFilesSeqList[0] : 0);
                    this.Execute("INSERT INTO `InstallExecuteSequence` (`Action`, `Sequence`) " +
                        "VALUES ('PatchFiles', {0})", installFilesSeq + 1);
                }
            }

            // Patch-added files need to be marked always-compressed
            this.LogMessage("Adjusting attributes of patch-added files");
            using(View fileView = this.OpenView("SELECT `File`, `Attributes`, `Sequence` " +
                  "FROM `File` ORDER BY `Sequence`"))
            {
                fileView.Execute();

                foreach (Record fileRec in fileView) using(fileRec)
                {
                    int fileAttributes = fileRec.GetInteger(2);
                    if ((fileAttributes & (int) FileAttributes.PatchAdded) != 0)
                    {
                        fileAttributes = (fileAttributes | (int) FileAttributes.Compressed)
                            & ~(int) FileAttributes.NonCompressed
                            & ~(int) FileAttributes.PatchAdded;
                        fileRec[2] = fileAttributes;
                        fileView.Update(fileRec);
                    }
                }
            }
        }

        this.LogMessage("Applying new summary info from patch package");
        this.SummaryInfo.RevisionNumber = this.Property["PATCHNEWPACKAGECODE"];
        this.SummaryInfo.Subject = this.Property["PATCHNEWSUMMARYSUBJECT"];
        this.SummaryInfo.Comments = this.Property["PATCHNEWSUMMARYCOMMENTS"];
        this.SummaryInfo.Persist();
        this.Property["PATCHNEWPACKAGECODE"    ] = null;
        this.Property["PATCHNEWSUMMARYSUBJECT" ] = null;
        this.Property["PATCHNEWSUMMARYCOMMENTS"] = null;

        this.LogMessage("Patch application finished");
    }

    /// <summary>
    /// Accessor for getting and setting properties of the InstallPackage database.
    /// </summary>
    public InstallPackageProperties Property
    {
        get
        {
            if(this.properties == null)
            {
                this.properties = new InstallPackageProperties(this);
            }
            return this.properties;
        }
    }
    private InstallPackageProperties properties = null;
}

/// <summary>
/// Accessor for getting and setting properties of the <see cref="InstallPackage"/> database.
/// </summary>
internal class InstallPackageProperties
{
    internal InstallPackageProperties(InstallPackage installPackage)
    {
        this.installPackage = installPackage;
    }
    private InstallPackage installPackage;

    /// <summary>
    /// Gets or sets a property in the database. When getting a property
    /// that does not exist in the database, an empty string is returned.
    /// To remove a property from the database, set it to an empty string.
    /// </summary>
    /// <remarks>
    /// This has the same results as direct SQL queries on the Property table; it's only
    /// meant to be a more convenient way of access.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public string this[string name]
    {
        get
        {
            IList<string> values = installPackage.ExecuteStringQuery(
                "SELECT `Value` FROM `Property` WHERE `Property` = '{0}'", name);
            return (values.Count != 0 ? values[0] : "");
        }
        set
        {
            Record propRec = new Record(name, (value != null ? value : ""));
            installPackage.Execute("DELETE FROM `Property` WHERE `Property` = ?", propRec);
            if(value != null && value.Length != 0)
            {
                installPackage.Execute("INSERT INTO `Property` (`Property`, `Value`) VALUES (?, ?)",
                    propRec);
            }
        }
    }
}

}
