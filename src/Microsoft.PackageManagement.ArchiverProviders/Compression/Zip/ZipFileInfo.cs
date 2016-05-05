//---------------------------------------------------------------------
// <copyright file="ZipFileInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Zip
{
    using System;
    using System.IO;

    /// <summary>
    /// Object representing a compressed file within a zip package; provides operations for getting
    /// the file properties and extracting the file.
    /// </summary>
    public class ZipFileInfo : ArchiveFileInfo
    {
        private long compressedLength;
        private ZipCompressionMethod compressionMethod;

        /// <summary>
        /// Creates a new ZipFileInfo object representing a file within a zip in a specified path.
        /// </summary>
        /// <param name="zipInfo">An object representing the zip archive containing the file.</param>
        /// <param name="filePath">The path to the file within the zip archive. Usually, this is a simple file
        /// name, but if the zip archive contains a directory structure this may include the directory.</param>
        public ZipFileInfo(ZipInfo zipInfo, string filePath)
            : base(zipInfo, filePath)
        {
            if (zipInfo == null)
            {
                throw new ArgumentNullException("zipInfo");
            }
        }

        /// <summary>
        /// Creates a new ZipFileInfo object with all parameters specified,
        /// used internally when reading the metadata out of a zip archive.
        /// </summary>
        /// <param name="filePath">The internal path and name of the file in the zip archive.</param>
        /// <param name="zipNumber">The zip archive number where the file starts.</param>
        /// <param name="attributes">The stored attributes of the file.</param>
        /// <param name="lastWriteTime">The stored last write time of the file.</param>
        /// <param name="length">The uncompressed size of the file.</param>
        /// <param name="compressedLength">The compressed size of the file.</param>
        /// <param name="compressionMethod">Compression algorithm used for this file.</param>
        internal ZipFileInfo(
            string filePath,
            int zipNumber,
            FileAttributes attributes,
            DateTime lastWriteTime,
            long length,
            long compressedLength,
            ZipCompressionMethod compressionMethod)
            : base(filePath, zipNumber, attributes, lastWriteTime, length)
        {
            this.compressedLength = compressedLength;
            this.compressionMethod = compressionMethod;
        }

        /// <summary>
        /// Gets the compressed size of the file in bytes.
        /// </summary>
        public long CompressedLength
        {
            get
            {
                return this.compressedLength;
            }
        }

        /// <summary>
        /// Gets the method used to compress this file.
        /// </summary>
        public ZipCompressionMethod CompressionMethod
        {
            get
            {
                return this.compressionMethod;
            }
        }

    }
}
