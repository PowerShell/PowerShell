//---------------------------------------------------------------------
// <copyright file="ArchiveProgressEventArgs.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression
{
    using System;

    /// <summary>
    /// Contains the data reported in an archive progress event.
    /// </summary>
    public class ArchiveProgressEventArgs : EventArgs
    {
        private ArchiveProgressType progressType;

        private string currentFileName;
        private int currentFileNumber;
        private int totalFiles;
        private long currentFileBytesProcessed;
        private long currentFileTotalBytes;

        private string currentArchiveName;
        private short currentArchiveNumber;
        private short totalArchives;
        private long currentArchiveBytesProcessed;
        private long currentArchiveTotalBytes;

        private long fileBytesProcessed;
        private long totalFileBytes;

        /// <summary>
        /// Creates a new ArchiveProgressEventArgs object from specified event parameters.
        /// </summary>
        /// <param name="progressType">type of status message</param>
        /// <param name="currentFileName">name of the file being processed</param>
        /// <param name="currentFileNumber">number of the current file being processed</param>
        /// <param name="totalFiles">total number of files to be processed</param>
        /// <param name="currentFileBytesProcessed">number of bytes processed so far when compressing or extracting a file</param>
        /// <param name="currentFileTotalBytes">total number of bytes in the current file</param>
        /// <param name="currentArchiveName">name of the current Archive</param>
        /// <param name="currentArchiveNumber">current Archive number, when processing a chained set of Archives</param>
        /// <param name="totalArchives">total number of Archives in a chained set</param>
        /// <param name="currentArchiveBytesProcessed">number of compressed bytes processed so far during an extraction</param>
        /// <param name="currentArchiveTotalBytes">total number of compressed bytes to be processed during an extraction</param>
        /// <param name="fileBytesProcessed">number of uncompressed file bytes processed so far</param>
        /// <param name="totalFileBytes">total number of uncompressed file bytes to be processed</param>
        public ArchiveProgressEventArgs(
            ArchiveProgressType progressType,
            string currentFileName,
            int currentFileNumber,
            int totalFiles,
            long currentFileBytesProcessed,
            long currentFileTotalBytes,
            string currentArchiveName,
            int currentArchiveNumber,
            int totalArchives,
            long currentArchiveBytesProcessed,
            long currentArchiveTotalBytes,
            long fileBytesProcessed,
            long totalFileBytes)
        {
            this.progressType = progressType;
            this.currentFileName = currentFileName;
            this.currentFileNumber = currentFileNumber;
            this.totalFiles = totalFiles;
            this.currentFileBytesProcessed = currentFileBytesProcessed;
            this.currentFileTotalBytes = currentFileTotalBytes;
            this.currentArchiveName = currentArchiveName;
            this.currentArchiveNumber = (short) currentArchiveNumber;
            this.totalArchives = (short) totalArchives;
            this.currentArchiveBytesProcessed = currentArchiveBytesProcessed;
            this.currentArchiveTotalBytes = currentArchiveTotalBytes;
            this.fileBytesProcessed = fileBytesProcessed;
            this.totalFileBytes = totalFileBytes;
        }

        /// <summary>
        /// Gets the type of status message.
        /// </summary>
        /// <value>A <see cref="ArchiveProgressType"/> value indicating what type of progress event occurred.</value>
        /// <remarks>
        /// The handler may choose to ignore some types of progress events.
        /// For example, if the handler will only list each file as it is
        /// compressed/extracted, it can ignore events that
        /// are not of type <see cref="ArchiveProgressType.FinishFile"/>.
        /// </remarks>
        public ArchiveProgressType ProgressType
        {
            get
            {
                return this.progressType;
            }
        }

        /// <summary>
        /// Gets the name of the file being processed. (The name of the file within the Archive; not the external
        /// file path.) Also includes the internal path of the file, if any.  Valid for
        /// <see cref="ArchiveProgressType.StartFile"/>, <see cref="ArchiveProgressType.PartialFile"/>,
        /// and <see cref="ArchiveProgressType.FinishFile"/> messages.
        /// </summary>
        /// <value>The name of the file currently being processed, or null if processing
        /// is currently at the stream or archive level.</value>
        public string CurrentFileName
        {
            get
            {
                return this.currentFileName;
            }
        }

        /// <summary>
        /// Gets the number of the current file being processed. The first file is number 0, and the last file
        /// is <see cref="TotalFiles"/>-1. Valid for <see cref="ArchiveProgressType.StartFile"/>,
        /// <see cref="ArchiveProgressType.PartialFile"/>, and <see cref="ArchiveProgressType.FinishFile"/> messages.
        /// </summary>
        /// <value>The number of the file currently being processed, or the most recent
        /// file processed if processing is currently at the stream or archive level.</value>
        public int CurrentFileNumber
        {
            get
            {
                return this.currentFileNumber;
            }
        }

        /// <summary>
        /// Gets the total number of files to be processed.  Valid for all message types.
        /// </summary>
        /// <value>The total number of files to be processed that are known so far.</value>
        public int TotalFiles
        {
            get
            {
                return this.totalFiles;
            }
        }

        /// <summary>
        /// Gets the number of bytes processed so far when compressing or extracting a file.  Valid for
        /// <see cref="ArchiveProgressType.StartFile"/>, <see cref="ArchiveProgressType.PartialFile"/>,
        /// and <see cref="ArchiveProgressType.FinishFile"/> messages.
        /// </summary>
        /// <value>The number of uncompressed bytes processed so far for the current file,
        /// or 0 if processing is currently at the stream or archive level.</value>
        public long CurrentFileBytesProcessed
        {
            get
            {
                return this.currentFileBytesProcessed;
            }
        }

        /// <summary>
        /// Gets the total number of bytes in the current file.  Valid for <see cref="ArchiveProgressType.StartFile"/>,
        /// <see cref="ArchiveProgressType.PartialFile"/>, and <see cref="ArchiveProgressType.FinishFile"/> messages.
        /// </summary>
        /// <value>The uncompressed size of the current file being processed,
        /// or 0 if processing is currently at the stream or archive level.</value>
        public long CurrentFileTotalBytes
        {
            get
            {
                return this.currentFileTotalBytes;
            }
        }

        /// <summary>
        /// Gets the name of the current archive.  Not necessarily the name of the archive on disk.
        /// Valid for all message types.
        /// </summary>
        /// <value>The name of the current archive, or an empty string if no name was specified.</value>
        public string CurrentArchiveName
        {
            get
            {
                return this.currentArchiveName;
            }
        }

        /// <summary>
        /// Gets the current archive number, when processing a chained set of archives. Valid for all message types.
        /// </summary>
        /// <value>The number of the current archive.</value>
        /// <remarks>The first archive is number 0, and the last archive is
        /// <see cref="TotalArchives"/>-1.</remarks>
        public int CurrentArchiveNumber
        {
            get
            {
                return this.currentArchiveNumber;
            }
        }

        /// <summary>
        /// Gets the total number of known archives in a chained set. Valid for all message types.
        /// </summary>
        /// <value>The total number of known archives in a chained set.</value>
        /// <remarks>
        /// When using the compression option to auto-split into multiple archives based on data size,
        /// this value will not be accurate until the end.
        /// </remarks>
        public int TotalArchives
        {
            get
            {
                return this.totalArchives;
            }
        }

        /// <summary>
        /// Gets the number of compressed bytes processed so far during extraction
        /// of the current archive. Valid for all extraction messages.
        /// </summary>
        /// <value>The number of compressed bytes processed so far during extraction
        /// of the current archive.</value>
        public long CurrentArchiveBytesProcessed
        {
            get
            {
                return this.currentArchiveBytesProcessed;
            }
        }

        /// <summary>
        /// Gets the total number of compressed bytes to be processed during extraction
        /// of the current archive. Valid for all extraction messages.
        /// </summary>
        /// <value>The total number of compressed bytes to be processed during extraction
        /// of the current archive.</value>
        public long CurrentArchiveTotalBytes
        {
            get
            {
                return this.currentArchiveTotalBytes;
            }
        }

        /// <summary>
        /// Gets the number of uncompressed bytes processed so far among all files. Valid for all message types.
        /// </summary>
        /// <value>The number of uncompressed file bytes processed so far among all files.</value>
        /// <remarks>
        /// When compared to <see cref="TotalFileBytes"/>, this can be used as a measure of overall progress.
        /// </remarks>
        public long FileBytesProcessed
        {
            get
            {
                return this.fileBytesProcessed;
            }
        }

        /// <summary>
        /// Gets the total number of uncompressed file bytes to be processed.  Valid for all message types.
        /// </summary>
        /// <value>The total number of uncompressed bytes to be processed among all files.</value>
        public long TotalFileBytes
        {
            get
            {
                return this.totalFileBytes;
            }
        }

#if DEBUG

    /// <summary>
    /// Creates a string representation of the progress event.
    /// </summary>
    /// <returns>a listing of all event parameters and values</returns>
    public override string ToString()
    {
        string formatString =
            "{0}\n" +
            "\t CurrentFileName              = {1}\n" +
            "\t CurrentFileNumber            = {2}\n" +
            "\t TotalFiles                   = {3}\n" +
            "\t CurrentFileBytesProcessed    = {4}\n" +
            "\t CurrentFileTotalBytes        = {5}\n" +
            "\t CurrentArchiveName           = {6}\n" +
            "\t CurrentArchiveNumber         = {7}\n" +
            "\t TotalArchives                = {8}\n" +
            "\t CurrentArchiveBytesProcessed = {9}\n" +
            "\t CurrentArchiveTotalBytes     = {10}\n" +
            "\t FileBytesProcessed           = {11}\n" +
            "\t TotalFileBytes               = {12}\n";
        return String.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            formatString,
            this.ProgressType,
            this.CurrentFileName,
            this.CurrentFileNumber,
            this.TotalFiles,
            this.CurrentFileBytesProcessed,
            this.CurrentFileTotalBytes,
            this.CurrentArchiveName,
            this.CurrentArchiveNumber,
            this.TotalArchives,
            this.CurrentArchiveBytesProcessed,
            this.CurrentArchiveTotalBytes,
            this.FileBytesProcessed,
            this.TotalFileBytes);
    }

#endif
    }
}
