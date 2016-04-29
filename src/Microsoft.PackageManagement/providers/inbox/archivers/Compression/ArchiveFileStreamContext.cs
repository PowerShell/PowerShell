//---------------------------------------------------------------------
// <copyright file="ArchiveFileStreamContext.cs" company="Microsoft Corporation">
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
    using System.IO;

    /// <summary>
    /// Provides a basic implementation of the archive pack and unpack stream context
    /// interfaces, based on a list of archive files, a default directory, and an
    /// optional mapping from internal to external file paths.
    /// </summary>
    /// <remarks>
    /// This class can also handle creating or extracting chained archive packages.
    /// </remarks>
    public class ArchiveFileStreamContext
        : IPackStreamContext, IUnpackStreamContext
    {
        private IList<string> archiveFiles;
        private string directory;
        private IDictionary<string, string> files;
        private bool extractOnlyNewerFiles;
        private bool enableOffsetOpen;

        #region Constructors

        /// <summary>
        /// Creates a new ArchiveFileStreamContext with a archive file and
        /// no default directory or file mapping.
        /// </summary>
        /// <param name="archiveFile">The path to a archive file that will be
        /// created or extracted.</param>
        public ArchiveFileStreamContext(string archiveFile)
            : this(archiveFile, null, null)
        {
        }

        /// <summary>
        /// Creates a new ArchiveFileStreamContext with a archive file, default
        /// directory and mapping from internal to external file paths.
        /// </summary>
        /// <param name="archiveFile">The path to a archive file that will be
        /// created or extracted.</param>
        /// <param name="directory">The default root directory where files will be
        /// located, optional.</param>
        /// <param name="files">A mapping from internal file paths to external file
        /// paths, optional.</param>
        /// <remarks>
        /// If the mapping is not null and a file is not included in the mapping,
        /// the file will be skipped.
        /// <para>If the external path in the mapping is a simple file name or
        /// relative file path, it will be concatenated onto the default directory,
        /// if one was specified.</para>
        /// <para>For more about how the default directory and files mapping are
        /// used, see <see cref="OpenFileReadStream"/> and
        /// <see cref="OpenFileWriteStream"/>.</para>
        /// </remarks>
        public ArchiveFileStreamContext(
            string archiveFile,
            string directory,
            IDictionary<string, string> files)
            : this(new string[] { archiveFile }, directory, files)
        {
            if (archiveFile == null)
            {
                throw new ArgumentNullException("archiveFile");
            }
        }

        /// <summary>
        /// Creates a new ArchiveFileStreamContext with a list of archive files,
        /// a default directory and a mapping from internal to external file paths.
        /// </summary>
        /// <param name="archiveFiles">A list of paths to archive files that will be
        /// created or extracted.</param>
        /// <param name="directory">The default root directory where files will be
        /// located, optional.</param>
        /// <param name="files">A mapping from internal file paths to external file
        /// paths, optional.</param>
        /// <remarks>
        /// When creating chained archives, the <paramref name="archiveFiles"/> list
        /// should include at least enough archives to handle the entire set of
        /// input files, based on the maximum archive size that is passed to the
        /// <see cref="CompressionEngine"/>.<see
        /// cref="CompressionEngine.Pack(IPackStreamContext,IEnumerable&lt;string&gt;,long)"/>.
        /// <para>If the mapping is not null and a file is not included in the mapping,
        /// the file will be skipped.</para>
        /// <para>If the external path in the mapping is a simple file name or
        /// relative file path, it will be concatenated onto the default directory,
        /// if one was specified.</para>
        /// <para>For more about how the default directory and files mapping are used,
        /// see <see cref="OpenFileReadStream"/> and
        /// <see cref="OpenFileWriteStream"/>.</para>
        /// </remarks>
        public ArchiveFileStreamContext(
            IList<string> archiveFiles,
            string directory,
            IDictionary<string, string> files)
        {
            if (archiveFiles == null || archiveFiles.Count == 0)
            {
                throw new ArgumentNullException("archiveFiles");
            }

            this.archiveFiles = archiveFiles;
            this.directory = directory;
            this.files = files;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the list of archive files that are created or extracted.
        /// </summary>
        /// <value>The list of archive files that are created or extracted.</value>
        public IList<string> ArchiveFiles
        {
            get
            {
                return this.archiveFiles;
            }
        }

        /// <summary>
        /// Gets or sets the default root directory where files are located.
        /// </summary>
        /// <value>The default root directory where files are located.</value>
        /// <remarks>
        /// For details about how the default directory is used,
        /// see <see cref="OpenFileReadStream"/> and <see cref="OpenFileWriteStream"/>.
        /// </remarks>
        public string Directory
        {
            get
            {
                return this.directory;
            }
        }

        /// <summary>
        /// Gets or sets the mapping from internal file paths to external file paths.
        /// </summary>
        /// <value>A mapping from internal file paths to external file paths.</value>
        /// <remarks>
        /// For details about how the files mapping is used,
        /// see <see cref="OpenFileReadStream"/> and <see cref="OpenFileWriteStream"/>.
        /// </remarks>
        public IDictionary<string, string> Files
        {
            get
            {
                return this.files;
            }
        }

        /// <summary>
        /// Gets or sets a flag that can prevent extracted files from overwriting
        /// newer files that already exist.
        /// </summary>
        /// <value>True to prevent overwriting newer files that already exist
        /// during extraction; false to always extract from the archive regardless
        /// of existing files.</value>
        public bool ExtractOnlyNewerFiles
        {
            get
            {
                return this.extractOnlyNewerFiles;
            }

            set
            {
                this.extractOnlyNewerFiles = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag that enables creating or extracting an archive
        /// at an offset within an existing file. (This is typically used to open
        /// archive-based self-extracting packages.)
        /// </summary>
        /// <value>True to search an existing package file for an archive offset
        /// or the end of the file;/ false to always create or open a plain
        /// archive file.</value>
        public bool EnableOffsetOpen
        {
            get
            {
                return this.enableOffsetOpen;
            }

            set
            {
                this.enableOffsetOpen = value;
            }
        }

        #endregion

        #region IPackStreamContext Members

        /// <summary>
        /// Gets the name of the archive with a specified number.
        /// </summary>
        /// <param name="archiveNumber">The 0-based index of the archive within
        /// the chain.</param>
        /// <returns>The name of the requested archive. May be an empty string
        /// for non-chained archives, but may never be null.</returns>
        /// <remarks>This method returns the file name of the archive from the
        /// <see cref="archiveFiles"/> list with the specified index, or an empty
        /// string if the archive number is outside the bounds of the list. The
        /// file name should not include any directory path.</remarks>
        public virtual string GetArchiveName(int archiveNumber)
        {
            if (archiveNumber < this.archiveFiles.Count)
            {
                return Path.GetFileName(this.archiveFiles[archiveNumber]);
            }

            return String.Empty;
        }

        /// <summary>
        /// Opens a stream for writing an archive.
        /// </summary>
        /// <param name="archiveNumber">The 0-based index of the archive within
        /// the chain.</param>
        /// <param name="archiveName">The name of the archive that was returned
        /// by <see cref="GetArchiveName"/>.</param>
        /// <param name="truncate">True if the stream should be truncated when
        /// opened (if it already exists); false if an existing stream is being
        /// re-opened for writing additional data.</param>
        /// <param name="compressionEngine">Instance of the compression engine
        /// doing the operations.</param>
        /// <returns>A writable Stream where the compressed archive bytes will be
        /// written, or null to cancel the archive creation.</returns>
        /// <remarks>
        /// This method opens the file from the <see cref="ArchiveFiles"/> list
        /// with the specified index. If the archive number is outside the bounds
        /// of the list, this method returns null.
        /// <para>If the <see cref="EnableOffsetOpen"/> flag is set, this method
        /// will seek to the start of any existing archive in the file, or to the
        /// end of the file if the existing file is not an archive.</para>
        /// </remarks>
        public virtual Stream OpenArchiveWriteStream(
            int archiveNumber,
            string archiveName,
            bool truncate,
            CompressionEngine compressionEngine)
        {
            if (archiveNumber >= this.archiveFiles.Count)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(archiveName))
            {
                throw new ArgumentNullException("archiveName");
            }

            // All archives must be in the same directory,
            // so always use the directory from the first archive.
            string archiveFile = Path.Combine(
                Path.GetDirectoryName(this.archiveFiles[0]), archiveName);
            Stream stream = File.Open(
                archiveFile,
                (truncate ? FileMode.OpenOrCreate : FileMode.Open),
                FileAccess.ReadWrite);

            if (this.enableOffsetOpen)
            {
                if (compressionEngine == null) {
                    throw new ArgumentNullException("compressionEngine");
                }

                long offset = compressionEngine.FindArchiveOffset(
                    new DuplicateStream(stream));

                // If this is not an archive file, append the archive to it.
                if (offset < 0)
                {
                    offset = stream.Length;
                }

                if (offset > 0)
                {
                    stream = new OffsetStream(stream, offset);
                }

                stream.Seek(0, SeekOrigin.Begin);
            }

            if (truncate)
            {
                // Truncate the stream, in case a larger old archive starts here.
                stream.SetLength(0);
            }

            return stream;
        }

        /// <summary>
        /// Closes a stream where an archive package was written.
        /// </summary>
        /// <param name="archiveNumber">The 0-based index of the archive within
        /// the chain.</param>
        /// <param name="archiveName">The name of the archive that was previously
        /// returned by <see cref="GetArchiveName"/>.</param>
        /// <param name="stream">A stream that was previously returned by
        /// <see cref="OpenArchiveWriteStream"/> and is now ready to be closed.</param>
        public virtual void CloseArchiveWriteStream(
            int archiveNumber,
            string archiveName,
            Stream stream)
        {
            if (stream != null)
            {
#if CORECLR
                stream.Dispose();
#else
                stream.Close();
#endif

                FileStream fileStream = stream as FileStream;
                if (fileStream != null)
                {
                    string streamFile = fileStream.Name;
                    if (!string.IsNullOrWhiteSpace(archiveName) &&
                        archiveName != Path.GetFileName(streamFile))
                    {
                        string archiveFile = Path.Combine(
                            Path.GetDirectoryName(this.archiveFiles[0]), archiveName);
                        if (File.Exists(archiveFile))
                        {
                            File.Delete(archiveFile);
                        }
                        File.Move(streamFile, archiveFile);
                    }
                }
            }
        }

        /// <summary>
        /// Opens a stream to read a file that is to be included in an archive.
        /// </summary>
        /// <param name="path">The path of the file within the archive.</param>
        /// <param name="attributes">The returned attributes of the opened file,
        /// to be stored in the archive.</param>
        /// <param name="lastWriteTime">The returned last-modified time of the
        /// opened file, to be stored in the archive.</param>
        /// <returns>A readable Stream where the file bytes will be read from
        /// before they are compressed, or null to skip inclusion of the file and
        /// continue to the next file.</returns>
        /// <remarks>
        /// This method opens a file using the following logic:
        /// <list>
        /// <item>If the <see cref="Directory"/> and the <see cref="Files"/> mapping
        /// are both null, the path is treated as relative to the current directory,
        /// and that file is opened.</item>
        /// <item>If the <see cref="Directory"/> is not null but the <see cref="Files"/>
        /// mapping is null, the path is treated as relative to that directory, and
        /// that file is opened.</item>
        /// <item>If the <see cref="Directory"/> is null but the <see cref="Files"/>
        /// mapping is not null, the path parameter is used as a key into the mapping,
        /// and the resulting value is the file path that is opened, relative to the
        /// current directory (or it may be an absolute path). If no mapping exists,
        /// the file is skipped.</item>
        /// <item>If both the <see cref="Directory"/> and the <see cref="Files"/>
        /// mapping are specified, the path parameter is used as a key into the
        /// mapping, and the resulting value is the file path that is opened, relative
        /// to the specified directory (or it may be an absolute path). If no mapping
        /// exists, the file is skipped.</item>
        /// </list>
        /// </remarks>
        public virtual Stream OpenFileReadStream(
            string path, out FileAttributes attributes, out DateTime lastWriteTime)
        {
            string filePath = this.TranslateFilePath(path);

            if (filePath == null)
            {
                attributes = FileAttributes.Normal;
                lastWriteTime = DateTime.Now;
                return null;
            }

            attributes = File.GetAttributes(filePath);
            lastWriteTime = File.GetLastWriteTime(filePath);
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// Closes a stream that has been used to read a file.
        /// </summary>
        /// <param name="path">The path of the file within the archive; the same as
        /// the path provided when the stream was opened.</param>
        /// <param name="stream">A stream that was previously returned by
        /// <see cref="OpenFileReadStream"/> and is now ready to be closed.</param>
        public virtual void CloseFileReadStream(string path, Stream stream)
        {
            if (stream != null)
            {
#if CORECLR
                stream.Dispose();
#else
                stream.Close();
#endif
            }
        }

        /// <summary>
        /// Gets extended parameter information specific to the compression format
        /// being used.
        /// </summary>
        /// <param name="optionName">Name of the option being requested.</param>
        /// <param name="parameters">Parameters for the option; for per-file options,
        /// the first parameter is typically the internal file path.</param>
        /// <returns>Option value, or null to use the default behavior.</returns>
        /// <remarks>
        /// This implementation does not handle any options. Subclasses may override
        /// this method to allow for non-default behavior.
        /// </remarks>
        public virtual object GetOption(string optionName, object[] parameters)
        {
            return null;
        }

        #endregion

        #region IUnpackStreamContext Members

        /// <summary>
        /// Opens the archive stream for reading.
        /// </summary>
        /// <param name="archiveNumber">The zero-based index of the archive to
        /// open.</param>
        /// <param name="archiveName">The name of the archive being opened.</param>
        /// <param name="compressionEngine">Instance of the compression engine
        /// doing the operations.</param>
        /// <returns>A stream from which archive bytes are read, or null to cancel
        /// extraction of the archive.</returns>
        /// <remarks>
        /// This method opens the file from the <see cref="ArchiveFiles"/> list with
        /// the specified index. If the archive number is outside the bounds of the
        /// list, this method returns null.
        /// <para>If the <see cref="EnableOffsetOpen"/> flag is set, this method will
        /// seek to the start of any existing archive in the file, or to the end of
        /// the file if the existing file is not an archive.</para>
        /// </remarks>
        public virtual Stream OpenArchiveReadStream(
            int archiveNumber, string archiveName, CompressionEngine compressionEngine)
        {
            if (archiveNumber >= this.archiveFiles.Count)
            {
                return null;
            }

            string archiveFile = this.archiveFiles[archiveNumber];
            Stream stream = File.Open(
                archiveFile, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (this.enableOffsetOpen)
            {
                if (compressionEngine == null) {
                    throw new ArgumentNullException("compressionEngine");
                }
                long offset = compressionEngine.FindArchiveOffset(
                    new DuplicateStream(stream));
                if (offset > 0)
                {
                    stream = new OffsetStream(stream, offset);
                }
                else
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            return stream;
        }

        /// <summary>
        /// Closes a stream where an archive was read.
        /// </summary>
        /// <param name="archiveNumber">The archive number of the stream
        /// to close.</param>
        /// <param name="archiveName">The name of the archive being closed.</param>
        /// <param name="stream">The stream that was previously returned by
        /// <see cref="OpenArchiveReadStream"/> and is now ready to be closed.</param>
        public virtual void CloseArchiveReadStream(
            int archiveNumber, string archiveName, Stream stream)
        {
            if (stream != null)
            {
#if CORECLR
                stream.Dispose();
#else
                stream.Close();
#endif
            }
        }

        /// <summary>
        /// Opens a stream for writing extracted file bytes.
        /// </summary>
        /// <param name="path">The path of the file within the archive.</param>
        /// <param name="fileSize">The uncompressed size of the file to be
        /// extracted.</param>
        /// <param name="lastWriteTime">The last write time of the file to be
        /// extracted.</param>
        /// <returns>A stream where extracted file bytes are to be written, or null
        /// to skip extraction of the file and continue to the next file.</returns>
        /// <remarks>
        /// This method opens a file using the following logic:
        /// <list>
        /// <item>If the <see cref="Directory"/> and the <see cref="Files"/> mapping
        /// are both null, the path is treated as relative to the current directory,
        /// and that file is opened.</item>
        /// <item>If the <see cref="Directory"/> is not null but the <see cref="Files"/>
        /// mapping is null, the path is treated as relative to that directory, and
        /// that file is opened.</item>
        /// <item>If the <see cref="Directory"/> is null but the <see cref="Files"/>
        /// mapping is not null, the path parameter is used as a key into the mapping,
        /// and the resulting value is the file path that is opened, relative to the
        /// current directory (or it may be an absolute path). If no mapping exists,
        /// the file is skipped.</item>
        /// <item>If both the <see cref="Directory"/> and the <see cref="Files"/>
        /// mapping are specified, the path parameter is used as a key into the
        /// mapping, and the resulting value is the file path that is opened,
        /// relative to the specified directory (or it may be an absolute path).
        /// If no mapping exists, the file is skipped.</item>
        /// </list>
        /// <para>If the <see cref="ExtractOnlyNewerFiles"/> flag is set, the file
        /// is skipped if a file currently exists in the same path with an equal
        /// or newer write time.</para>
        /// </remarks>
        public virtual Stream OpenFileWriteStream(
            string path,
            long fileSize,
            DateTime lastWriteTime)
        {
            string filePath = this.TranslateFilePath(path);

            if (filePath == null)
            {
                return null;
            }

            FileInfo file = new FileInfo(filePath);
            if (file.Exists)
            {
                if (this.extractOnlyNewerFiles && lastWriteTime != DateTime.MinValue)
                {
                    if (file.LastWriteTime >= lastWriteTime)
                    {
                        return null;
                    }
                }

                // Clear attributes that will prevent overwriting the file.
                // (The final attributes will be set after the file is unpacked.)
                FileAttributes attributesToClear =
                    FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System;
                if ((file.Attributes & attributesToClear) != 0)
                {
                    file.Attributes &= ~attributesToClear;
                }
            }

            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            return File.Open(
                filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// Closes a stream where an extracted file was written.
        /// </summary>
        /// <param name="path">The path of the file within the archive.</param>
        /// <param name="stream">The stream that was previously returned by
        /// <see cref="OpenFileWriteStream"/> and is now ready to be closed.</param>
        /// <param name="attributes">The attributes of the extracted file.</param>
        /// <param name="lastWriteTime">The last write time of the file.</param>
        /// <remarks>
        /// After closing the extracted file stream, this method applies the date
        /// and attributes to that file.
        /// </remarks>
        public virtual void CloseFileWriteStream(
            string path,
            Stream stream,
            FileAttributes attributes,
            DateTime lastWriteTime)
        {
            if (stream != null)
            {
#if CORECLR
                stream.Dispose();
#else
                stream.Close();
#endif
            }

            string filePath = this.TranslateFilePath(path);
            if (filePath != null)
            {
                FileInfo file = new FileInfo(filePath);

                if (lastWriteTime != DateTime.MinValue)
                {
                    try
                    {
                        file.LastWriteTime = lastWriteTime;
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                }

                try
                {
                    file.Attributes = attributes;
                }
                catch (IOException)
                {
                }
            }
        }

        #endregion

        #region Private utility methods

        /// <summary>
        /// Translates an internal file path to an external file path using the
        /// <see cref="Directory"/> and the <see cref="Files"/> mapping, according to
        /// rules documented in <see cref="OpenFileReadStream"/> and
        /// <see cref="OpenFileWriteStream"/>.
        /// </summary>
        /// <param name="path">The path of the file with the archive.</param>
        /// <returns>The external path of the file, or null if there is no
        /// valid translation.</returns>
        private string TranslateFilePath(string path)
        {
            string filePath;
            if (this.files != null)
            {
                filePath = this.files[path];
            }
            else
            {
                filePath = path;
            }

            if (filePath != null)
            {
                if (this.directory != null)
                {
                    filePath = Path.Combine(this.directory, filePath);
                }
            }

            return filePath;
        }

        #endregion
    }
}
