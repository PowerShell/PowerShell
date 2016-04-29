//---------------------------------------------------------------------
// <copyright file="CabUnpacker.cs" company="Microsoft Corporation">
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

    internal class CabUnpacker : CabWorker
    {
        private NativeMethods.FDI.Handle fdiHandle;

        // These delegates need to be saved as member variables
        // so that they don't get GC'd.
        private NativeMethods.FDI.PFNALLOC fdiAllocMemHandler;
        private NativeMethods.FDI.PFNFREE fdiFreeMemHandler;
        private NativeMethods.FDI.PFNOPEN fdiOpenStreamHandler;
        private NativeMethods.FDI.PFNREAD fdiReadStreamHandler;
        private NativeMethods.FDI.PFNWRITE fdiWriteStreamHandler;
        private NativeMethods.FDI.PFNCLOSE fdiCloseStreamHandler;
        private NativeMethods.FDI.PFNSEEK fdiSeekStreamHandler;

        private IUnpackStreamContext context;

        private List<ArchiveFileInfo> fileList;

        private int folderId;

        private Predicate<string> filter;

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
#if !CORECLR
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
#endif
        public CabUnpacker(CabEngine cabEngine)
            : base(cabEngine)
        {
            this.fdiAllocMemHandler = this.CabAllocMem;
            this.fdiFreeMemHandler = this.CabFreeMem;
            this.fdiOpenStreamHandler = this.CabOpenStream;
            this.fdiReadStreamHandler = this.CabReadStream;
            this.fdiWriteStreamHandler = this.CabWriteStream;
            this.fdiCloseStreamHandler = this.CabCloseStream;
            this.fdiSeekStreamHandler = this.CabSeekStream;

            this.fdiHandle = NativeMethods.FDI.Create(
                this.fdiAllocMemHandler,
                this.fdiFreeMemHandler,
                this.fdiOpenStreamHandler,
                this.fdiReadStreamHandler,
                this.fdiWriteStreamHandler,
                this.fdiCloseStreamHandler,
                this.fdiSeekStreamHandler,
                NativeMethods.FDI.CPU_80386,
                this.ErfHandle.AddrOfPinnedObject());
            if (this.Erf.Error)
            {
                int error = this.Erf.Oper;
                int errorCode = this.Erf.Type;
                this.ErfHandle.Free();
                throw new CabException(
                    error,
                    errorCode,
                    CabException.GetErrorMessage(error, errorCode, true));
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
#if !CORECLR
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
#endif
        public bool IsArchive(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            lock (this)
            {
                short id;
                int folderCount, fileCount;
                return this.IsCabinet(stream, out id, out folderCount, out fileCount);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
#if !CORECLR
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
#endif
        public IList<ArchiveFileInfo> GetFileInfo(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter)
        {
            if (streamContext == null)
            {
                throw new ArgumentNullException("streamContext");
            }

            lock (this)
            {
                this.context = streamContext;
                this.filter = fileFilter;
                this.NextCabinetName = String.Empty;
                this.fileList = new List<ArchiveFileInfo>();
                bool tmpSuppress = this.SuppressProgressEvents;
                this.SuppressProgressEvents = true;
                try
                {
                    for (short cabNumber = 0;
                         this.NextCabinetName != null;
                         cabNumber++)
                    {
                        this.Erf.Clear();
                        this.CabNumbers[this.NextCabinetName] = cabNumber;

                        var result = NativeMethods.FDI.Copy(
                            this.fdiHandle,
                            this.NextCabinetName,
                            String.Empty,
                            0,
                            this.CabListNotify,
                            IntPtr.Zero,
                            IntPtr.Zero);
                        if (result == 0) {
                            // stop compiler from complaining
                            this.CheckError(true);
                        }
                        this.CheckError(true);
                    }

                    List<ArchiveFileInfo> tmpFileList = this.fileList;
                    this.fileList = null;
                    return tmpFileList.AsReadOnly();
                }
                finally
                {
                    this.SuppressProgressEvents = tmpSuppress;

                    if (this.CabStream != null)
                    {
                        this.context.CloseArchiveReadStream(
                            this.currentArchiveNumber,
                            this.currentArchiveName,
                            this.CabStream);
                        this.CabStream = null;
                    }

                    this.context = null;
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
#if !CORECLR
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
#endif
        public void Unpack(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter)
        {
            lock (this)
            {
                IList<ArchiveFileInfo> files =
                    this.GetFileInfo(streamContext, fileFilter);

                this.ResetProgressData();

                if (files != null)
                {
                    this.totalFiles = files.Count;

                    for (int i = 0; i < files.Count; i++)
                    {
                        this.totalFileBytes += files[i].Length;
                        if (files[i].ArchiveNumber >= this.totalArchives)
                        {
                            int totalArchives = files[i].ArchiveNumber + 1;
                            this.totalArchives = (short) totalArchives;
                        }
                    }
                }

                this.context = streamContext;
                this.fileList = null;
                this.NextCabinetName = String.Empty;
                this.folderId = -1;
                this.currentFileNumber = -1;

                try
                {
                    for (short cabNumber = 0;
                         this.NextCabinetName != null;
                         cabNumber++)
                    {
                        this.Erf.Clear();
                        this.CabNumbers[this.NextCabinetName] = cabNumber;

                        var result =NativeMethods.FDI.Copy(
                            this.fdiHandle,
                            this.NextCabinetName,
                            String.Empty,
                            0,
                            this.CabExtractNotify,
                            IntPtr.Zero,
                            IntPtr.Zero);
                        if (result == 0) {
                            // stop compiler from complaining
                            this.CheckError(true);
                        }
                        this.CheckError(true);
                    }
                }
                finally
                {
                    if (this.CabStream != null)
                    {
                        this.context.CloseArchiveReadStream(
                            this.currentArchiveNumber,
                            this.currentArchiveName,
                            this.CabStream);
                        this.CabStream = null;
                    }

                    if (this.FileStream != null)
                    {
                        this.context.CloseFileWriteStream(this.currentFileName, this.FileStream, FileAttributes.Normal, DateTime.Now);
                        this.FileStream = null;
                    }

                    this.context = null;
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

                    stream = this.context.OpenArchiveReadStream(cabNumber, path, this.CabEngine);
                    if (stream == null)
                    {
                        throw new FileNotFoundException(String.Format(CultureInfo.InvariantCulture, "Cabinet {0} not provided.", cabNumber));
                    }
                    this.currentArchiveName = path;
                    this.currentArchiveNumber = cabNumber;
                    if (this.totalArchives <= this.currentArchiveNumber)
                    {
                        int totalArchives = this.currentArchiveNumber + 1;
                        this.totalArchives = (short) totalArchives;
                    }
                    this.currentArchiveTotalBytes = stream.Length;
                    this.currentArchiveBytesProcessed = 0;

                    if (this.folderId != -3)  // -3 is a special folderId that requires re-opening the same cab
                    {
                        this.OnProgress(ArchiveProgressType.StartArchive);
                    }
                    this.CabStream = stream;
                }
                path = CabWorker.CabStreamName;
            }
            return base.CabOpenStreamEx(path, openFlags, shareMode, out err, pv);
        }

        internal override int CabReadStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv)
        {
            int count = base.CabReadStreamEx(streamHandle, memory, cb, out err, pv);
            if (err == 0 && this.CabStream != null)
            {
                if (this.fileList == null)
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
            }
            return count;
        }

        internal override int CabWriteStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv)
        {
            int count = base.CabWriteStreamEx(streamHandle, memory, cb, out err, pv);
            if (count > 0 && err == 0)
            {
                this.currentFileBytesProcessed += cb;
                this.fileBytesProcessed += cb;
                this.OnProgress(ArchiveProgressType.PartialFile);
            }
            return count;
        }

        internal override int CabCloseStreamEx(int streamHandle, out int err, IntPtr pv)
        {
            Stream stream = DuplicateStream.OriginalStream(this.StreamHandles[streamHandle]);

            if (stream == DuplicateStream.OriginalStream(this.CabStream))
            {
                if (this.folderId != -3)  // -3 is a special folderId that requires re-opening the same cab
                {
                    this.OnProgress(ArchiveProgressType.FinishArchive);
                }

                this.context.CloseArchiveReadStream(this.currentArchiveNumber, this.currentArchiveName, stream);

                this.currentArchiveName = this.NextCabinetName;
                this.currentArchiveBytesProcessed = this.currentArchiveTotalBytes = 0;

                this.CabStream = null;
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
                    if (this.fdiHandle != null)
                    {
                        this.fdiHandle.Dispose();
                        this.fdiHandle = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private static string GetFileName(NativeMethods.FDI.NOTIFICATION notification)
        {
            bool utf8Name = (notification.attribs & (ushort) FileAttributes.Normal) != 0;  // _A_NAME_IS_UTF

            // Non-utf8 names should be completely ASCII. But for compatibility with
            // legacy tools, interpret them using the current (Default) ANSI codepage.
            Encoding nameEncoding = utf8Name ? Encoding.UTF8 : Encoding.GetEncoding(0);

            // Find how many bytes are in the string.
            // Unfortunately there is no faster way.
            int nameBytesCount = 0;
            while (Marshal.ReadByte(notification.psz1, nameBytesCount) != 0)
            {
                nameBytesCount++;
            }

            byte[] nameBytes = new byte[nameBytesCount];
            Marshal.Copy(notification.psz1, nameBytes, 0, nameBytesCount);
            string name = nameEncoding.GetString(nameBytes);
            if (Path.IsPathRooted(name))
            {
                name = name.Replace("" + Path.VolumeSeparatorChar, "");
            }

            return name;
        }

        private bool IsCabinet(Stream cabStream, out short id, out int cabFolderCount, out int fileCount)
        {
            int streamHandle = this.StreamHandles.AllocHandle(cabStream);
            try
            {
                this.Erf.Clear();
                NativeMethods.FDI.CABINFO fdici;
                bool isCabinet = 0 != NativeMethods.FDI.IsCabinet(this.fdiHandle, streamHandle, out fdici);

                if (this.Erf.Error)
                {
                    if (((NativeMethods.FDI.ERROR) this.Erf.Oper) == NativeMethods.FDI.ERROR.UNKNOWN_CABINET_VERSION)
                    {
                        isCabinet = false;
                    }
                    else
                    {
                        throw new CabException(
                            this.Erf.Oper,
                            this.Erf.Type,
                            CabException.GetErrorMessage(this.Erf.Oper, this.Erf.Type, true));
                    }
                }

                id = fdici.setID;
                cabFolderCount = (int) fdici.cFolders;
                fileCount = (int) fdici.cFiles;
                return isCabinet;
            }
            finally
            {
                this.StreamHandles.FreeHandle(streamHandle);
            }
        }

        private int CabListNotify(NativeMethods.FDI.NOTIFICATIONTYPE notificationType, NativeMethods.FDI.NOTIFICATION notification)
        {
            switch (notificationType)
            {
                case NativeMethods.FDI.NOTIFICATIONTYPE.CABINET_INFO:
                    {
                        string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                        this.NextCabinetName = (nextCab.Length != 0 ? nextCab : null);
                        return 0;  // Continue
                    }
                case NativeMethods.FDI.NOTIFICATIONTYPE.PARTIAL_FILE:
                    {
                        // This notification can occur when examining the contents of a non-first cab file.
                        return 0;  // Continue
                    }
                case NativeMethods.FDI.NOTIFICATIONTYPE.COPY_FILE:
                    {
                        //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

                        string name = CabUnpacker.GetFileName(notification);

                        if (this.filter == null || this.filter(name))
                        {
                            if (this.fileList != null)
                            {
                                FileAttributes attributes = (FileAttributes) notification.attribs &
                                    (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                                if (attributes == (FileAttributes) 0)
                                {
                                    attributes = FileAttributes.Normal;
                                }
                                DateTime lastWriteTime;
                                CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);
                                long length = notification.cb;

                                CabFileInfo fileInfo = new CabFileInfo(
                                    name,
                                    notification.iFolder,
                                    notification.iCabinet,
                                    attributes,
                                    lastWriteTime,
                                    length);
                                this.fileList.Add(fileInfo);
                                this.currentFileNumber = this.fileList.Count - 1;
                                this.fileBytesProcessed += notification.cb;
                            }
                        }

                        this.totalFiles++;
                        this.totalFileBytes += notification.cb;
                        return 0;  // Continue
                    }
            }
            return 0;
        }

        private int CabExtractNotify(NativeMethods.FDI.NOTIFICATIONTYPE notificationType, NativeMethods.FDI.NOTIFICATION notification)
        {
            switch (notificationType)
            {
                case NativeMethods.FDI.NOTIFICATIONTYPE.CABINET_INFO:
                    {
                        if (this.NextCabinetName != null && this.NextCabinetName.StartsWith("?", StringComparison.Ordinal))
                        {
                            // We are just continuing the copy of a file that spanned cabinets.
                            // The next cabinet name needs to be preserved.
                            this.NextCabinetName = this.NextCabinetName.Substring(1);
                        }
                        else
                        {
                            string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                            this.NextCabinetName = (nextCab.Length != 0 ? nextCab : null);
                        }
                        return 0;  // Continue
                    }
                case NativeMethods.FDI.NOTIFICATIONTYPE.NEXT_CABINET:
                    {
                        string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                        this.CabNumbers[nextCab] = (short) notification.iCabinet;
                        this.NextCabinetName = "?" + this.NextCabinetName;
                        return 0;  // Continue
                    }
                case NativeMethods.FDI.NOTIFICATIONTYPE.COPY_FILE:
                    {
                        return this.CabExtractCopyFile(notification);
                    }
                case NativeMethods.FDI.NOTIFICATIONTYPE.CLOSE_FILE_INFO:
                    {
                        return this.CabExtractCloseFile(notification);
                    }
            }
            return 0;
        }

        private int CabExtractCopyFile(NativeMethods.FDI.NOTIFICATION notification)
        {
            if (notification.iFolder != this.folderId)
            {
                if (notification.iFolder != -3)  // -3 is a special folderId used when continuing a folder from a previous cab
                {
                    if (this.folderId != -1) // -1 means we just started the extraction sequence
                    {
                        this.currentFolderNumber++;
                    }
                }
                this.folderId = notification.iFolder;
            }

            //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

            string name = CabUnpacker.GetFileName(notification);

            if (this.filter == null || this.filter(name))
            {
                this.currentFileNumber++;
                this.currentFileName = name;

                this.currentFileBytesProcessed = 0;
                this.currentFileTotalBytes = notification.cb;
                this.OnProgress(ArchiveProgressType.StartFile);

                DateTime lastWriteTime;
                CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);

                Stream stream = this.context.OpenFileWriteStream(name, notification.cb, lastWriteTime);
                if (stream != null)
                {
                    this.FileStream = stream;
                    int streamHandle = this.StreamHandles.AllocHandle(stream);
                    return streamHandle;
                }
                else
                {
                    this.fileBytesProcessed += notification.cb;
                    this.OnProgress(ArchiveProgressType.FinishFile);
                    this.currentFileName = null;
                }
            }
            return 0;  // Continue
        }

        private int CabExtractCloseFile(NativeMethods.FDI.NOTIFICATION notification)
        {
            Stream stream = this.StreamHandles[notification.hf];
            this.StreamHandles.FreeHandle(notification.hf);

            //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

            string name = CabUnpacker.GetFileName(notification);

            FileAttributes attributes = (FileAttributes) notification.attribs &
                (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
            if (attributes == (FileAttributes) 0)
            {
                attributes = FileAttributes.Normal;
            }
            DateTime lastWriteTime;
            CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);

            stream.Flush();
            this.context.CloseFileWriteStream(name, stream, attributes, lastWriteTime);
            this.FileStream = null;

            long remainder = this.currentFileTotalBytes - this.currentFileBytesProcessed;
            this.currentFileBytesProcessed += remainder;
            this.fileBytesProcessed += remainder;
            this.OnProgress(ArchiveProgressType.FinishFile);
            this.currentFileName = null;

            return 1;  // Continue
        }
    }
}
