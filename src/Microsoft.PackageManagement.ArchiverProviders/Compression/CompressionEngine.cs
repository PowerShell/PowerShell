//---------------------------------------------------------------------
// <copyright file="CompressionEngine.cs" company="Microsoft Corporation">
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
    /// Base class for an engine capable of packing and unpacking a particular
    /// compressed file format.
    /// </summary>
    public abstract class CompressionEngine : IDisposable
    {
        private CompressionLevel compressionLevel;
        private bool dontUseTempFiles;

        /// <summary>
        /// Creates a new instance of the compression engine base class.
        /// </summary>
        protected CompressionEngine()
        {
            this.compressionLevel = CompressionLevel.Normal;
        }

        /// <summary>
        /// Disposes the compression engine.
        /// </summary>
        ~CompressionEngine()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Occurs when the compression engine reports progress in packing
        /// or unpacking an archive.
        /// </summary>
        /// <seealso cref="ArchiveProgressType"/>
        public event EventHandler<ArchiveProgressEventArgs> Progress;

        /// <summary>
        /// Gets or sets a flag indicating whether temporary files are created
        /// and used during compression.
        /// </summary>
        /// <value>True if temporary files are used; false if compression is done
        /// entirely in-memory.</value>
        /// <remarks>The value of this property is true by default. Using temporary
        /// files can greatly reduce the memory requirement of compression,
        /// especially when compressing large archives. However, setting this property
        /// to false may yield slightly better performance when creating small
        /// archives. Or it may be necessary if the process does not have sufficient
        /// privileges to create temporary files.</remarks>
        public bool UseTempFiles
        {
            get
            {
                return !this.dontUseTempFiles;
            }

            set
            {
                this.dontUseTempFiles = !value;
            }
        }

        /// <summary>
        /// Compression level to use when compressing files.
        /// </summary>
        /// <value>A compression level ranging from minimum to maximum compression,
        /// or no compression.</value>
        public CompressionLevel CompressionLevel
        {
            get
            {
                return this.compressionLevel;
            }

            set
            {
                this.compressionLevel = value;
            }
        }

        /// <summary>
        /// Disposes of resources allocated by the compression engine.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates an archive.
        /// </summary>
        /// <param name="streamContext">A context interface to handle opening
        /// and closing of archive and file streams.</param>
        /// <param name="files">The paths of the files in the archive
        /// (not external file paths).</param>
        /// <exception cref="ArchiveException">The archive could not be
        /// created.</exception>
        /// <remarks>
        /// The stream context implementation may provide a mapping from the
        /// file paths within the archive to the external file paths.
        /// </remarks>
        public void Pack(IPackStreamContext streamContext, IEnumerable<string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            this.Pack(streamContext, files, 0);
        }

        /// <summary>
        /// Creates an archive or chain of archives.
        /// </summary>
        /// <param name="streamContext">A context interface to handle opening
        /// and closing of archive and file streams.</param>
        /// <param name="files">The paths of the files in the archive (not
        /// external file paths).</param>
        /// <param name="maxArchiveSize">The maximum number of bytes for one
        /// archive before the contents are chained to the next archive, or zero
        /// for unlimited archive size.</param>
        /// <exception cref="ArchiveException">The archive could not be
        /// created.</exception>
        /// <remarks>
        /// The stream context implementation may provide a mapping from the file
        /// paths within the archive to the external file paths.
        /// </remarks>
        public abstract void Pack(
            IPackStreamContext streamContext,
            IEnumerable<string> files,
            long maxArchiveSize);

        /// <summary>
        /// Checks whether a Stream begins with a header that indicates
        /// it is a valid archive.
        /// </summary>
        /// <param name="stream">Stream for reading the archive file.</param>
        /// <returns>True if the stream is a valid archive
        /// (with no offset); false otherwise.</returns>
        public abstract bool IsArchive(Stream stream);

        /// <summary>
        /// Gets the offset of an archive that is positioned 0 or more bytes
        /// from the start of the Stream.
        /// </summary>
        /// <param name="stream">A stream for reading the archive.</param>
        /// <returns>The offset in bytes of the archive,
        /// or -1 if no archive is found in the Stream.</returns>
        /// <remarks>The archive must begin on a 4-byte boundary.</remarks>
        public virtual long FindArchiveOffset(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            long sectionSize = 4;
            long length = stream.Length;
            for (long offset = 0; offset <= length - sectionSize; offset += sectionSize)
            {
                stream.Seek(offset, SeekOrigin.Begin);
                if (this.IsArchive(stream))
                {
                    return offset;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets information about all files in an archive stream.
        /// </summary>
        /// <param name="stream">A stream for reading the archive.</param>
        /// <returns>Information about all files in the archive stream.</returns>
        /// <exception cref="ArchiveException">The stream is not a valid
        /// archive.</exception>
        public IList<ArchiveFileInfo> GetFileInfo(Stream stream)
        {
            return this.GetFileInfo(new BasicUnpackStreamContext(stream), null);
        }

        /// <summary>
        /// Gets information about files in an archive or archive chain.
        /// </summary>
        /// <param name="streamContext">A context interface to handle opening
        /// and closing of archive and file streams.</param>
        /// <param name="fileFilter">A predicate that can determine
        /// which files to process, optional.</param>
        /// <returns>Information about files in the archive stream.</returns>
        /// <exception cref="ArchiveException">The archive provided
        /// by the stream context is not valid.</exception>
        /// <remarks>
        /// The <paramref name="fileFilter"/> predicate takes an internal file
        /// path and returns true to include the file or false to exclude it.
        /// </remarks>
        public abstract IList<ArchiveFileInfo> GetFileInfo(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter);

        /// <summary>
        /// Gets the list of files in an archive Stream.
        /// </summary>
        /// <param name="stream">A stream for reading the archive.</param>
        /// <returns>A list of the paths of all files contained in the
        /// archive.</returns>
        /// <exception cref="ArchiveException">The stream is not a valid
        /// archive.</exception>
        public IList<string> GetFiles(Stream stream)
        {
            return this.GetFiles(new BasicUnpackStreamContext(stream), null);
        }

        /// <summary>
        /// Gets the list of files in an archive or archive chain.
        /// </summary>
        /// <param name="streamContext">A context interface to handle opening
        /// and closing of archive and file streams.</param>
        /// <param name="fileFilter">A predicate that can determine
        /// which files to process, optional.</param>
        /// <returns>An array containing the names of all files contained in
        /// the archive or archive chain.</returns>
        /// <exception cref="ArchiveException">The archive provided
        /// by the stream context is not valid.</exception>
        /// <remarks>
        /// The <paramref name="fileFilter"/> predicate takes an internal file
        /// path and returns true to include the file or false to exclude it.
        /// </remarks>
        public IList<string> GetFiles(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter)
        {
            if (streamContext == null)
            {
                throw new ArgumentNullException("streamContext");
            }

            IList<ArchiveFileInfo> files =
                this.GetFileInfo(streamContext, fileFilter);
            IList<string> fileNames = new List<string>(files.Count);
            for (int i = 0; i < files.Count; i++)
            {
                fileNames.Add(files[i].Name);
            }

            return fileNames;
        }

        /// <summary>
        /// Reads a single file from an archive stream.
        /// </summary>
        /// <param name="stream">A stream for reading the archive.</param>
        /// <param name="path">The path of the file within the archive
        /// (not the external file path).</param>
        /// <returns>A stream for reading the extracted file, or null
        /// if the file does not exist in the archive.</returns>
        /// <exception cref="ArchiveException">The stream is not a valid
        /// archive.</exception>
        /// <remarks>The entire extracted file is cached in memory, so this
        /// method requires enough free memory to hold the file.</remarks>
        public Stream Unpack(Stream stream, string path)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            BasicUnpackStreamContext streamContext =
                new BasicUnpackStreamContext(stream);
            this.Unpack(
                streamContext,
                delegate(string match)
                {
                    return String.Compare(
                        match, path, StringComparison.OrdinalIgnoreCase) == 0;
                });

            Stream extractStream = streamContext.FileStream;
            if (extractStream != null)
            {
                extractStream.Position = 0;
            }

            return extractStream;
        }

        /// <summary>
        /// Extracts files from an archive or archive chain.
        /// </summary>
        /// <param name="streamContext">A context interface to handle opening
        /// and closing of archive and file streams.</param>
        /// <param name="fileFilter">An optional predicate that can determine
        /// which files to process.</param>
        /// <exception cref="ArchiveException">The archive provided
        /// by the stream context is not valid.</exception>
        /// <remarks>
        /// The <paramref name="fileFilter"/> predicate takes an internal file
        /// path and returns true to include the file or false to exclude it.
        /// </remarks>
        public abstract void Unpack(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter);

        /// <summary>
        /// Called by subclasses to distribute a packing or unpacking progress
        /// event to listeners.
        /// </summary>
        /// <param name="e">Event details.</param>
        protected void OnProgress(ArchiveProgressEventArgs e)
        {
            if (this.Progress != null)
            {
                this.Progress(this, e);
            }
        }

        /// <summary>
        /// Disposes of resources allocated by the compression engine.
        /// </summary>
        /// <param name="disposing">If true, the method has been called
        /// directly or indirectly by a user's code, so managed and unmanaged
        /// resources will be disposed. If false, the method has been called by
        /// the runtime from inside the finalizer, and only unmanaged resources
        /// will be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Compression utility function for converting old-style
        /// date and time values to a DateTime structure.
        /// </summary>
        public static void DosDateAndTimeToDateTime(
            short dosDate, short dosTime, out DateTime dateTime)
        {
            if (dosDate == 0 && dosTime == 0)
            {
                dateTime = DateTime.MinValue;
            }
            else
            {
                long fileTime;
                SafeNativeMethods.DosDateTimeToFileTime(dosDate, dosTime, out fileTime);
                dateTime = DateTime.FromFileTimeUtc(fileTime);
                dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Local);
            }
        }

        /// <summary>
        /// Compression utility function for converting a DateTime structure
        /// to old-style date and time values.
        /// </summary>
        public static void DateTimeToDosDateAndTime(
            DateTime dateTime, out short dosDate, out short dosTime)
        {
            dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);
            long filetime = dateTime.ToFileTimeUtc();
            SafeNativeMethods.FileTimeToDosDateTime(ref filetime, out dosDate, out dosTime);
        }
    }
}
