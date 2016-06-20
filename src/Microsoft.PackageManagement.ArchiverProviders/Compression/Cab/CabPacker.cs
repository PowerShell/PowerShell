//---------------------------------------------------------------------
// <copyright file="CabPacker.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
#if !CORECLR
    using System.Security.Permissions;
#endif
    using System.Text;

    internal class CabPacker : CabWorker
    {
        private const string TempStreamName = "%%TEMP%%";

        private NativeMethods.FCI.Handle fciHandle;

        // These delegates need to be saved as member variables
        // so that they don't get GC'd.
        private NativeMethods.FCI.PFNALLOC fciAllocMemHandler;
        private NativeMethods.FCI.PFNFREE fciFreeMemHandler;
        private NativeMethods.FCI.PFNOPEN fciOpenStreamHandler;
        private NativeMethods.FCI.PFNREAD fciReadStreamHandler;
        private NativeMethods.FCI.PFNWRITE fciWriteStreamHandler;
        private NativeMethods.FCI.PFNCLOSE fciCloseStreamHandler;
        private NativeMethods.FCI.PFNSEEK fciSeekStreamHandler;
        private NativeMethods.FCI.PFNFILEPLACED fciFilePlacedHandler;
        private NativeMethods.FCI.PFNDELETE fciDeleteFileHandler;
        private NativeMethods.FCI.PFNGETTEMPFILE fciGetTempFileHandler;

        private NativeMethods.FCI.PFNGETNEXTCABINET fciGetNextCabinet;
        private NativeMethods.FCI.PFNSTATUS fciCreateStatus;
        private NativeMethods.FCI.PFNGETOPENINFO fciGetOpenInfo;

        private IPackStreamContext context;

        private FileAttributes fileAttributes;
        private DateTime fileLastWriteTime;

        private int maxCabBytes;

        private long totalFolderBytesProcessedInCurrentCab;

        private CompressionLevel compressionLevel;
        private bool dontUseTempFiles;
        private IList<Stream> tempStreams;

        public CabPacker(CabEngine cabEngine)
            : base(cabEngine)
        {
            this.fciAllocMemHandler = this.CabAllocMem;
            this.fciFreeMemHandler = this.CabFreeMem;
            this.fciOpenStreamHandler = this.CabOpenStreamEx;
            this.fciReadStreamHandler = this.CabReadStreamEx;
            this.fciWriteStreamHandler = this.CabWriteStreamEx;
            this.fciCloseStreamHandler = this.CabCloseStreamEx;
            this.fciSeekStreamHandler = this.CabSeekStreamEx;
            this.fciFilePlacedHandler = this.CabFilePlaced;
            this.fciDeleteFileHandler = this.CabDeleteFile;
            this.fciGetTempFileHandler = this.CabGetTempFile;
            this.fciGetNextCabinet = this.CabGetNextCabinet;
            this.fciCreateStatus = this.CabCreateStatus;
            this.fciGetOpenInfo = this.CabGetOpenInfo;
            this.tempStreams = new List<Stream>();
            this.compressionLevel = CompressionLevel.Normal;
        }

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

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void CreateFci(long maxArchiveSize)
        {
            NativeMethods.FCI.CCAB ccab = new NativeMethods.FCI.CCAB();
            if (maxArchiveSize > 0 && maxArchiveSize < ccab.cb)
            {
                ccab.cb = Math.Max(
                    NativeMethods.FCI.MIN_DISK, (int)maxArchiveSize);
            }

            object maxFolderSizeOption = this.context.GetOption(
                "maxFolderSize", null);
            if (maxFolderSizeOption != null)
            {
                long maxFolderSize = Convert.ToInt64(
                    maxFolderSizeOption, CultureInfo.InvariantCulture);
                if (maxFolderSize > 0 && maxFolderSize < ccab.cbFolderThresh)
                {
                    ccab.cbFolderThresh = (int)maxFolderSize;
                }
            }

            this.maxCabBytes = ccab.cb;
            ccab.szCab = this.context.GetArchiveName(0);
            if (ccab.szCab == null)
            {
                throw new FileNotFoundException(
                    "Cabinet name not provided by stream context.");
            }
            ccab.setID = (short)new Random().Next(
                Int16.MinValue, Int16.MaxValue + 1);
            this.CabNumbers[ccab.szCab] = 0;
            this.currentArchiveName = ccab.szCab;
            this.totalArchives = 1;
            this.CabStream = null;

            this.Erf.Clear();
            this.fciHandle = NativeMethods.FCI.Create(
                this.ErfHandle.AddrOfPinnedObject(),
                this.fciFilePlacedHandler,
                this.fciAllocMemHandler,
                this.fciFreeMemHandler,
                this.fciOpenStreamHandler,
                this.fciReadStreamHandler,
                this.fciWriteStreamHandler,
                this.fciCloseStreamHandler,
                this.fciSeekStreamHandler,
                this.fciDeleteFileHandler,
                this.fciGetTempFileHandler,
                ccab,
                IntPtr.Zero);
            this.CheckError(false);
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
#if !CORECLR
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
#endif
        public void Pack(
            IPackStreamContext streamContext,
            IEnumerable<string> files,
            long maxArchiveSize)
        {
            if (streamContext == null)
            {
                throw new ArgumentNullException("streamContext");
            }

            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            lock (this)
            {
                try
                {
                    this.context = streamContext;

                    this.ResetProgressData();

                    this.CreateFci(maxArchiveSize);

                    foreach (string file in files)
                    {
                        FileAttributes attributes;
                        DateTime lastWriteTime;
                        Stream fileStream = this.context.OpenFileReadStream(
                            file,
                            out attributes,
                            out lastWriteTime);
                        if (fileStream != null)
                        {
                            this.totalFileBytes += fileStream.Length;
                            this.totalFiles++;
                            this.context.CloseFileReadStream(file, fileStream);
                        }
                    }

                    long uncompressedBytesInFolder = 0;
                    this.currentFileNumber = -1;

                    foreach (string file in files)
                    {
                        FileAttributes attributes;
                        DateTime lastWriteTime;
                        Stream fileStream = this.context.OpenFileReadStream(
                            file, out attributes, out lastWriteTime);
                        if (fileStream == null)
                        {
                            continue;
                        }

                        if (fileStream.Length >= (long)NativeMethods.FCI.MAX_FOLDER)
                        {
                            throw new NotSupportedException(String.Format(
                                CultureInfo.InvariantCulture,
                                "File {0} exceeds maximum file size " +
                                "for cabinet format.",
                                file));
                        }

                        if (uncompressedBytesInFolder > 0)
                        {
                            // Automatically create a new folder if this file
                            // won't fit in the current folder.
                            bool nextFolder = uncompressedBytesInFolder
                                + fileStream.Length >= (long)NativeMethods.FCI.MAX_FOLDER;

                            // Otherwise ask the client if it wants to
                            // move to the next folder.
                            if (!nextFolder)
                            {
                                object nextFolderOption = streamContext.GetOption(
                                    "nextFolder",
                                    new object[] { file, this.currentFolderNumber });
                                nextFolder = Convert.ToBoolean(
                                    nextFolderOption, CultureInfo.InvariantCulture);
                            }

                            if (nextFolder)
                            {
                                this.FlushFolder();
                                uncompressedBytesInFolder = 0;
                            }
                        }

                        if (this.currentFolderTotalBytes > 0)
                        {
                            this.currentFolderTotalBytes = 0;
                            this.currentFolderNumber++;
                            uncompressedBytesInFolder = 0;
                        }

                        this.currentFileName = file;
                        this.currentFileNumber++;

                        this.currentFileTotalBytes = fileStream.Length;
                        this.currentFileBytesProcessed = 0;
                        this.OnProgress(ArchiveProgressType.StartFile);

                        uncompressedBytesInFolder += fileStream.Length;

                        this.AddFile(
                            file,
                            fileStream,
                            attributes,
                            lastWriteTime,
                            false,
                            this.CompressionLevel);
                    }

                    this.FlushFolder();
                    this.FlushCabinet();
                }
                finally
                {
                    if (this.CabStream != null)
                    {
                        this.context.CloseArchiveWriteStream(
                            this.currentArchiveNumber,
                            this.currentArchiveName,
                            this.CabStream);
                        this.CabStream = null;
                    }

                    if (this.FileStream != null)
                    {
                        this.context.CloseFileReadStream(
                            this.currentFileName, this.FileStream);
                        this.FileStream = null;
                    }
                    this.context = null;

                    if (this.fciHandle != null)
                    {
                        this.fciHandle.Dispose();
                        this.fciHandle = null;
                    }
                }
            }
        }

        internal override int CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, IntPtr pv)
        {
            if (this.CabNumbers.ContainsKey(path))
            {
                Stream stream = this.CabStream;
                if (stream == null)
                {
                    short cabNumber = this.CabNumbers[path];

                    this.currentFolderTotalBytes = 0;

                    stream = this.context.OpenArchiveWriteStream(cabNumber, path, true, this.CabEngine);
                    if (stream == null)
                    {
                        throw new FileNotFoundException(
                            String.Format(CultureInfo.InvariantCulture, "Cabinet {0} not provided.", cabNumber));
                    }
                    this.currentArchiveName = path;

                    this.currentArchiveTotalBytes = Math.Min(
                        this.totalFolderBytesProcessedInCurrentCab, this.maxCabBytes);
                    this.currentArchiveBytesProcessed = 0;

                    this.OnProgress(ArchiveProgressType.StartArchive);
                    this.CabStream = stream;
                }
                path = CabWorker.CabStreamName;
            }
            else if (path == CabPacker.TempStreamName)
            {
                // Opening memory stream for a temp file.
                Stream stream = new MemoryStream();
                this.tempStreams.Add(stream);
                int streamHandle = this.StreamHandles.AllocHandle(stream);
                err = 0;
                return streamHandle;
            }
            else if (path != CabWorker.CabStreamName)
            {
                // Opening a file on disk for a temp file.
                path = Path.Combine(Path.GetTempPath(), path);
                Stream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                this.tempStreams.Add(stream);
                stream = new DuplicateStream(stream);
                int streamHandle = this.StreamHandles.AllocHandle(stream);
                err = 0;
                return streamHandle;
            }
            return base.CabOpenStreamEx(path, openFlags, shareMode, out err, pv);
        }

        internal override int CabWriteStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv)
        {
            int count = base.CabWriteStreamEx(streamHandle, memory, cb, out err, pv);
            if (count > 0 && err == 0)
            {
                Stream stream = this.StreamHandles[streamHandle];
                if (DuplicateStream.OriginalStream(stream) ==
                    DuplicateStream.OriginalStream(this.CabStream))
                {
                    this.currentArchiveBytesProcessed += cb;
                    if (this.currentArchiveBytesProcessed > this.currentArchiveTotalBytes)
                    {
                        this.currentArchiveBytesProcessed = this.currentArchiveTotalBytes;
                    }
                }
            }
            return count;
        }

        internal override int CabCloseStreamEx(int streamHandle, out int err, IntPtr pv)
        {
            Stream stream = DuplicateStream.OriginalStream(this.StreamHandles[streamHandle]);

            if (stream == DuplicateStream.OriginalStream(this.FileStream))
            {
                this.context.CloseFileReadStream(this.currentFileName, stream);
                this.FileStream = null;
                long remainder = this.currentFileTotalBytes - this.currentFileBytesProcessed;
                this.currentFileBytesProcessed += remainder;
                this.fileBytesProcessed += remainder;
                this.OnProgress(ArchiveProgressType.FinishFile);

                this.currentFileTotalBytes = 0;
                this.currentFileBytesProcessed = 0;
                this.currentFileName = null;
            }
            else if (stream == DuplicateStream.OriginalStream(this.CabStream))
            {
                if (stream.CanWrite)
                {
                    stream.Flush();
                }

                this.currentArchiveBytesProcessed = this.currentArchiveTotalBytes;
                this.OnProgress(ArchiveProgressType.FinishArchive);
                this.currentArchiveNumber++;
                this.totalArchives++;

                this.context.CloseArchiveWriteStream(
                    this.currentArchiveNumber,
                    this.currentArchiveName,
                    stream);

                this.currentArchiveName = this.NextCabinetName;
                this.currentArchiveBytesProcessed = this.currentArchiveTotalBytes = 0;
                this.totalFolderBytesProcessedInCurrentCab = 0;

                this.CabStream = null;
            }
            else  // Must be a temp stream
            {
#if CORECLR
                stream.Dispose();
#else
                stream.Close();
#endif
                this.tempStreams.Remove(stream);
            }
            return base.CabCloseStreamEx(streamHandle, out err, pv);
        }

        /// <summary>
        /// Disposes of resources allocated by the cabinet engine.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly or indirectly by a user's code,
        /// so managed and unmanaged resources will be disposed. If false, the method has been called by the
        /// runtime from inside the finalizer, and only unmanaged resources will be disposed.</param>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (this.fciHandle != null)
                    {
                        this.fciHandle.Dispose();
                        this.fciHandle = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private static NativeMethods.FCI.TCOMP GetCompressionType(CompressionLevel compLevel)
        {
            if (compLevel < CompressionLevel.Min)
            {
                return NativeMethods.FCI.TCOMP.TYPE_NONE;
            }
            else
            {
                if (compLevel > CompressionLevel.Max)
                {
                    compLevel = CompressionLevel.Max;
                }

                int lzxWindowMax =
                    ((int)NativeMethods.FCI.TCOMP.LZX_WINDOW_HI >> (int)NativeMethods.FCI.TCOMP.SHIFT_LZX_WINDOW) -
                    ((int)NativeMethods.FCI.TCOMP.LZX_WINDOW_LO >> (int)NativeMethods.FCI.TCOMP.SHIFT_LZX_WINDOW);
                int lzxWindow = lzxWindowMax *
                    (compLevel - CompressionLevel.Min) / (CompressionLevel.Max - CompressionLevel.Min);

                return (NativeMethods.FCI.TCOMP)((int)NativeMethods.FCI.TCOMP.TYPE_LZX |
                    ((int)NativeMethods.FCI.TCOMP.LZX_WINDOW_LO +
                    (lzxWindow << (int)NativeMethods.FCI.TCOMP.SHIFT_LZX_WINDOW)));
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private void AddFile(
            string name,
            Stream stream,
            FileAttributes attributes,
            DateTime lastWriteTime,
            bool execute,
            CompressionLevel compLevel)
        {
            this.FileStream = stream;
            this.fileAttributes = attributes &
                (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
            this.fileLastWriteTime = lastWriteTime;
            this.currentFileName = name;

            NativeMethods.FCI.TCOMP tcomp = CabPacker.GetCompressionType(compLevel);

            IntPtr namePtr = IntPtr.Zero;
            try
            {
                Encoding nameEncoding = Encoding.ASCII;
                if (Encoding.UTF8.GetByteCount(name) > name.Length)
                {
                    nameEncoding = Encoding.UTF8;
                    this.fileAttributes |= FileAttributes.Normal;  // _A_NAME_IS_UTF
                }

                byte[] nameBytes = nameEncoding.GetBytes(name);
                namePtr = Marshal.AllocHGlobal(nameBytes.Length + 1);
                Marshal.Copy(nameBytes, 0, namePtr, nameBytes.Length);
                Marshal.WriteByte(namePtr, nameBytes.Length, 0);

                this.Erf.Clear();
                var result = NativeMethods.FCI.AddFile(
                    this.fciHandle,
                    String.Empty,
                    namePtr,
                    execute,
                    this.fciGetNextCabinet,
                    this.fciCreateStatus,
                    this.fciGetOpenInfo,
                    tcomp);
                if (result == 0)
                {
                    // Stop compiler from complaining
                    this.CheckError(false);
                    this.FileStream = null;
                    this.currentFileName = null;
                    return;
                }
            }
            finally
            {
                if (namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(namePtr);
                }
            }

            this.CheckError(false);
            this.FileStream = null;
            this.currentFileName = null;
        }

        private void FlushFolder()
        {
            this.Erf.Clear();
            var result = NativeMethods.FCI.FlushFolder(this.fciHandle, this.fciGetNextCabinet, this.fciCreateStatus);
            if (result == 0)
            {
                // Stop compiler from complaining
                this.CheckError(false);
                return;
            }
            this.CheckError(false);
        }

        private void FlushCabinet()
        {
            this.Erf.Clear();
            var result = NativeMethods.FCI.FlushCabinet(this.fciHandle, false, this.fciGetNextCabinet, this.fciCreateStatus);
            if (result == 0)
            {
                // Stop compiler from complaining
                this.CheckError(false);
                return;
            }
            this.CheckError(false);
        }

        private int CabGetOpenInfo(
            string path,
            out short date,
            out short time,
            out short attribs,
            out int err,
            IntPtr pv)
        {
            CompressionEngine.DateTimeToDosDateAndTime(this.fileLastWriteTime, out date, out time);
            attribs = (short)this.fileAttributes;

            Stream stream = this.FileStream;
            this.FileStream = new DuplicateStream(stream);
            int streamHandle = this.StreamHandles.AllocHandle(stream);
            err = 0;
            return streamHandle;
        }

        private int CabFilePlaced(
            IntPtr pccab,
            string filePath,
            long fileSize,
            int continuation,
            IntPtr pv)
        {
            return 0;
        }

        private int CabGetNextCabinet(IntPtr pccab, uint prevCabSize, IntPtr pv)
        {
            NativeMethods.FCI.CCAB nextCcab = new NativeMethods.FCI.CCAB();
            Marshal.PtrToStructure(pccab, nextCcab);

            nextCcab.szDisk = String.Empty;
            nextCcab.szCab = this.context.GetArchiveName(nextCcab.iCab);
            this.CabNumbers[nextCcab.szCab] = (short)nextCcab.iCab;
            this.NextCabinetName = nextCcab.szCab;

            Marshal.StructureToPtr(nextCcab, pccab, false);
            return 1;
        }

        private int CabCreateStatus(NativeMethods.FCI.STATUS typeStatus, uint cb1, uint cb2, IntPtr pv)
        {
            switch (typeStatus)
            {
                case NativeMethods.FCI.STATUS.FILE:
                    if (cb2 > 0 && this.currentFileBytesProcessed < this.currentFileTotalBytes)
                    {
                        if (this.currentFileBytesProcessed + cb2 > this.currentFileTotalBytes)
                        {
                            cb2 = (uint)this.currentFileTotalBytes - (uint)this.currentFileBytesProcessed;
                        }
                        this.currentFileBytesProcessed += cb2;
                        this.fileBytesProcessed += cb2;

                        this.OnProgress(ArchiveProgressType.PartialFile);
                    }
                    break;

                case NativeMethods.FCI.STATUS.FOLDER:
                    if (cb1 == 0)
                    {
                        this.currentFolderTotalBytes = cb2 - this.totalFolderBytesProcessedInCurrentCab;
                        this.totalFolderBytesProcessedInCurrentCab = cb2;
                    }
                    else if (this.currentFolderTotalBytes == 0)
                    {
                        this.OnProgress(ArchiveProgressType.PartialArchive);
                    }
                    break;

                case NativeMethods.FCI.STATUS.CABINET:
                    break;
            }
            return 0;
        }

        private int CabGetTempFile(IntPtr tempNamePtr, int tempNameSize, IntPtr pv)
        {
            string tempFileName;
            if (this.UseTempFiles)
            {
                tempFileName = Path.GetFileName(Path.GetTempFileName());
            }
            else
            {
                tempFileName = CabPacker.TempStreamName;
            }

            byte[] tempNameBytes = Encoding.ASCII.GetBytes(tempFileName);
            if (tempNameBytes.Length >= tempNameSize)
            {
                return -1;
            }

            Marshal.Copy(tempNameBytes, 0, tempNamePtr, tempNameBytes.Length);
            Marshal.WriteByte(tempNamePtr, tempNameBytes.Length, 0);  // null-terminator
            return 1;
        }

        private int CabDeleteFile(string path, out int err, IntPtr pv)
        {
            try
            {
                // Deleting a temp file - don't bother if it is only a memory stream.
                if (path != CabPacker.TempStreamName)
                {
                    path = Path.Combine(Path.GetTempPath(), path);
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Failure to delete a temp file is not fatal.
            }
            err = 0;
            return 1;
        }
    }
}
