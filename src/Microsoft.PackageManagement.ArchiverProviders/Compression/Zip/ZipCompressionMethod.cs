//---------------------------------------------------------------------
// <copyright file="ZipCompressionMethod.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Zip
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Identifies the compression method or &quot;algorithm&quot;
    /// used for a single file within a zip archive.
    /// </summary>
    /// <remarks>
    /// Proprietary zip implementations may define additional compression
    /// methods outside of those included here.
    /// </remarks>
    public enum ZipCompressionMethod
    {
        /// <summary>
        /// The file is stored (no compression)
        /// </summary>
        Store = 0,

        /// <summary>
        /// The file is Shrunk
        /// </summary>
        Shrink = 1,

        /// <summary>
        /// The file is Reduced with compression factor 1
        /// </summary>
        Reduce1 = 2,

        /// <summary>
        /// The file is Reduced with compression factor 2
        /// </summary>
        Reduce2 = 3,

        /// <summary>
        /// The file is Reduced with compression factor 3
        /// </summary>
        Reduce3 = 4,

        /// <summary>
        /// The file is Reduced with compression factor 4
        /// </summary>
        Reduce4 = 5,

        /// <summary>
        /// The file is Imploded
        /// </summary>
        Implode = 6,

        /// <summary>
        /// The file is Deflated;
        /// the most common and widely-compatible form of zip compression.
        /// </summary>
        Deflate = 8,

        /// <summary>
        /// The file is Deflated using the enhanced Deflate64 method.
        /// </summary>
        Deflate64 = 9,

        /// <summary>
        /// The file is compressed using the BZIP2 algorithm.
        /// </summary>
        BZip2 = 12,

        /// <summary>
        /// The file is compressed using the LZMA algorithm.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lzma")]
        Lzma = 14,

        /// <summary>
        /// The file is compressed using the PPMd algorithm.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ppmd")]
        Ppmd = 98
    }
}
