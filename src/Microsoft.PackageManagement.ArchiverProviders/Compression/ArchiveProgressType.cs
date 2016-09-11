//---------------------------------------------------------------------
// <copyright file="ArchiveProgressType.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    /// <summary>
    /// The type of progress event.
    /// </summary>
    /// <remarks>
    /// <p>PACKING EXAMPLE: The following sequence of events might be received when
    /// extracting a simple archive file with 2 files.</p>
    /// <list type="table">
    /// <listheader><term>Message Type</term><description>Description</description></listheader>
    /// <item><term>StartArchive</term> <description>Begin extracting archive</description></item>
    /// <item><term>StartFile</term>    <description>Begin extracting first file</description></item>
    /// <item><term>PartialFile</term>  <description>Extracting first file</description></item>
    /// <item><term>PartialFile</term>  <description>Extracting first file</description></item>
    /// <item><term>FinishFile</term>   <description>Finished extracting first file</description></item>
    /// <item><term>StartFile</term>    <description>Begin extracting second file</description></item>
    /// <item><term>PartialFile</term>  <description>Extracting second file</description></item>
    /// <item><term>FinishFile</term>   <description>Finished extracting second file</description></item>
    /// <item><term>FinishArchive</term><description>Finished extracting archive</description></item>
    /// </list>
    /// <p></p>
    /// <p>UNPACKING EXAMPLE:  Packing 3 files into 2 archive chunks, where the second file is
    ///	continued to the second archive chunk.</p>
    /// <list type="table">
    /// <listheader><term>Message Type</term><description>Description</description></listheader>
    /// <item><term>StartFile</term>     <description>Begin compressing first file</description></item>
    /// <item><term>FinishFile</term>    <description>Finished compressing first file</description></item>
    /// <item><term>StartFile</term>     <description>Begin compressing second file</description></item>
    /// <item><term>PartialFile</term>   <description>Compressing second file</description></item>
    /// <item><term>PartialFile</term>   <description>Compressing second file</description></item>
    /// <item><term>FinishFile</term>    <description>Finished compressing second file</description></item>
    /// <item><term>StartArchive</term>  <description>Begin writing first archive</description></item>
    /// <item><term>PartialArchive</term><description>Writing first archive</description></item>
    /// <item><term>FinishArchive</term> <description>Finished writing first archive</description></item>
    /// <item><term>StartFile</term>     <description>Begin compressing third file</description></item>
    /// <item><term>PartialFile</term>   <description>Compressing third file</description></item>
    /// <item><term>FinishFile</term>    <description>Finished compressing third file</description></item>
    /// <item><term>StartArchive</term>  <description>Begin writing second archive</description></item>
    /// <item><term>PartialArchive</term><description>Writing second archive</description></item>
    /// <item><term>FinishArchive</term> <description>Finished writing second archive</description></item>
    /// </list>
    /// </remarks>
    public enum ArchiveProgressType : int
    {
        /// <summary>Status message before beginning the packing or unpacking an individual file.</summary>
        StartFile,

        /// <summary>Status message (possibly reported multiple times) during the process of packing or unpacking a file.</summary>
        PartialFile,

        /// <summary>Status message after completion of the packing or unpacking an individual file.</summary>
        FinishFile,

        /// <summary>Status message before beginning the packing or unpacking an archive.</summary>
        StartArchive,

        /// <summary>Status message (possibly reported multiple times) during the process of packing or unpacking an archive.</summary>
        PartialArchive,

        /// <summary>Status message after completion of the packing or unpacking of an archive.</summary>
        FinishArchive,
    }
}
