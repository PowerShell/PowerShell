//---------------------------------------------------------------------
// <copyright file="ZipInfo.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// Object representing a zip file on disk; provides access to
    /// file-based operations on the zip file.
    /// </summary>
    /// <remarks>
    /// Generally, the methods on this class are much easier to use than the
    /// stream-based interfaces provided by the <see cref="ZipEngine"/> class.
    /// </remarks>
    public class ZipInfo : ArchiveInfo
    {
        /// <summary>
        /// Creates a new CabinetInfo object representing a zip file in a specified path.
        /// </summary>
        /// <param name="path">The path to the zip file. When creating a zip file, this file does not
        /// necessarily exist yet.</param>
        public ZipInfo(string path)
            : base(path)
        {
        }

        /// <summary>
        /// Creates a compression engine that does the low-level work for
        /// this object.
        /// </summary>
        /// <returns>A new <see cref="ZipEngine"/> instance.</returns>
        /// <remarks>
        /// Each instance will be <see cref="CompressionEngine.Dispose()"/>d
        /// immediately after use.
        /// </remarks>
        protected override CompressionEngine CreateCompressionEngine()
        {
            return new ZipEngine();
        }

        /// <summary>
        /// Gets information about the files contained in the archive.
        /// </summary>
        /// <returns>A list of <see cref="ZipFileInfo"/> objects, each
        /// containing information about a file in the archive.</returns>
        public new IList<ZipFileInfo> GetFiles()
        {
            IList<ArchiveFileInfo> files = base.GetFiles();
            List<ZipFileInfo> zipFiles = new List<ZipFileInfo>(files.Count);
            foreach (ZipFileInfo zipFile in files) zipFiles.Add(zipFile);
            return zipFiles.AsReadOnly();
        }

        /// <summary>
        /// Gets information about the certain files contained in the archive file.
        /// </summary>
        /// <param name="searchPattern">The search string, such as
        /// &quot;*.txt&quot;.</param>
        /// <returns>A list of <see cref="ZipFileInfo"/> objects, each containing
        /// information about a file in the archive.</returns>
        public new IList<ZipFileInfo> GetFiles(string searchPattern)
        {
            IList<ArchiveFileInfo> files = base.GetFiles(searchPattern);
            List<ZipFileInfo> zipFiles = new List<ZipFileInfo>(files.Count);
            foreach (ZipFileInfo zipFile in files) zipFiles.Add(zipFile);
            return zipFiles.AsReadOnly();
        }
    }
}
