//---------------------------------------------------------------------
// <copyright file="ZipUnpacker.cs" company="Microsoft Corporation">
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
    using System.IO;

    public partial class ZipEngine
    {
        /// <summary>
        /// Extracts files from a zip archive or archive chain.
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
        public override void Unpack(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter)
        {
            if (streamContext == null)
            {
                throw new ArgumentNullException("streamContext");
            }

            lock (this)
            {
                IList<ZipFileHeader> allHeaders = this.GetCentralDirectory(streamContext);
                if (allHeaders == null)
                {
                    throw new ZipException("Zip central directory not found.");
                }

                IList<ZipFileHeader> headers = new List<ZipFileHeader>(allHeaders.Count);
                foreach (ZipFileHeader header in allHeaders)
                {
                    if (!header.IsDirectory &&
                        (fileFilter == null || fileFilter(header.fileName)))
                    {
                        headers.Add(header);
                    }
                }

                this.ResetProgressData();

                // Count the total number of files and bytes to be compressed.
                this.totalFiles = headers.Count;
                foreach (ZipFileHeader header in headers)
                {
                    long compressedSize;
                    long uncompressedSize;
                    long localHeaderOffset;
                    int archiveNumber;
                    uint crc;
                    header.GetZip64Fields(
                        out compressedSize,
                        out uncompressedSize,
                        out localHeaderOffset,
                        out archiveNumber,
                        out crc);

                    this.totalFileBytes += uncompressedSize;
                    if (archiveNumber >= this.totalArchives)
                    {
                        this.totalArchives = (short)(archiveNumber + 1);
                    }
                }

                this.currentArchiveNumber = -1;
                this.currentFileNumber = -1;
                Stream archiveStream = null;
                try
                {
                    foreach (ZipFileHeader header in headers)
                    {
                        this.currentFileNumber++;
                        this.UnpackOneFile(streamContext, header, ref archiveStream);
                    }
                }
                finally
                {
                    if (archiveStream != null)
                    {
                        streamContext.CloseArchiveReadStream(
                            0, String.Empty, archiveStream);
                        this.currentArchiveNumber--;
                        this.OnProgress(ArchiveProgressType.FinishArchive);
                    }
                }
            }
        }

        /// <summary>
        /// Unpacks a single file from an archive or archive chain.
        /// </summary>
        private void UnpackOneFile(
            IUnpackStreamContext streamContext,
            ZipFileHeader header,
            ref Stream archiveStream)
        {
            ZipFileInfo fileInfo = null;
            Stream fileStream = null;
            try
            {
                Func<Stream, Stream> compressionStreamCreator;
                if (!ZipEngine.decompressionStreamCreators.TryGetValue(
                    header.compressionMethod, out compressionStreamCreator))
                {
                    // Silently skip files of an unsupported compression method.
                    return;
                }

                long compressedSize;
                long uncompressedSize;
                long localHeaderOffset;
                int archiveNumber;
                uint crc;
                header.GetZip64Fields(
                    out compressedSize,
                    out uncompressedSize,
                    out localHeaderOffset,
                    out archiveNumber,
                    out crc);

                if (this.currentArchiveNumber != archiveNumber + 1)
                {
                    if (archiveStream != null)
                    {
                        streamContext.CloseArchiveReadStream(
                            this.currentArchiveNumber,
                            String.Empty,
                            archiveStream);
                        archiveStream = null;

                        this.OnProgress(ArchiveProgressType.FinishArchive);
                        this.currentArchiveName = null;
                    }

                    this.currentArchiveNumber = (short)(archiveNumber + 1);
                    this.currentArchiveBytesProcessed = 0;
                    this.currentArchiveTotalBytes = 0;

                    archiveStream = this.OpenArchive(
                        streamContext, this.currentArchiveNumber);

                    FileStream archiveFileStream = archiveStream as FileStream;
                    this.currentArchiveName = (archiveFileStream != null ?
                        Path.GetFileName(archiveFileStream.Name) : null);

                    this.currentArchiveTotalBytes = archiveStream.Length;
                    this.currentArchiveNumber--;
                    this.OnProgress(ArchiveProgressType.StartArchive);
                    this.currentArchiveNumber++;
                }

                archiveStream.Seek(localHeaderOffset, SeekOrigin.Begin);

                ZipFileHeader localHeader = new ZipFileHeader();
                if (!localHeader.Read(archiveStream, false) ||
                    !ZipEngine.AreFilePathsEqual(localHeader.fileName, header.fileName))
                {
                    string msg = "Could not read file: " + header.fileName;
                    throw new ZipException(msg);
                }

                fileInfo = header.ToZipFileInfo();

                fileStream = streamContext.OpenFileWriteStream(
                    fileInfo.FullName,
                    fileInfo.Length,
                    fileInfo.LastWriteTime);

                if (fileStream != null)
                {
                    this.currentFileName = header.fileName;
                    this.currentFileBytesProcessed = 0;
                    this.currentFileTotalBytes = fileInfo.Length;
                    this.currentArchiveNumber--;
                    this.OnProgress(ArchiveProgressType.StartFile);
                    this.currentArchiveNumber++;

                    this.UnpackFileBytes(
                        streamContext,
                        fileInfo.FullName,
                        fileInfo.CompressedLength,
                        fileInfo.Length,
                        header.crc32,
                        fileStream,
                        compressionStreamCreator,
                        ref archiveStream);
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    streamContext.CloseFileWriteStream(
                        fileInfo.FullName,
                        fileStream,
                        fileInfo.Attributes,
                        fileInfo.LastWriteTime);

                    this.currentArchiveNumber--;
                    this.OnProgress(ArchiveProgressType.FinishFile);
                    this.currentArchiveNumber++;
                }
            }
        }

        /// <summary>
        /// Compares two internal file paths while ignoring case and slash differences.
        /// </summary>
        /// <param name="path1">The first path to compare.</param>
        /// <param name="path2">The second path to compare.</param>
        /// <returns>True if the paths are equivalent.</returns>
        private static bool AreFilePathsEqual(string path1, string path2)
        {
            path1 = path1.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            path2 = path2.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return String.Compare(path1, path2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        private Stream OpenArchive(IUnpackStreamContext streamContext, int archiveNumber)
        {
            Stream archiveStream = streamContext.OpenArchiveReadStream(
                archiveNumber, String.Empty, this);
            if (archiveStream == null && archiveNumber != 0)
            {
                archiveStream = streamContext.OpenArchiveReadStream(
                    0, String.Empty, this);
            }

            if (archiveStream == null)
            {
                throw new FileNotFoundException("Archive stream not provided.");
            }

            return archiveStream;
        }

        /// <summary>
        /// Decompresses bytes for one file from an archive or archive chain,
        /// checking the crc at the end.
        /// </summary>
        private void UnpackFileBytes(
            IUnpackStreamContext streamContext,
            string fileName,
            long compressedSize,
            long uncompressedSize,
            uint crc,
            Stream fileStream,
            Func<Stream, Stream> compressionStreamCreator,
            ref Stream archiveStream)
        {
            CrcStream crcStream = new CrcStream(fileStream);

            ConcatStream concatStream = new ConcatStream(
                delegate(ConcatStream s)
                {
                    this.currentArchiveBytesProcessed = s.Source.Position;
                    streamContext.CloseArchiveReadStream(
                        this.currentArchiveNumber,
                        String.Empty,
                        s.Source);

                    this.currentArchiveNumber--;
                    this.OnProgress(ArchiveProgressType.FinishArchive);
                    this.currentArchiveNumber += 2;
                    this.currentArchiveName = null;
                    this.currentArchiveBytesProcessed = 0;
                    this.currentArchiveTotalBytes = 0;

                    s.Source = this.OpenArchive(streamContext, this.currentArchiveNumber);

                    FileStream archiveFileStream = s.Source as FileStream;
                    this.currentArchiveName = (archiveFileStream != null ?
                        Path.GetFileName(archiveFileStream.Name) : null);

                    this.currentArchiveTotalBytes = s.Source.Length;
                    this.currentArchiveNumber--;
                    this.OnProgress(ArchiveProgressType.StartArchive);
                    this.currentArchiveNumber++;
                });

            concatStream.Source = archiveStream;
            concatStream.SetLength(compressedSize);

            Stream decompressionStream = compressionStreamCreator(concatStream);

            try
            {
                byte[] buf = new byte[4096];
                long bytesRemaining = uncompressedSize;
                int counter = 0;
                while (bytesRemaining > 0)
                {
                    int count = (int)Math.Min(buf.Length, bytesRemaining);
                    count = decompressionStream.Read(buf, 0, count);
                    crcStream.Write(buf, 0, count);
                    bytesRemaining -= count;

                    this.fileBytesProcessed += count;
                    this.currentFileBytesProcessed += count;
                    this.currentArchiveBytesProcessed = concatStream.Source.Position;

                    if (++counter % 16 == 0) // Report every 64K
                    {
                        this.currentArchiveNumber--;
                        this.OnProgress(ArchiveProgressType.PartialFile);
                        this.currentArchiveNumber++;
                    }
                }
            }
            finally
            {
                archiveStream = concatStream.Source;
            }

            crcStream.Flush();

            if (crcStream.Crc != crc)
            {
                throw new ZipException("CRC check failed for file: " + fileName);
            }
        }
    }
}

