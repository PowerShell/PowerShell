//---------------------------------------------------------------------
// <copyright file="CabWorker.cs" company="Microsoft Corporation">
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
    using System.IO;
    using System.Runtime.InteropServices;

    internal abstract class CabWorker : IDisposable
    {
        internal const string CabStreamName = "%%CAB%%";

        private CabEngine cabEngine;

        private HandleManager<Stream> streamHandles;
        private Stream cabStream;
        private Stream fileStream;

        private NativeMethods.ERF erf;
        private GCHandle erfHandle;

        private IDictionary<string, short> cabNumbers;
        private string nextCabinetName;

        private bool suppressProgressEvents;

        private byte[] buf;

        // Progress data
        protected string currentFileName;
        protected int    currentFileNumber;
        protected int    totalFiles;
        protected long   currentFileBytesProcessed;
        protected long   currentFileTotalBytes;
        protected short  currentFolderNumber;
        protected long   currentFolderTotalBytes;
        protected string currentArchiveName;
        protected short  currentArchiveNumber;
        protected short  totalArchives;
        protected long   currentArchiveBytesProcessed;
        protected long   currentArchiveTotalBytes;
        protected long   fileBytesProcessed;
        protected long   totalFileBytes;

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected CabWorker(CabEngine cabEngine)
        {
            this.cabEngine = cabEngine;
            this.streamHandles = new HandleManager<Stream>();
            this.erf = new NativeMethods.ERF();
            this.erfHandle = GCHandle.Alloc(this.erf, GCHandleType.Pinned);
            this.cabNumbers = new Dictionary<string, short>(1);

            // 32K seems to be the size of the largest chunks processed by cabinet.dll.
            // But just in case, this buffer will auto-enlarge.
            this.buf = new byte[32768];
        }

        ~CabWorker()
        {
            this.Dispose(false);
        }

        public CabEngine CabEngine
        {
            get
            {
                return this.cabEngine;
            }
        }

        internal NativeMethods.ERF Erf
        {
            get
            {
                return this.erf;
            }
        }

        internal GCHandle ErfHandle
        {
            get
            {
                return this.erfHandle;
            }
        }

        internal HandleManager<Stream> StreamHandles
        {
            get
            {
                return this.streamHandles;
            }
        }

        internal bool SuppressProgressEvents
        {
            get
            {
                return this.suppressProgressEvents;
            }

            set
            {
                this.suppressProgressEvents = value;
            }
        }

        internal IDictionary<string, short> CabNumbers
        {
            get
            {
                return this.cabNumbers;
            }
        }

        internal string NextCabinetName
        {
            get
            {
                return this.nextCabinetName;
            }

            set
            {
                this.nextCabinetName = value;
            }
        }

        internal Stream CabStream
        {
            get
            {
                return this.cabStream;
            }

            set
            {
                this.cabStream = value;
            }
        }

        internal Stream FileStream
        {
            get
            {
                return this.fileStream;
            }

            set
            {
                this.fileStream = value;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void ResetProgressData()
        {
            this.currentFileName = null;
            this.currentFileNumber = 0;
            this.totalFiles = 0;
            this.currentFileBytesProcessed = 0;
            this.currentFileTotalBytes = 0;
            this.currentFolderNumber = 0;
            this.currentFolderTotalBytes = 0;
            this.currentArchiveName = null;
            this.currentArchiveNumber = 0;
            this.totalArchives = 0;
            this.currentArchiveBytesProcessed = 0;
            this.currentArchiveTotalBytes = 0;
            this.fileBytesProcessed = 0;
            this.totalFileBytes = 0;
        }

        protected void OnProgress(ArchiveProgressType progressType)
        {
            if (!this.suppressProgressEvents)
            {
                ArchiveProgressEventArgs e = new ArchiveProgressEventArgs(
                    progressType,
                    this.currentFileName,
                    this.currentFileNumber >= 0 ? this.currentFileNumber : 0,
                    this.totalFiles,
                    this.currentFileBytesProcessed,
                    this.currentFileTotalBytes,
                    this.currentArchiveName,
                    this.currentArchiveNumber,
                    this.totalArchives,
                    this.currentArchiveBytesProcessed,
                    this.currentArchiveTotalBytes,
                    this.fileBytesProcessed,
                    this.totalFileBytes);
                this.CabEngine.ReportProgress(e);
            }
        }

        internal IntPtr CabAllocMem(int byteCount)
        {
            IntPtr memPointer = Marshal.AllocHGlobal((IntPtr) byteCount);
            return memPointer;
        }

        internal void CabFreeMem(IntPtr memPointer)
        {
            Marshal.FreeHGlobal(memPointer);
        }

        internal int CabOpenStream(string path, int openFlags, int shareMode)
        {
            int err; return this.CabOpenStreamEx(path, openFlags, shareMode, out err, IntPtr.Zero);
        }

        internal virtual int CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, IntPtr pv)
        {
            path = path.Trim();
            Stream stream = this.cabStream;
            this.cabStream = new DuplicateStream(stream);
            int streamHandle = this.streamHandles.AllocHandle(stream);
            err = 0;
            return streamHandle;
        }

        internal int CabReadStream(int streamHandle, IntPtr memory, int cb)
        {
            int err; return this.CabReadStreamEx(streamHandle, memory, cb, out err, IntPtr.Zero);
        }

        internal virtual int CabReadStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv)
        {
            Stream stream = this.streamHandles[streamHandle];
            int count = (int) cb;
            if (count > this.buf.Length)
            {
                this.buf = new byte[count];
            }
            count = stream.Read(this.buf, 0, count);
            Marshal.Copy(this.buf, 0, memory, count);
            err = 0;
            return count;
        }

        internal int CabWriteStream(int streamHandle, IntPtr memory, int cb)
        {
            int err; return this.CabWriteStreamEx(streamHandle, memory, cb, out err, IntPtr.Zero);
        }

        internal virtual int CabWriteStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv)
        {
            Stream stream = this.streamHandles[streamHandle];
            int count = (int) cb;
            if (count > this.buf.Length)
            {
                this.buf = new byte[count];
            }
            Marshal.Copy(memory, this.buf, 0, count);
            stream.Write(this.buf, 0, count);
            err = 0;
            return cb;
        }

        internal int CabCloseStream(int streamHandle)
        {
            int err; return this.CabCloseStreamEx(streamHandle, out err, IntPtr.Zero);
        }

        internal virtual int CabCloseStreamEx(int streamHandle, out int err, IntPtr pv)
        {
            this.streamHandles.FreeHandle(streamHandle);
            err = 0;
            return 0;
        }

        internal int CabSeekStream(int streamHandle, int offset, int seekOrigin)
        {
            int err; return this.CabSeekStreamEx(streamHandle, offset, seekOrigin, out err, IntPtr.Zero);
        }

        internal virtual int CabSeekStreamEx(int streamHandle, int offset, int seekOrigin, out int err, IntPtr pv)
        {
            Stream stream = this.streamHandles[streamHandle];
            offset = (int) stream.Seek(offset, (SeekOrigin) seekOrigin);
            err = 0;
            return offset;
        }

        /// <summary>
        /// Disposes of resources allocated by the cabinet engine.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly or indirectly by a user's code,
        /// so managed and unmanaged resources will be disposed. If false, the method has been called by the
        /// runtime from inside the finalizer, and only unmanaged resources will be disposed.</param>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.cabStream != null)
                {
#if CORECLR
                    this.cabStream.Dispose();
#else
                    this.cabStream.Close();
#endif
                    this.cabStream = null;
                }

                if (this.fileStream != null)
                {
#if CORECLR
                    this.fileStream.Dispose();
#else
                    this.fileStream.Close();
#endif
                    this.fileStream = null;
                }
            }

            if (this.erfHandle.IsAllocated)
            {
                this.erfHandle.Free();
            }
        }

        protected void CheckError(bool extracting)
        {
            if (this.Erf.Error)
            {
                throw new CabException(
                    this.Erf.Oper,
                    this.Erf.Type,
                    CabException.GetErrorMessage(this.Erf.Oper, this.Erf.Type, extracting));
            }
        }
    }
}
