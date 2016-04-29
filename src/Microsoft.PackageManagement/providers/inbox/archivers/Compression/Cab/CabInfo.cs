//---------------------------------------------------------------------
// <copyright file="CabInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Cab
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Object representing a cabinet file on disk; provides access to
    /// file-based operations on the cabinet file.
    /// </summary>
    /// <remarks>
    /// Generally, the methods on this class are much easier to use than the
    /// stream-based interfaces provided by the <see cref="CabEngine"/> class.
    /// </remarks>
    public class CabInfo : ArchiveInfo
    {
        /// <summary>
        /// Creates a new CabinetInfo object representing a cabinet file in a specified path.
        /// </summary>
        /// <param name="path">The path to the cabinet file. When creating a cabinet file, this file does not
        /// necessarily exist yet.</param>
        public CabInfo(string path)
            : base(path)
        {
        }

        /// <summary>
        /// Creates a compression engine that does the low-level work for
        /// this object.
        /// </summary>
        /// <returns>A new <see cref="CabEngine"/> instance.</returns>
        /// <remarks>
        /// Each instance will be <see cref="CompressionEngine.Dispose()"/>d
        /// immediately after use.
        /// </remarks>
        protected override CompressionEngine CreateCompressionEngine()
        {
            return new CabEngine();
        }

        /// <summary>
        /// Gets information about the files contained in the archive.
        /// </summary>
        /// <returns>A list of <see cref="CabFileInfo"/> objects, each
        /// containing information about a file in the archive.</returns>
        public new IList<CabFileInfo> GetFiles()
        {
            IList<ArchiveFileInfo> files = base.GetFiles();
            List<CabFileInfo> cabFiles = new List<CabFileInfo>(files.Count);
            foreach (CabFileInfo cabFile in files) cabFiles.Add(cabFile);
            return cabFiles.AsReadOnly();
        }

        /// <summary>
        /// Gets information about the certain files contained in the archive file.
        /// </summary>
        /// <param name="searchPattern">The search string, such as
        /// &quot;*.txt&quot;.</param>
        /// <returns>A list of <see cref="CabFileInfo"/> objects, each containing
        /// information about a file in the archive.</returns>
        public new IList<CabFileInfo> GetFiles(string searchPattern)
        {
            IList<ArchiveFileInfo> files = base.GetFiles(searchPattern);
            List<CabFileInfo> cabFiles = new List<CabFileInfo>(files.Count);
            foreach (CabFileInfo cabFile in files) cabFiles.Add(cabFile);
            return cabFiles.AsReadOnly();
        }
    }
}
