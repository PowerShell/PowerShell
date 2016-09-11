//---------------------------------------------------------------------
// <copyright file="ZipEngine.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Zip
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Engine capable of packing and unpacking archives in the zip format.
    /// </summary>
    public partial class ZipEngine : CompressionEngine
    {
        private static Dictionary<ZipCompressionMethod, Func<Stream, Stream>>
            compressionStreamCreators;
        private static Dictionary<ZipCompressionMethod, Func<Stream, Stream>>
            decompressionStreamCreators;

        private static void InitCompressionStreamCreators()
        {
            if (ZipEngine.compressionStreamCreators == null)
            {
                ZipEngine.compressionStreamCreators = new
                    Dictionary<ZipCompressionMethod, Func<Stream, Stream>>();
                ZipEngine.decompressionStreamCreators = new
                    Dictionary<ZipCompressionMethod, Func<Stream, Stream>>();

                ZipEngine.RegisterCompressionStreamCreator(
                    ZipCompressionMethod.Store,
                    CompressionMode.Compress,
                    delegate(Stream stream)
                    {
                        return stream;
                    });
                ZipEngine.RegisterCompressionStreamCreator(
                    ZipCompressionMethod.Deflate,
                    CompressionMode.Compress,
                    delegate(Stream stream)
                    {
                        return new DeflateStream(stream, CompressionMode.Compress, true);
                    });
                ZipEngine.RegisterCompressionStreamCreator(
                    ZipCompressionMethod.Store,
                    CompressionMode.Decompress,
                    delegate(Stream stream)
                    {
                        return stream;
                    });
                ZipEngine.RegisterCompressionStreamCreator(
                    ZipCompressionMethod.Deflate,
                    CompressionMode.Decompress,
                    delegate(Stream stream)
                    {
                        return new DeflateStream(stream, CompressionMode.Decompress, true);
                    });
            }
        }

        /// <summary>
        /// Registers a delegate that can create a wrapper stream for
        /// compressing or uncompressing the data of a source stream.
        /// </summary>
        /// <param name="compressionMethod">Compression method being registered.</param>
        /// <param name="compressionMode">Indicates registration for ether
        /// compress or decompress mode.</param>
        /// <param name="creator">Delegate being registered.</param>
        /// <remarks>
        /// For compression, the delegate accepts a stream that writes to the archive
        /// and returns a wrapper stream that compresses bytes as they are written.
        /// For decompression, the delegate accepts a stream that reads from the archive
        /// and returns a wrapper stream that decompresses bytes as they are read.
        /// This wrapper stream model follows the design used by
        /// System.IO.Compression.DeflateStream, and indeed that class is used
        /// to implement the Deflate compression method by default.
        /// <para>To unregister a delegate, call this method again and pass
        /// null for the delegate parameter.</para>
        /// </remarks>
        /// <example>
        /// When the ZipEngine class is initialized, the Deflate compression method
        /// is automatically registered like this:
        /// <code>
        ///        ZipEngine.RegisterCompressionStreamCreator(
        ///            ZipCompressionMethod.Deflate,
        ///            CompressionMode.Compress,
        ///            delegate(Stream stream) {
        ///                return new DeflateStream(stream, CompressionMode.Compress, true);
        ///            });
        ///        ZipEngine.RegisterCompressionStreamCreator(
        ///            ZipCompressionMethod.Deflate,
        ///            CompressionMode.Decompress,
        ///            delegate(Stream stream) {
        ///                return new DeflateStream(stream, CompressionMode.Decompress, true);
        ///            });
        /// </code></example>
        public static void RegisterCompressionStreamCreator(
            ZipCompressionMethod compressionMethod,
            CompressionMode compressionMode,
            Func<Stream, Stream> creator)
        {
            ZipEngine.InitCompressionStreamCreators();
            if (compressionMode == CompressionMode.Compress)
            {
                ZipEngine.compressionStreamCreators[compressionMethod] = creator;
            }
            else
            {
                ZipEngine.decompressionStreamCreators[compressionMethod] = creator;
            }
        }

        // Progress data
        private string currentFileName;
        private int currentFileNumber;
        private int totalFiles;
        private long currentFileBytesProcessed;
        private long currentFileTotalBytes;
        private string mainArchiveName;
        private string currentArchiveName;
        private short currentArchiveNumber;
        private short totalArchives;
        private long currentArchiveBytesProcessed;
        private long currentArchiveTotalBytes;
        private long fileBytesProcessed;
        private long totalFileBytes;
        private string comment;

        /// <summary>
        /// Creates a new instance of the zip engine.
        /// </summary>
        public ZipEngine()
            : base()
        {
            ZipEngine.InitCompressionStreamCreators();
        }

        /// <summary>
        /// Gets the comment from the last-examined archive,
        /// or sets the comment to be added to any created archives.
        /// </summary>
        public string ArchiveComment
        {
            get
            {
                return this.comment;
            }
            set
            {
                this.comment = value;
            }
        }

        /// <summary>
        /// Checks whether a Stream begins with a header that indicates
        /// it is a valid archive file.
        /// </summary>
        /// <param name="stream">Stream for reading the archive file.</param>
        /// <returns>True if the stream is a valid zip archive
        /// (with no offset); false otherwise.</returns>
        public override bool IsArchive(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (stream.Length - stream.Position < 4)
            {
                return false;
            }

            BinaryReader reader = new BinaryReader(stream);
            uint sig = reader.ReadUInt32();
            switch (sig)
            {
                case ZipFileHeader.LFHSIG:
                case ZipEndOfCentralDirectory.EOCDSIG:
                case ZipEndOfCentralDirectory.EOCD64SIG:
                case ZipFileHeader.SPANSIG:
                case ZipFileHeader.SPANSIG2:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the offset of an archive that is positioned 0 or more bytes
        /// from the start of the Stream.
        /// </summary>
        /// <param name="stream">A stream for reading the archive.</param>
        /// <returns>The offset in bytes of the archive,
        /// or -1 if no archive is found in the Stream.</returns>
        /// <remarks>The archive must begin on a 4-byte boundary.</remarks>
        public override long FindArchiveOffset(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            long offset = base.FindArchiveOffset(stream);
            if (offset > 0)
            {
                // Some self-extract packages include the exe stub in file offset calculations.
                // Check the first header directory offset to decide whether the entire
                // archive needs to be offset or not.

                ZipEndOfCentralDirectory eocd = this.GetEOCD(null, stream);
                if (eocd != null && eocd.totalEntries > 0)
                {
                    stream.Seek(eocd.dirOffset, SeekOrigin.Begin);

                    ZipFileHeader header = new ZipFileHeader();
                    if (header.Read(stream, true) && header.localHeaderOffset < stream.Length)
                    {
                        stream.Seek(header.localHeaderOffset, SeekOrigin.Begin);
                        if (header.Read(stream, false))
                        {
                            return 0;
                        }
                    }
                }
            }

            return offset;
        }

        /// <summary>
        /// Gets information about files in a zip archive or archive chain.
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
        public override IList<ArchiveFileInfo> GetFileInfo(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter)
        {
            if (streamContext == null)
            {
                throw new ArgumentNullException("streamContext");
            }

            lock (this)
            {
                IList<ZipFileHeader> headers = this.GetCentralDirectory(streamContext);
                if (headers == null)
                {
                    throw new ZipException("Zip central directory not found.");
                }

                List<ArchiveFileInfo> files = new List<ArchiveFileInfo>(headers.Count);
                foreach (ZipFileHeader header in headers)
                {
                    if (!header.IsDirectory &&
                        (fileFilter == null || fileFilter(header.fileName)))
                    {
                        files.Add(header.ToZipFileInfo());
                    }
                }

                return files.AsReadOnly();
            }
        }

        /// <summary>
        /// Reads all the file headers from the central directory in the main archive.
        /// </summary>
        private IList<ZipFileHeader> GetCentralDirectory(IUnpackStreamContext streamContext)
        {
            Stream archiveStream = null;
            this.currentArchiveNumber = 0;
            try
            {
                List<ZipFileHeader> headers = new List<ZipFileHeader>();
                archiveStream = this.OpenArchive(streamContext, 0);

                ZipEndOfCentralDirectory eocd = this.GetEOCD(streamContext, archiveStream);
                if (eocd == null)
                {
                    return null;
                }
                else if (eocd.totalEntries == 0)
                {
                    return headers;
                }

                headers.Capacity = (int)eocd.totalEntries;

                if (eocd.dirOffset > archiveStream.Length - ZipFileHeader.CFH_FIXEDSIZE)
                {
                    streamContext.CloseArchiveReadStream(
                        this.currentArchiveNumber, String.Empty, archiveStream);
                    archiveStream = null;
                }
                else
                {
                    archiveStream.Seek(eocd.dirOffset, SeekOrigin.Begin);
                    uint sig = new BinaryReader(archiveStream).ReadUInt32();
                    if (sig != ZipFileHeader.CFHSIG)
                    {
                        streamContext.CloseArchiveReadStream(
                            this.currentArchiveNumber, String.Empty, archiveStream);
                        archiveStream = null;
                    }
                }

                if (archiveStream == null)
                {
                    this.currentArchiveNumber = (short)(eocd.dirStartDiskNumber + 1);
                    archiveStream = streamContext.OpenArchiveReadStream(
                        this.currentArchiveNumber, String.Empty, this);

                    if (archiveStream == null)
                    {
                        return null;
                    }
                }

                archiveStream.Seek(eocd.dirOffset, SeekOrigin.Begin);

                while (headers.Count < eocd.totalEntries)
                {
                    ZipFileHeader header = new ZipFileHeader();
                    if (!header.Read(archiveStream, true))
                    {
                        throw new ZipException(
                            "Missing or invalid central directory file header");
                    }

                    headers.Add(header);

                    if (headers.Count < eocd.totalEntries &&
                        archiveStream.Position == archiveStream.Length)
                    {
                        streamContext.CloseArchiveReadStream(
                            this.currentArchiveNumber, String.Empty, archiveStream);
                        this.currentArchiveNumber++;
                        archiveStream = streamContext.OpenArchiveReadStream(
                            this.currentArchiveNumber, String.Empty, this);
                        if (archiveStream == null)
                        {
                            this.currentArchiveNumber = 0;
                            archiveStream = streamContext.OpenArchiveReadStream(
                                this.currentArchiveNumber, String.Empty, this);
                        }
                    }
                }

                return headers;
            }
            finally
            {
                if (archiveStream != null)
                {
                    streamContext.CloseArchiveReadStream(
                        this.currentArchiveNumber, String.Empty, archiveStream);
                }
            }
        }

        /// <summary>
        /// Locates and reads the end of central directory record near the
        /// end of the archive.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "streamContext")]
        private ZipEndOfCentralDirectory GetEOCD(
            IUnpackStreamContext streamContext, Stream archiveStream)
        {
            BinaryReader reader = new BinaryReader(archiveStream);
            long offset = archiveStream.Length
                - ZipEndOfCentralDirectory.EOCD_RECORD_FIXEDSIZE;
            while (offset >= 0)
            {
                archiveStream.Seek(offset, SeekOrigin.Begin);

                uint sig = reader.ReadUInt32();
                if (sig == ZipEndOfCentralDirectory.EOCDSIG)
                {
                    break;
                }

                offset--;
            }

            if (offset < 0)
            {
                return null;
            }

            ZipEndOfCentralDirectory eocd = new ZipEndOfCentralDirectory();
            archiveStream.Seek(offset, SeekOrigin.Begin);
            if (!eocd.Read(archiveStream))
            {
                throw new ZipException("Invalid end of central directory record");
            }

            if (eocd.dirOffset == (long)UInt32.MaxValue)
            {
                string saveComment = eocd.comment;

                archiveStream.Seek(
                    offset - Zip64EndOfCentralDirectoryLocator.EOCDL64_SIZE,
                    SeekOrigin.Begin);

                Zip64EndOfCentralDirectoryLocator eocdl =
                    new Zip64EndOfCentralDirectoryLocator();
                if (!eocdl.Read(archiveStream))
                {
                    throw new ZipException("Missing or invalid end of " +
                        "central directory record locator");
                }

                if (eocdl.dirStartDiskNumber == eocdl.totalDisks - 1)
                {
                    // ZIP64 eocd is entirely in current stream.
                    archiveStream.Seek(eocdl.dirOffset, SeekOrigin.Begin);
                    if (!eocd.Read(archiveStream))
                    {
                        throw new ZipException("Missing or invalid ZIP64 end of " +
                            "central directory record");
                    }
                }
                else if (streamContext == null)
                {
                    return null;
                }
                else
                {
                    // TODO: handle EOCD64 spanning archives!
                    throw new NotImplementedException("Zip implementation does not " +
                        "handle end of central directory record that spans archives.");
                }

                eocd.comment = saveComment;
            }

            return eocd;
        }

        private void ResetProgressData()
        {
            this.currentFileName = null;
            this.currentFileNumber = 0;
            this.totalFiles = 0;
            this.currentFileBytesProcessed = 0;
            this.currentFileTotalBytes = 0;
            this.currentArchiveName = null;
            this.currentArchiveNumber = 0;
            this.totalArchives = 0;
            this.currentArchiveBytesProcessed = 0;
            this.currentArchiveTotalBytes = 0;
            this.fileBytesProcessed = 0;
            this.totalFileBytes = 0;
        }

        private void OnProgress(ArchiveProgressType progressType)
        {
            ArchiveProgressEventArgs e = new ArchiveProgressEventArgs(
                progressType,
                this.currentFileName,
                this.currentFileNumber >= 0 ? this.currentFileNumber : 0,
                this.totalFiles,
                this.currentFileBytesProcessed,
                this.currentFileTotalBytes,
                this.currentArchiveName,
                this.currentArchiveNumber,
                this.totalArchives,
                this.currentArchiveBytesProcessed,
                this.currentArchiveTotalBytes,
                this.fileBytesProcessed,
                this.totalFileBytes);
            this.OnProgress(e);
        }
    }
}
