// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Management.Automation.Internal
{
    internal class CabinetExtractor : ICabinetExtractor
    {
        /// <summary>
        /// GC handle which prevents garbage collector from collecting this delegate.
        /// </summary>
        private CabinetNativeApi.FdiAllocDelegate _allocDelegate;
        private GCHandle _fdiAllocHandle;
        private CabinetNativeApi.FdiFreeDelegate _freeDelegate;
        private GCHandle _fdiFreeHandle;
        private CabinetNativeApi.FdiOpenDelegate _openDelegate;
        private GCHandle _fdiOpenHandle;
        private CabinetNativeApi.FdiReadDelegate _readDelegate;
        private GCHandle _fdiReadHandle;
        private CabinetNativeApi.FdiWriteDelegate _writeDelegate;
        private GCHandle _fdiWriteHandle;
        private CabinetNativeApi.FdiCloseDelegate _closeDelegate;
        private GCHandle _fdiCloseHandle;
        private CabinetNativeApi.FdiSeekDelegate _seekDelegate;
        private GCHandle _fdiSeekHandle;
        private CabinetNativeApi.FdiNotifyDelegate _notifyDelegate;
        private GCHandle _fdiNotifyHandle;

        internal CabinetNativeApi.FdiContextHandle fdiContext; // HFDI

        internal CabinetExtractor()
        {
            CabinetNativeApi.FdiERF err = new CabinetNativeApi.FdiERF();

            populateDelegates();

            // marshal the delegate to a unmanaged function pointer so that AppDomain reference is stored correctly.
            fdiContext = CabinetNativeApi.FDICreate(
                Marshal.GetFunctionPointerForDelegate(_allocDelegate),
                Marshal.GetFunctionPointerForDelegate(_freeDelegate),
                Marshal.GetFunctionPointerForDelegate(_openDelegate),
                Marshal.GetFunctionPointerForDelegate(_readDelegate),
                Marshal.GetFunctionPointerForDelegate(_writeDelegate),
                Marshal.GetFunctionPointerForDelegate(_closeDelegate),
                Marshal.GetFunctionPointerForDelegate(_seekDelegate),
                CabinetNativeApi.FdiCreateCpuType.Cpu80386,
                err);
        }

        #region IDisposable Methods

        /// <summary>
        /// Flag: Has Dispose already been called?
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Free managed objects within 'if (disposing)' if needed
            fdiContext?.Dispose();
            // Free unmanaged objects here
            this.CleanUpDelegates();

            _disposed = true;

            // Call base class implementation in case it has resources to release
            base.Dispose(disposing);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CabinetExtractor"/> class.
        /// </summary>
        ~CabinetExtractor()
        {
            Dispose(false);
        }

        #endregion

        internal override bool Extract(string cabinetName, string srcPath, string destPath)
        {
            IntPtr nativeDestPath = Marshal.StringToHGlobalAnsi(destPath);

            bool result = CabinetNativeApi.FDICopy(
                fdiContext,
                cabinetName,
                srcPath,
                0,                    // Not used
                Marshal.GetFunctionPointerForDelegate(_notifyDelegate),
                IntPtr.Zero,
                nativeDestPath);

            Marshal.FreeHGlobal(nativeDestPath);

            return result;
        }

        /// <summary>
        /// Creates delegates for the FDI* operation functions.
        /// </summary>
        private void populateDelegates()
        {
            // if a delegate is re-located by a garbage collection, it will not affect
            // the underlaying managed callback, so Alloc is used to add a reference
            // to the delegate, allowing relocation of the delegate, but preventing
            // disposal. Using GCHandle without pinning reduces fragmentation potential
            // of the managed heap.
            _allocDelegate = new CabinetNativeApi.FdiAllocDelegate(CabinetNativeApi.FdiAlloc);
            _fdiAllocHandle = GCHandle.Alloc(_allocDelegate);

            _freeDelegate = new CabinetNativeApi.FdiFreeDelegate(CabinetNativeApi.FdiFree);
            _fdiFreeHandle = GCHandle.Alloc(_freeDelegate);

            _openDelegate = new CabinetNativeApi.FdiOpenDelegate(CabinetNativeApi.FdiOpen);
            _fdiOpenHandle = GCHandle.Alloc(_openDelegate);

            _readDelegate = new CabinetNativeApi.FdiReadDelegate(CabinetNativeApi.FdiRead);
            _fdiReadHandle = GCHandle.Alloc(_readDelegate);

            _writeDelegate = new CabinetNativeApi.FdiWriteDelegate(CabinetNativeApi.FdiWrite);
            _fdiWriteHandle = GCHandle.Alloc(_writeDelegate);

            _closeDelegate = new CabinetNativeApi.FdiCloseDelegate(CabinetNativeApi.FdiClose);
            _fdiCloseHandle = GCHandle.Alloc(_closeDelegate);

            _seekDelegate = new CabinetNativeApi.FdiSeekDelegate(CabinetNativeApi.FdiSeek);
            _fdiSeekHandle = GCHandle.Alloc(_seekDelegate);

            _notifyDelegate = new CabinetNativeApi.FdiNotifyDelegate(CabinetNativeApi.FdiNotify);
            _fdiNotifyHandle = GCHandle.Alloc(_notifyDelegate);
        }

        /// <summary>
        /// Frees all the delegate handles.
        /// </summary>
        private void CleanUpDelegates()
        {
            // Free GCHandles so that the memory they point to may be unpinned (garbage collected)
            if (_fdiAllocHandle.IsAllocated)
            {
                _fdiAllocHandle.Free();
                _fdiFreeHandle.Free();
                _fdiOpenHandle.Free();
                _fdiReadHandle.Free();
                _fdiWriteHandle.Free();
                _fdiCloseHandle.Free();
                _fdiSeekHandle.Free();
                _fdiNotifyHandle.Free();
            }
        }
    }

    // CabinetExtractor loader implementation
    internal class CabinetExtractorLoader : ICabinetExtractorLoader
    {
        private static CabinetExtractor s_extractorInstance;
        private static CabinetExtractorLoader s_instance;
        private static double s_created = 0;

        internal static CabinetExtractorLoader GetInstance()
        {
            if (System.Threading.Interlocked.CompareExchange(ref s_created, 1, 0) == 0)
            {
                s_instance = new CabinetExtractorLoader();
                s_extractorInstance = new CabinetExtractor();
            }

            return s_instance;
        }

        internal override ICabinetExtractor GetCabinetExtractor()
        {
            return s_extractorInstance;
        }
    }

    internal static class CabinetNativeApi
    {
        #region Delegates and function definitions

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate IntPtr FdiAllocDelegate(int size);

        internal static IntPtr FdiAlloc(int size)
        {
            try
            {
                return Marshal.AllocHGlobal(size);
            }
            catch (OutOfMemoryException)
            {
                return IntPtr.Zero;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate void FdiFreeDelegate(IntPtr memblock);

        internal static void FdiFree(IntPtr memblock)
        {
            Marshal.FreeHGlobal(memblock);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate IntPtr FdiOpenDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            int oflag,
            int pmode);

        internal static IntPtr FdiOpen(string filename, int oflag, int pmode)
        {
            FileMode mode = CabinetNativeApi.ConvertOpflagToFileMode(oflag);

            FileAccess access = CabinetNativeApi.ConvertPermissionModeToFileAccess(pmode);
            FileShare share = CabinetNativeApi.ConvertPermissionModeToFileShare(pmode);

            // This method is used for opening the cab file as well as saving the extracted files.
            // When we are opening the cab file we only need read permissions.
            // We force read permissions so that non-elevated users can extract cab files.
            if (mode == FileMode.Open || mode == FileMode.OpenOrCreate)
            {
                access = FileAccess.Read;
                share = FileShare.Read;
            }

            try
            {
                FileStream stream = new FileStream(filename, mode, access, share);

                if (stream == null)
                {
                    return new IntPtr(-1);
                }

                return GCHandle.ToIntPtr(GCHandle.Alloc(stream));
            }
            catch (IOException)
            {
                return new IntPtr(-1);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int FdiReadDelegate(
            IntPtr fp,
            [In, Out]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.U1)]
            byte[] buffer,
            int count);

        internal static int FdiRead(IntPtr fp, byte[] buffer, int count)
        {
            GCHandle handle = GCHandle.FromIntPtr(fp);
            FileStream stream = (FileStream)handle.Target;

            int numCharactersRead = 0;
            try
            {
                numCharactersRead = stream.Read(buffer, 0, count);
            }
            catch (ArgumentNullException) { numCharactersRead = -1; }
            catch (ArgumentOutOfRangeException) { numCharactersRead = -1; }
            catch (NotSupportedException) { numCharactersRead = -1; }
            catch (IOException) { numCharactersRead = -1; }
            catch (ArgumentException) { numCharactersRead = -1; }
            catch (ObjectDisposedException) { numCharactersRead = -1; }

            return numCharactersRead;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int FdiWriteDelegate(
            IntPtr fp,
            [In]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.U1)]
            byte[] buffer,
            int count);

        internal static int FdiWrite(IntPtr fp, byte[] buffer, int count)
        {
            GCHandle handle = GCHandle.FromIntPtr(fp);
            FileStream stream = (FileStream)handle.Target;

            int numCharactersWritten = 0;
            try
            {
                stream.Write(buffer, 0, count);
                numCharactersWritten = count; // Write doesn't return the number of bytes written. Per MSDN, if it succeeds, it will have written count bytes.
            }
            catch (ArgumentNullException) { numCharactersWritten = -1; }
            catch (ArgumentOutOfRangeException) { numCharactersWritten = -1; }
            catch (NotSupportedException) { numCharactersWritten = -1; }
            catch (IOException) { numCharactersWritten = -1; }
            catch (ArgumentException) { numCharactersWritten = -1; }
            catch (ObjectDisposedException) { numCharactersWritten = -1; }

            return numCharactersWritten;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int FdiCloseDelegate(IntPtr fp);

        internal static int FdiClose(IntPtr fp)
        {
            GCHandle handle = GCHandle.FromIntPtr(fp);
            FileStream stream = (FileStream)handle.Target;

            if (stream == null)
            {
                return -1;
            }
            else
            {
                stream.Dispose();
                return 0;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int FdiSeekDelegate(IntPtr fp, int offset, int origin);

        internal static int FdiSeek(IntPtr fp, int offset, int origin)
        {
            GCHandle handle = GCHandle.FromIntPtr(fp);
            FileStream stream = (FileStream)handle.Target;

            SeekOrigin seekOrigin = CabinetNativeApi.ConvertOriginToSeekOrigin(origin);
            long status = 0;
            try
            {
                status = stream.Seek(offset, seekOrigin);
            }
            catch (NotSupportedException) { status = -1; }
            catch (IOException) { status = -1; }
            catch (ArgumentException) { status = -1; }
            catch (ObjectDisposedException) { status = -1; }

            return (int)status;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate IntPtr FdiNotifyDelegate(FdiNotificationType fdint, FdiNotification fdin);

        // Handles FDI notification
        internal static IntPtr FdiNotify(FdiNotificationType fdint, FdiNotification fdin)
        {
            switch (fdint)
            {
                case FdiNotificationType.FdintCOPY_FILE:
                    {
                        // TODO: Should I catch exceptions for the new functions?

                        // Copy target directory
                        string destPath = Marshal.PtrToStringAnsi(fdin.pv);

                        // Split the path to a filename and path
                        string fileName = Path.GetFileName(fdin.psz1);
                        string remainingPsz1Path = Path.GetDirectoryName(fdin.psz1);
                        destPath = Path.Combine(destPath, remainingPsz1Path);

                        Directory.CreateDirectory(destPath); // Creates all intermediate directories if necessary.

                        // Create the file
                        string absoluteFilePath = Path.Combine(destPath, fileName);
                        return CabinetNativeApi.FdiOpen(absoluteFilePath, (int)OpFlags.Create, (int)(PermissionMode.Read | PermissionMode.Write)); // TODO: OK to ignore _O_SEQUENTIAL, WrOnly, and _O_BINARY?
                    }
                case FdiNotificationType.FdintCLOSE_FILE_INFO:
                    {
                        // Close the file
                        CabinetNativeApi.FdiClose(fdin.hf);

                        // Set the file attributes
                        string destPath = Marshal.PtrToStringAnsi(fdin.pv);
                        string absoluteFilePath = Path.Combine(destPath, fdin.psz1);

                        IntPtr hFile = PlatformInvokes.CreateFile(
                            absoluteFilePath,
                            PlatformInvokes.FileDesiredAccess.GenericRead | PlatformInvokes.FileDesiredAccess.GenericWrite,
                            PlatformInvokes.FileShareMode.Read,
                            IntPtr.Zero,
                            PlatformInvokes.FileCreationDisposition.OpenExisting,
                            PlatformInvokes.FileAttributes.Normal,
                            IntPtr.Zero);

                        if (hFile != IntPtr.Zero)
                        {
                            PlatformInvokes.FILETIME ftFile = new PlatformInvokes.FILETIME();
                            if (PlatformInvokes.DosDateTimeToFileTime(fdin.date, fdin.time, ftFile))
                            {
                                PlatformInvokes.FILETIME ftLocal = new PlatformInvokes.FILETIME();
                                if (PlatformInvokes.LocalFileTimeToFileTime(ftFile, ftLocal))
                                {
                                    PlatformInvokes.SetFileTime(hFile, ftLocal, null, ftLocal);
                                }
                            }

                            PlatformInvokes.CloseHandle(hFile);
                        }

                        PlatformInvokes.SetFileAttributesW(
                            absoluteFilePath,
                            (PlatformInvokes.FileAttributes)fdin.attribs & (PlatformInvokes.FileAttributes.ReadOnly | PlatformInvokes.FileAttributes.Hidden | PlatformInvokes.FileAttributes.System | PlatformInvokes.FileAttributes.Archive));

                        // Call notification function
                        return new IntPtr(1);
                    }
            }

            return new IntPtr(0);
        }

        #endregion

        #region Helper methods for non-trivial conversions

        /// <summary>
        /// Converts an unmanaged define into a known managed value.
        /// </summary>
        /// <param name="origin">Defined in stdio.h.</param>
        /// <returns>The appropriate System.IO.SeekOrigin value.</returns>
        internal static SeekOrigin ConvertOriginToSeekOrigin(int origin)
        {
            switch (origin)
            {
                case 0x0: // SEEK_SET
                    return SeekOrigin.Begin;
                case 0x1: // SEEK_CUR
                    return SeekOrigin.Current;
                case 0x2: // SEEK_END
                    return SeekOrigin.End;
                default:
                    return SeekOrigin.Current;
            }
        }

        /// <summary>
        /// Converts an unmanaged define into a known managed type.
        /// </summary>
        /// <param name="oflag">Operation mode defined in fcntl.h.</param>
        /// <returns>The appropriate System.IO.FileMode type.</returns>
        internal static FileMode ConvertOpflagToFileMode(int oflag)
        {
            // Note: This is not done in a switch because the order of tests matters.

            if ((oflag & (int)(OpFlags.Create | OpFlags.Excl)) == (int)(OpFlags.Create | OpFlags.Excl))
            {
                return FileMode.CreateNew;
            }
            else if ((oflag & (int)(OpFlags.Create | OpFlags.Truncate)) == (int)(OpFlags.Create | OpFlags.Truncate))
            {
                return FileMode.OpenOrCreate;
            }
            else if ((oflag & (int)OpFlags.Append) != 0)
            {
                return FileMode.Append;
            }
            else if ((oflag & (int)OpFlags.Create) != 0)
            {
                return FileMode.Create;
            }
            else if ((oflag & (int)OpFlags.RdWr) != 0)
            {
                return FileMode.Open;
            }
            else if ((oflag & (int)OpFlags.Truncate) != 0)
            {
                return FileMode.Truncate;
            }
            else
            {
                return FileMode.OpenOrCreate; // This seemed the safest way to handled unrecognized types
            }
        }

        /// <summary>
        /// Converts an unmanaged define into a known managed type.
        /// </summary>
        /// <param name="pmode">Permission mode defined in stat.h.</param>
        /// <returns>The appropriate System.IO.FileAccess type.</returns>
        internal static FileAccess ConvertPermissionModeToFileAccess(int pmode)
        {
            // Note: This is not done in a switch because the order of tests matters.

            if ((pmode & (int)(PermissionMode.Read | PermissionMode.Write)) == (int)(PermissionMode.Read | PermissionMode.Write))
            {
                return FileAccess.ReadWrite;
            }
            else if ((pmode & (int)PermissionMode.Read) != 0)
            {
                return FileAccess.Read;
            }
            else if ((pmode & (int)PermissionMode.Write) != 0)
            {
                return FileAccess.Write;
            }
            else
            {
                return FileAccess.Read;
            }
        }

        /// <summary>
        /// Converts an unmanaged define into a known managed type.
        /// </summary>
        /// <param name="pmode">Permission mode defined in stat.h.</param>
        /// <returns>The appropriate System.IO.FileShare type.</returns>
        internal static FileShare ConvertPermissionModeToFileShare(int pmode)
        {
            // Note: This is not done in a switch because the order of tests matters.

            if ((pmode & (int)(PermissionMode.Read | PermissionMode.Write)) == (int)(PermissionMode.Read | PermissionMode.Write))
            {
                return FileShare.ReadWrite;
            }
            else if ((pmode & (int)PermissionMode.Read) != 0)
            {
                return FileShare.Read;
            }
            else if ((pmode & (int)PermissionMode.Write) != 0)
            {
                return FileShare.Write;
            }
            else
            {
                return FileShare.Read;
            }
        }

        #endregion

        #region IO classes, structures, and enums

        [Flags]
        internal enum PermissionMode : int
        {
            None = 0x0000,
            Write = 0x0080,
            Read = 0x0100
        }

        [Flags]
        internal enum OpFlags : int
        {
            RdOnly = 0x0000,
            WrOnly = 0x0001,
            RdWr = 0x0002,
            Append = 0x0008,
            Create = 0x0100,
            Truncate = 0x0200,
            Excl = 0x0400
        }

        internal enum FdiCreateCpuType : int
        {
            CpuUnknown = -1,
            Cpu80286 = 0,
            Cpu80386 = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal class FdiNotification
        {
            internal int cb; // LONG
            internal string psz1; // char FAR *
            internal string psz2; // char FAR *
            internal string psz3; // char FAR *
            internal IntPtr pv; // void FAR * // In this case, it is the destination path
            internal IntPtr hf; // INT_PTR
            internal short date; // USHORT
            internal short time; // USHORT
            internal short attribs; // USHORT
            internal short setID; // USHORT
            internal short iCabinet; // USHORT
            internal short iFolder; // USHORT
            internal int fdie; // FDIERROR
        }

        internal enum FdiNotificationType : int
        {
            FdintCABINET_INFO = 0x0,
            FdintPARTIAL_FILE = 0x1,
            FdintCOPY_FILE = 0x2,
            FdintCLOSE_FILE_INFO = 0x3,
            FdintNEXT_CABINET = 0x4,
            FdintENUMERATE = 0x5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class FdiERF
        {
            internal int erfOper;
            internal int erfType;
            internal bool fError;
        }

        internal sealed class FdiContextHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private FdiContextHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return CabinetNativeApi.FDIDestroy(this.handle);
            }
        }

        #endregion

        #region PInvoke Definitions

        /// <summary>
        /// Creates an FDI context.
        /// </summary>
        /// <param name="pfnalloc">_In_ PFNALLOC - Memory allocation delegate.</param>
        /// <param name="pfnfree">_In_ PFNFREE - Memory free delegate.</param>
        /// <param name="pfnopen">_In_ PFNOPEN - File open delegate.</param>
        /// <param name="pfnread">_In_ PFNREAD - File read delegate.</param>
        /// <param name="pfnwrite">_In_ PFNWRITE - File write delegate.</param>
        /// <param name="pfnclose">_In_ PFNCLOSE - File close delegate.</param>
        /// <param name="pfnseek">_In_ PFNSEEK - File seek delegate.</param>
        /// <param name="cpuType">_In_ int - CPU type.</param>
        /// <param name="erf">_Inout_ PERF - Error structure containing error information.</param>
        /// <returns></returns>
        [DllImport("cabinet.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern FdiContextHandle FDICreate(
            IntPtr pfnalloc,
            IntPtr pfnfree,
            IntPtr pfnopen,
            IntPtr pfnread,
            IntPtr pfnwrite,
            IntPtr pfnclose,
            IntPtr pfnseek,
            CabinetNativeApi.FdiCreateCpuType cpuType,
            FdiERF erf);

        /// <summary>
        /// Extracts files from cabinets.
        /// </summary>
        /// <param name="hfdi">_In_ HFDI - A valid FDI context handle returned by FDICreate.</param>
        /// <param name="pszCabinet">_In_ LPSTR - The name of the cabinet file.</param>
        /// <param name="pszCabPath">_In_ LPSTR - The path to the cabinet file excluding the file name.</param>
        /// <param name="flags">_In_ int - Not defined.</param>
        /// <param name="pfnfdin">_In_ PFNFDINOTIFY - Pointer to the notification callback delegate.</param>
        /// <param name="pfnfdid">_In_ PFNFDIDECRYPT - Not used.</param>
        /// <param name="pvUser">_In_opt_ void FAR * - Path string passed to the notification function.</param>
        /// <returns></returns>
        [DllImport("cabinet.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, SetLastError = true, BestFitMapping = false)]
        internal static extern bool FDICopy(
            FdiContextHandle hfdi,
            [MarshalAs(UnmanagedType.LPStr)] string pszCabinet,
            [MarshalAs(UnmanagedType.LPStr)] string pszCabPath,
            int flags,
            IntPtr pfnfdin,
            IntPtr pfnfdid,
            IntPtr pvUser);

        /// <summary>
        /// Deletes an open FDI context.
        /// </summary>
        /// <param name="hfdi">_In_ HFDI - The FDI context handle to destroy.</param>
        /// <returns></returns>
        [DllImport("cabinet.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        internal static extern bool FDIDestroy(
            IntPtr hfdi);

        #endregion
    }
}
