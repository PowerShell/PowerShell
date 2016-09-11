//---------------------------------------------------------------------
// <copyright file="CompressionLevel.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    /// <summary>
    /// Specifies the compression level ranging from minimum compression to
    /// maximum compression, or no compression at all.
    /// </summary>
    /// <remarks>
    /// Although only four values are enumerated, any integral value between
    /// <see cref="CompressionLevel.Min"/> and <see cref="CompressionLevel.Max"/> can also be used.
    /// </remarks>
    public enum CompressionLevel
    {
        /// <summary>Do not compress files, only store.</summary>
        None = 0,

        /// <summary>Minimum compression; fastest.</summary>
        Min = 1,

        /// <summary>A compromise between speed and compression efficiency.</summary>
        Normal = 6,

        /// <summary>Maximum compression; slowest.</summary>
        Max = 10
    }
}
