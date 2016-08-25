//---------------------------------------------------------------------
// <copyright file="ArchiveInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Abstract object representing a compressed archive on disk;
    /// provides access to file-based operations on the archive.
    /// </summary>
    public abstract class ArchiveInfo : FileSystemInfo
    {
        /// <summary>
        /// Creates a new ArchiveInfo object representing an archive in a
        /// specified path.
        /// </summary>
        /// <param name="path">The path to the archive. When creating an archive,
        /// this file does not necessarily exist yet.</param>
        protected ArchiveInfo(string path)
            : base()
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            // protected instance members inherited from FileSystemInfo:
            this.OriginalPath = path;
            this.FullPath = Path.GetFullPath(path);
        }

        /// <summary>
        /// Gets the directory that contains the archive.
        /// </summary>
        /// <value>A DirectoryInfo object representing the parent directory of the
        /// archive.</value>
        public DirectoryInfo Directory
        {
            get
            {
                return new DirectoryInfo(Path.GetDirectoryName(this.FullName));
            }
        }

        /// <summary>
        /// Gets the full path of the directory that contains the archive.
        /// </summary>
        /// <value>The full path of the directory that contains the archive.</value>
        public string DirectoryName
        {
            get
            {
                return Path.GetDirectoryName(this.FullName);
            }
        }

        /// <summary>
        /// Gets the size of the archive.
        /// </summary>
        /// <value>The size of the archive in bytes.</value>
        public long Length
        {
            get
            {
                return new FileInfo(this.FullName).Length;
            }
        }

        /// <summary>
        /// Gets the file name of the archive.
        /// </summary>
        /// <value>The file name of the archive, not including any path.</value>
        public override string Name
        {
            get
            {
                return Path.GetFileName(this.FullName);
            }
        }

        /// <summary>
        /// Checks if the archive exists.
        /// </summary>
        /// <value>True if the archive exists; else false.</value>
        public override bool Exists
        {
            get
            {
                return File.Exists(this.FullName);
            }
        }

        /// <summary>
        /// Gets the full path of the archive.
        /// </summary>
        /// <returns>The full path of the archive.</returns>
        public override string ToString()
        {
            return this.FullName;
        }

        /// <summary>
        /// Deletes the archive.
        /// </summary>
        public override void Delete()
        {
            File.Delete(this.FullName);
        }

        /// <summary>
        /// Copies an existing archive to another location.
        /// </summary>
        /// <param name="destFileName">The destination file path.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void CopyTo(string destFileName)
        {
            File.Copy(this.FullName, destFileName);
        }

        /// <summary>
        /// Copies an existing archive to another location, optionally
        /// overwriting the destination file.
        /// </summary>
        /// <param name="destFileName">The destination file path.</param>
        /// <param name="overwrite">If true, the destination file will be
        /// overwritten if it exists.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void CopyTo(string destFileName, bool overwrite)
        {
            File.Copy(this.FullName, destFileName, overwrite);
        }

        /// <summary>
        /// Moves an existing archive to another location.
        /// </summary>
        /// <param name="destFileName">The destination file path.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void MoveTo(string destFileName)
        {
            File.Move(this.FullName, destFileName);
            this.FullPath = Path.GetFullPath(destFileName);
        }

        /// <summary>
        /// Checks if the archive contains a valid archive header.
        /// </summary>
        /// <returns>True if the file is a valid archive; false otherwise.</returns>
        public bool IsValid()
        {
            using (Stream stream = File.OpenRead(this.FullName))
            {
                using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
                {
                    return compressionEngine.FindArchiveOffset(stream) >= 0;
                }
            }
        }

        /// <summary>
        /// Gets information about the files contained in the archive.
        /// </summary>
        /// <returns>A list of <see cref="ArchiveFileInfo"/> objects, each
        /// containing information about a file in the archive.</returns>
        public IList<ArchiveFileInfo> GetFiles()
        {
            return this.InternalGetFiles((Predicate<string>)null);
        }

        /// <summary>
        /// Gets information about the certain files contained in the archive file.
        /// </summary>
        /// <param name="searchPattern">The search string, such as
        /// &quot;*.txt&quot;.</param>
        /// <returns>A list of <see cref="ArchiveFileInfo"/> objects, each containing
        /// information about a file in the archive.</returns>
        public IList<ArchiveFileInfo> GetFiles(string searchPattern)
        {
            if (searchPattern == null)
            {
                throw new ArgumentNullException("searchPattern");
            }

            string regexPattern = String.Format(
                CultureInfo.InvariantCulture,
                "^{0}$",
                Regex.Escape(searchPattern).Replace("\\*", ".*").Replace("\\?", "."));
            Regex regex = new Regex(
                    regexPattern,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            return this.InternalGetFiles(
                delegate(string match)
                {
                    return regex.IsMatch(match);
                });
        }

        /// <summary>
        /// Extracts all files from an archive to a destination directory.
        /// </summary>
        /// <param name="destDirectory">Directory where the files are to be
        /// extracted.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void Unpack(string destDirectory)
        {
            this.Unpack(destDirectory, null);
        }

        /// <summary>
        /// Extracts all files from an archive to a destination directory,
        /// optionally extracting only newer files.
        /// </summary>
        /// <param name="destDirectory">Directory where the files are to be
        /// extracted.</param>
        /// <param name="progressHandler">Handler for receiving progress
        /// information; this may be null if progress is not desired.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void Unpack(
            string destDirectory,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
            {
                compressionEngine.Progress += progressHandler;
                ArchiveFileStreamContext streamContext =
                    new ArchiveFileStreamContext(this.FullName, destDirectory, null);
                streamContext.EnableOffsetOpen = true;
                compressionEngine.Unpack(streamContext, null);
            }
        }

        /// <summary>
        /// Extracts a single file from the archive.
        /// </summary>
        /// <param name="fileName">The name of the file in the archive. Also
        /// includes the internal path of the file, if any. File name matching
        /// is case-insensitive.</param>
        /// <param name="destFileName">The path where the file is to be
        /// extracted on disk.</param>
        /// <remarks>If <paramref name="destFileName"/> already exists,
        /// it will be overwritten.</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void UnpackFile(string fileName, string destFileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (destFileName == null)
            {
                throw new ArgumentNullException("destFileName");
            }

            this.UnpackFiles(
                new string[] { fileName },
                null,
                new string[] { destFileName });
        }

        /// <summary>
        /// Extracts multiple files from the archive.
        /// </summary>
        /// <param name="fileNames">The names of the files in the archive.
        /// Each name includes the internal path of the file, if any. File name
        /// matching is case-insensitive.</param>
        /// <param name="destDirectory">This parameter may be null, but if
        /// specified it is the root directory for any relative paths in
        /// <paramref name="destFileNames"/>.</param>
        /// <param name="destFileNames">The paths where the files are to be
        /// extracted on disk. If this parameter is null, the files will be
        /// extracted with the names from the archive.</param>
        /// <remarks>
        /// If any extracted files already exist on disk, they will be overwritten.
        /// <p>The <paramref name="destDirectory"/> and
        /// <paramref name="destFileNames"/> parameters cannot both be null.</p>
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void UnpackFiles(
            IList<string> fileNames,
            string destDirectory,
            IList<string> destFileNames)
        {
            this.UnpackFiles(fileNames, destDirectory, destFileNames, null);
        }

        /// <summary>
        /// Extracts multiple files from the archive, optionally extracting
        /// only newer files.
        /// </summary>
        /// <param name="fileNames">The names of the files in the archive.
        /// Each name includes the internal path of the file, if any. File name
        /// matching is case-insensitive.</param>
        /// <param name="destDirectory">This parameter may be null, but if
        /// specified it is the root directory for any relative paths in
        /// <paramref name="destFileNames"/>.</param>
        /// <param name="destFileNames">The paths where the files are to be
        /// extracted on disk. If this parameter is null, the files will be
        /// extracted with the names from the archive.</param>
        /// <param name="progressHandler">Handler for receiving progress information;
        /// this may be null if progress is not desired.</param>
        /// <remarks>
        /// If any extracted files already exist on disk, they will be overwritten.
        /// <p>The <paramref name="destDirectory"/> and
        /// <paramref name="destFileNames"/> parameters cannot both be null.</p>
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void UnpackFiles(
            IList<string> fileNames,
            string destDirectory,
            IList<string> destFileNames,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }

            if (destFileNames == null)
            {
                if (destDirectory == null)
                {
                    throw new ArgumentNullException("destFileNames");
                }

                destFileNames = fileNames;
            }

            if (destFileNames.Count != fileNames.Count)
            {
                throw new ArgumentOutOfRangeException("destFileNames");
            }

            IDictionary<string, string> files =
                ArchiveInfo.CreateStringDictionary(fileNames, destFileNames);
            this.UnpackFileSet(files, destDirectory, progressHandler);
        }

        /// <summary>
        /// Extracts multiple files from the archive.
        /// </summary>
        /// <param name="fileNames">A mapping from internal file paths to
        /// external file paths. Case-sensitivity when matching internal paths
        /// depends on the IDictionary implementation.</param>
        /// <param name="destDirectory">This parameter may be null, but if
        /// specified it is the root directory for any relative external paths
        /// in <paramref name="fileNames"/>.</param>
        /// <remarks>
        /// If any extracted files already exist on disk, they will be overwritten.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void UnpackFileSet(
            IDictionary<string, string> fileNames,
            string destDirectory)
        {
            this.UnpackFileSet(fileNames, destDirectory, null);
        }

        /// <summary>
        /// Extracts multiple files from the archive.
        /// </summary>
        /// <param name="fileNames">A mapping from internal file paths to
        /// external file paths. Case-sensitivity when matching internal
        /// paths depends on the IDictionary implementation.</param>
        /// <param name="destDirectory">This parameter may be null, but if
        /// specified it is the root directory for any relative external
        /// paths in <paramref name="fileNames"/>.</param>
        /// <param name="progressHandler">Handler for receiving progress
        /// information; this may be null if progress is not desired.</param>
        /// <remarks>
        /// If any extracted files already exist on disk, they will be overwritten.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dest")]
        public void UnpackFileSet(
            IDictionary<string, string> fileNames,
            string destDirectory,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }

            using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
            {
                compressionEngine.Progress += progressHandler;
                ArchiveFileStreamContext streamContext =
                    new ArchiveFileStreamContext(this.FullName, destDirectory, fileNames);
                streamContext.EnableOffsetOpen = true;
                compressionEngine.Unpack(
                    streamContext,
                    delegate(string match)
                    {
                        return fileNames.ContainsKey(match);
                    });
            }
        }

        /// <summary>
        /// Opens a file inside the archive for reading without actually
        /// extracting the file to disk.
        /// </summary>
        /// <param name="fileName">The name of the file in the archive. Also
        /// includes the internal path of the file, if any. File name matching
        /// is case-insensitive.</param>
        /// <returns>
        /// A stream for reading directly from the packed file. Like any stream
        /// this should be closed/disposed as soon as it is no longer needed.
        /// </returns>
        public Stream OpenRead(string fileName)
        {
            Stream archiveStream = File.OpenRead(this.FullName);
            CompressionEngine compressionEngine = this.CreateCompressionEngine();
            Stream fileStream = compressionEngine.Unpack(archiveStream, fileName);

            // Attach the archiveStream and compressionEngine to the
            // fileStream so they get disposed when the fileStream is disposed.
            return new CargoStream(fileStream, archiveStream, compressionEngine);
        }

        /// <summary>
        /// Opens a file inside the archive for reading text with UTF-8 encoding
        /// without actually extracting the file to disk.
        /// </summary>
        /// <param name="fileName">The name of the file in the archive. Also
        /// includes the internal path of the file, if any. File name matching
        /// is case-insensitive.</param>
        /// <returns>
        /// A reader for reading text directly from the packed file. Like any reader
        /// this should be closed/disposed as soon as it is no longer needed.
        /// </returns>
        /// <remarks>
        /// To open an archived text file with different encoding, use the
        /// <see cref="OpenRead" /> method and pass the returned stream to one of
        /// the <see cref="StreamReader" /> constructor overloads.
        /// </remarks>
        public StreamReader OpenText(string fileName)
        {
            return new StreamReader(this.OpenRead(fileName));
        }

        /// <summary>
        /// Compresses all files in a directory into the archive.
        /// Does not include subdirectories.
        /// </summary>
        /// <param name="sourceDirectory">The directory containing the
        /// files to be included.</param>
        /// <remarks>
        /// Uses maximum compression level.
        /// </remarks>
        public void Pack(string sourceDirectory)
        {
            this.Pack(sourceDirectory, false, CompressionLevel.Max, null);
        }

        /// <summary>
        /// Compresses all files in a directory into the archive, optionally
        /// including subdirectories.
        /// </summary>
        /// <param name="sourceDirectory">This is the root directory
        /// for to pack all files.</param>
        /// <param name="includeSubdirectories">If true, recursively include
        /// files in subdirectories.</param>
        /// <param name="compLevel">The compression level used when creating
        /// the archive.</param>
        /// <param name="progressHandler">Handler for receiving progress information;
        /// this may be null if progress is not desired.</param>
        /// <remarks>
        /// The files are stored in the archive using their relative file paths in
        /// the directory tree, if supported by the archive file format.
        /// </remarks>
        public void Pack(
            string sourceDirectory,
            bool includeSubdirectories,
            CompressionLevel compLevel,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            IList<string> files = this.GetRelativeFilePathsInDirectoryTree(
                sourceDirectory, includeSubdirectories);
            this.PackFiles(sourceDirectory, files, files, compLevel, progressHandler);
        }

        /// <summary>
        /// Compresses files into the archive, specifying the names used to
        /// store the files in the archive.
        /// </summary>
        /// <param name="sourceDirectory">This parameter may be null, but
        /// if specified it is the root directory
        /// for any relative paths in <paramref name="sourceFileNames"/>.</param>
        /// <param name="sourceFileNames">The list of files to be included in
        /// the archive.</param>
        /// <param name="fileNames">The names of the files as they are stored
        /// in the archive. Each name
        /// includes the internal path of the file, if any. This parameter may
        /// be null, in which case the files are stored in the archive with their
        /// source file names and no path information.</param>
        /// <remarks>
        /// Uses maximum compression level.
        /// <p>Duplicate items in the <paramref name="fileNames"/> array will cause
        /// an <see cref="ArchiveException"/>.</p>
        /// </remarks>
        public void PackFiles(
            string sourceDirectory,
            IList<string> sourceFileNames,
            IList<string> fileNames)
        {
            this.PackFiles(
                sourceDirectory,
                sourceFileNames,
                fileNames,
                CompressionLevel.Max,
                null);
        }

        /// <summary>
        /// Compresses files into the archive, specifying the names used to
        /// store the files in the archive.
        /// </summary>
        /// <param name="sourceDirectory">This parameter may be null, but if
        /// specified it is the root directory
        /// for any relative paths in <paramref name="sourceFileNames"/>.</param>
        /// <param name="sourceFileNames">The list of files to be included in
        /// the archive.</param>
        /// <param name="fileNames">The names of the files as they are stored in
        /// the archive. Each name includes the internal path of the file, if any.
        /// This parameter may be null, in which case the files are stored in the
        /// archive with their source file names and no path information.</param>
        /// <param name="compLevel">The compression level used when creating the
        /// archive.</param>
        /// <param name="progressHandler">Handler for receiving progress information;
        /// this may be null if progress is not desired.</param>
        /// <remarks>
        /// Duplicate items in the <paramref name="fileNames"/> array will cause
        /// an <see cref="ArchiveException"/>.
        /// </remarks>
        public void PackFiles(
            string sourceDirectory,
            IList<string> sourceFileNames,
            IList<string> fileNames,
            CompressionLevel compLevel,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            if (sourceFileNames == null)
            {
                throw new ArgumentNullException("sourceFileNames");
            }

            if (fileNames == null)
            {
                string[] fileNamesArray = new string[sourceFileNames.Count];
                for (int i = 0; i < sourceFileNames.Count; i++)
                {
                    fileNamesArray[i] = Path.GetFileName(sourceFileNames[i]);
                }

                fileNames = fileNamesArray;
            }
            else if (fileNames.Count != sourceFileNames.Count)
            {
                throw new ArgumentOutOfRangeException("fileNames");
            }

            using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
            {
                compressionEngine.Progress += progressHandler;
                IDictionary<string, string> contextFiles =
                    ArchiveInfo.CreateStringDictionary(fileNames, sourceFileNames);
                ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext(
                        this.FullName, sourceDirectory, contextFiles);
                streamContext.EnableOffsetOpen = true;
                compressionEngine.CompressionLevel = compLevel;
                compressionEngine.Pack(streamContext, fileNames);
            }
        }

        /// <summary>
        /// Compresses files into the archive, specifying the names used
        /// to store the files in the archive.
        /// </summary>
        /// <param name="sourceDirectory">This parameter may be null, but if
        /// specified it is the root directory
        /// for any relative paths in <paramref name="fileNames"/>.</param>
        /// <param name="fileNames">A mapping from internal file paths to
        /// external file paths.</param>
        /// <remarks>
        /// Uses maximum compression level.
        /// </remarks>
        public void PackFileSet(
            string sourceDirectory,
            IDictionary<string, string> fileNames)
        {
            this.PackFileSet(sourceDirectory, fileNames, CompressionLevel.Max, null);
        }

        /// <summary>
        /// Compresses files into the archive, specifying the names used to
        /// store the files in the archive.
        /// </summary>
        /// <param name="sourceDirectory">This parameter may be null, but if
        /// specified it is the root directory
        /// for any relative paths in <paramref name="fileNames"/>.</param>
        /// <param name="fileNames">A mapping from internal file paths to
        /// external file paths.</param>
        /// <param name="compLevel">The compression level used when creating
        /// the archive.</param>
        /// <param name="progressHandler">Handler for receiving progress information;
        /// this may be null if progress is not desired.</param>
        public void PackFileSet(
            string sourceDirectory,
            IDictionary<string, string> fileNames,
            CompressionLevel compLevel,
            EventHandler<ArchiveProgressEventArgs> progressHandler)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }

            string[] fileNamesArray = new string[fileNames.Count];
            fileNames.Keys.CopyTo(fileNamesArray, 0);

            using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
            {
                compressionEngine.Progress += progressHandler;
                ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext(
                        this.FullName, sourceDirectory, fileNames);
                streamContext.EnableOffsetOpen = true;
                compressionEngine.CompressionLevel = compLevel;
                compressionEngine.Pack(streamContext, fileNamesArray);
            }
        }

        /// <summary>
        /// Given a directory, gets the relative paths of all files in the
        /// directory, optionally including all subdirectories.
        /// </summary>
        /// <param name="dir">The directory to search.</param>
        /// <param name="includeSubdirectories">True to include subdirectories
        /// in the search.</param>
        /// <returns>A list of file paths relative to the directory.</returns>
        internal IList<string> GetRelativeFilePathsInDirectoryTree(
            string dir, bool includeSubdirectories)
        {
            IList<string> fileList = new List<string>();
            this.RecursiveGetRelativeFilePathsInDirectoryTree(
                dir, String.Empty, includeSubdirectories, fileList);
            return fileList;
        }

        /// <summary>
        /// Retrieves information about one file from this archive.
        /// </summary>
        /// <param name="path">Path of the file in the archive.</param>
        /// <returns>File information, or null if the file was not found
        /// in the archive.</returns>
        internal ArchiveFileInfo GetFile(string path)
        {
            IList<ArchiveFileInfo> files = this.InternalGetFiles(
                delegate(string match)
                {
                    return String.Compare(
                        match, path, StringComparison.OrdinalIgnoreCase) == 0;
                });
            return (files != null && files.Count > 0 ? files[0] : null);
        }

        /// <summary>
        /// Creates a compression engine that does the low-level work for
        /// this object.
        /// </summary>
        /// <returns>A new compression engine instance that matches the specific
        /// subclass of archive.</returns>
        /// <remarks>
        /// Each instance will be <see cref="CompressionEngine.Dispose()"/>d
        /// immediately after use.
        /// </remarks>
        protected abstract CompressionEngine CreateCompressionEngine();

        /// <summary>
        /// Creates a case-insensitive dictionary mapping from one list of
        /// strings to the other.
        /// </summary>
        /// <param name="keys">List of keys.</param>
        /// <param name="values">List of values that are mapped 1-to-1 to
        /// the keys.</param>
        /// <returns>A filled dictionary of the strings.</returns>
        private static IDictionary<string, string> CreateStringDictionary(
            IList<string> keys, IList<string> values)
        {
            IDictionary<string, string> stringDict =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < keys.Count; i++)
            {
                stringDict.Add(keys[i], values[i]);
            }

            return stringDict;
        }

        /// <summary>
        /// Recursive-descent helper function for
        /// GetRelativeFilePathsInDirectoryTree.
        /// </summary>
        /// <param name="dir">The root directory of the search.</param>
        /// <param name="relativeDir">The relative directory to be
        /// processed now.</param>
        /// <param name="includeSubdirectories">True to descend into
        /// subdirectories.</param>
        /// <param name="fileList">List of files found so far.</param>
        private void RecursiveGetRelativeFilePathsInDirectoryTree(
            string dir,
            string relativeDir,
            bool includeSubdirectories,
            IList<string> fileList)
        {
            foreach (string file in System.IO.Directory.GetFiles(dir))
            {
                string fileName = Path.GetFileName(file);
                fileList.Add(Path.Combine(relativeDir, fileName));
            }

            if (includeSubdirectories)
            {
                foreach (string subDir in System.IO.Directory.GetDirectories(dir))
                {
                    string subDirName = Path.GetFileName(subDir);
                    this.RecursiveGetRelativeFilePathsInDirectoryTree(
                        Path.Combine(dir, subDirName),
                        Path.Combine(relativeDir, subDirName),
                        includeSubdirectories,
                        fileList);
                }
            }
        }

        /// <summary>
        /// Uses a CompressionEngine to get ArchiveFileInfo objects from this
        /// archive, and then associates them with this ArchiveInfo instance.
        /// </summary>
        /// <param name="fileFilter">Optional predicate that can determine
        /// which files to process.</param>
        /// <returns>A list of <see cref="ArchiveFileInfo"/> objects, each
        /// containing information about a file in the archive.</returns>
        private IList<ArchiveFileInfo> InternalGetFiles(Predicate<string> fileFilter)
        {
            using (CompressionEngine compressionEngine = this.CreateCompressionEngine())
            {
                ArchiveFileStreamContext streamContext =
                    new ArchiveFileStreamContext(this.FullName, null, null);
                streamContext.EnableOffsetOpen = true;
                IList<ArchiveFileInfo> files =
                    compressionEngine.GetFileInfo(streamContext, fileFilter);
                for (int i = 0; i < files.Count; i++)
                {
                    files[i].Archive = this;
                }

                return files;
            }
        }
    }
}
