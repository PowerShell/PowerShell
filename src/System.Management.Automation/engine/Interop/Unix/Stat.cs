// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        /// <summary>
        /// A platform-neutral, normalized subset of the fields returned by <c>stat</c>/<c>lstat</c>.
        /// This mirrors the role of libpsl-native's <c>CommonStat</c>: it hides the per-platform
        /// (and per-architecture) differences in the raw <c>struct stat</c> layout.
        /// </summary>
        internal struct StatInfo
        {
            internal long Device;
            internal long Inode;
            internal int Mode;
            internal int UserId;
            internal int GroupId;
            internal int HardlinkCount;
            internal long Size;
            internal long BlockSize;
            internal long NumberOfBlocks;
            internal long AccessTime;
            internal long ModifiedTime;
            internal long StatusChangeTime;
        }

        /// <summary>
        /// Retrieve normalized stat information for <paramref name="path"/>.
        /// On Linux the architecture-independent <c>statx(2)</c> is used, which has a fixed struct
        /// layout across glibc/musl and all CPU architectures. On macOS the stable 64-bit inode
        /// <c>stat</c>/<c>lstat</c> variant is used.
        /// </summary>
        /// <param name="path">The filesystem path to query.</param>
        /// <param name="followSymlink">When <see langword="true"/> symlinks are followed (like <c>stat</c>); otherwise not (like <c>lstat</c>).</param>
        /// <param name="info">Receives the normalized stat information on success.</param>
        /// <returns>0 on success, -1 on failure (see <c>errno</c> via <see cref="Marshal.GetLastWin32Error()"/>).</returns>
        internal static int Stat(string path, bool followSymlink, out StatInfo info)
        {
            info = default;

            if (OperatingSystem.IsMacOS())
            {
                return DarwinStat(path, followSymlink, ref info);
            }

            return LinuxStat(path, followSymlink, ref info);
        }

        #region Linux (statx)

        // statx() constants. These are architecture-independent.
        private const int AT_FDCWD = -100;
        private const int AT_SYMLINK_NOFOLLOW = 0x100;
        private const uint STATX_BASIC_STATS = 0x000007ffU;

        [StructLayout(LayoutKind.Sequential)]
        private struct StatxTimestamp
        {
            internal long Seconds;
            internal uint Nanoseconds;
            internal int Reserved;
        }

        // Layout of 'struct statx' (linux/stat.h). It is a fixed 256-byte structure that is the
        // same on every Linux architecture, which is why statx is preferred over the raw stat call.
        [StructLayout(LayoutKind.Sequential)]
        private struct StatxBuffer
        {
            internal uint Mask;
            internal uint Blksize;
            internal ulong Attributes;
            internal uint Nlink;
            internal uint Uid;
            internal uint Gid;
            internal ushort Mode;
            internal ushort Spare0;
            internal ulong Ino;
            internal ulong Size;
            internal ulong Blocks;
            internal ulong AttributesMask;
            internal StatxTimestamp Atime;
            internal StatxTimestamp Btime;
            internal StatxTimestamp Ctime;
            internal StatxTimestamp Mtime;
            internal uint RdevMajor;
            internal uint RdevMinor;
            internal uint DevMajor;
            internal uint DevMinor;
            internal ulong MntId;

            // Padding so the managed struct is at least sizeof(struct statx) == 256 bytes; the
            // kernel may write up to that size. These fields are intentionally unused.
            internal ulong Spare2_0;
            internal ulong Spare2_1;
            internal ulong Spare2_2;
            internal ulong Spare2_3;
            internal ulong Spare2_4;
            internal ulong Spare2_5;
            internal ulong Spare2_6;
            internal ulong Spare2_7;
            internal ulong Spare2_8;
            internal ulong Spare2_9;
            internal ulong Spare2_10;
            internal ulong Spare2_11;
            internal ulong Spare2_12;
        }

        [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int Statx(int dirfd, string pathname, int flags, uint mask, out StatxBuffer buffer);

        private static int LinuxStat(string path, bool followSymlink, ref StatInfo info)
        {
            int flags = followSymlink ? 0 : AT_SYMLINK_NOFOLLOW;
            int ret = Statx(AT_FDCWD, path, flags, STATX_BASIC_STATS, out StatxBuffer buffer);
            if (ret != 0)
            {
                return ret;
            }

            info.Device = MakeDev(buffer.DevMajor, buffer.DevMinor);
            info.Inode = (long)buffer.Ino;
            info.Mode = buffer.Mode;
            info.UserId = (int)buffer.Uid;
            info.GroupId = (int)buffer.Gid;
            info.HardlinkCount = (int)buffer.Nlink;
            info.Size = (long)buffer.Size;
            info.BlockSize = buffer.Blksize;
            info.NumberOfBlocks = (long)buffer.Blocks;
            info.AccessTime = buffer.Atime.Seconds;
            info.ModifiedTime = buffer.Mtime.Seconds;
            info.StatusChangeTime = buffer.Ctime.Seconds;
            return 0;
        }

        // Reconstruct a dev_t from the major/minor pair using the glibc gnu_dev_makedev encoding.
        // The exact encoding only needs to be self-consistent for our callers (equality checks and
        // opaque device ids), and matching glibc keeps the value meaningful.
        private static long MakeDev(uint major, uint minor)
        {
            ulong dev = ((ulong)(major & 0x00000fffU)) << 8;
            dev |= ((ulong)(major & 0xfffff000U)) << 32;
            dev |= (ulong)(minor & 0x000000ffU);
            dev |= ((ulong)(minor & 0xffffff00U)) << 12;
            return (long)dev;
        }

        #endregion

        #region macOS (stat/lstat with 64-bit inode)

        [StructLayout(LayoutKind.Sequential)]
        private struct DarwinTimespec
        {
            internal long Seconds;
            internal long Nanoseconds;
        }

        // Layout of macOS 'struct stat' when __DARWIN_64_BIT_INO_T is in effect, which is the
        // default for 64-bit builds. This layout is identical on x86_64 and arm64.
        [StructLayout(LayoutKind.Sequential)]
        private struct DarwinStatBuffer
        {
            internal int Dev;
            internal ushort Mode;
            internal ushort Nlink;
            internal ulong Ino;
            internal uint Uid;
            internal uint Gid;
            internal int Rdev;
            internal DarwinTimespec Atime;
            internal DarwinTimespec Mtime;
            internal DarwinTimespec Ctime;
            internal DarwinTimespec Btime;
            internal long Size;
            internal long Blocks;
            internal int Blksize;
            internal uint Flags;
            internal uint Gen;
            internal int Lspare;
            internal long Qspare0;
            internal long Qspare1;
        }

        // On x86_64 macOS the 64-bit inode variants are exported with the "$INODE64" suffix; on
        // arm64 the base symbols already use the 64-bit inode layout.
        [LibraryImport("libc", EntryPoint = "stat$INODE64", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int DarwinStatInode64(string path, out DarwinStatBuffer buffer);

        [LibraryImport("libc", EntryPoint = "lstat$INODE64", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int DarwinLStatInode64(string path, out DarwinStatBuffer buffer);

        [LibraryImport("libc", EntryPoint = "stat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int DarwinStatDefault(string path, out DarwinStatBuffer buffer);

        [LibraryImport("libc", EntryPoint = "lstat", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int DarwinLStatDefault(string path, out DarwinStatBuffer buffer);

        private static int DarwinStat(string path, bool followSymlink, ref StatInfo info)
        {
            int ret;
            DarwinStatBuffer buffer;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                ret = followSymlink
                    ? DarwinStatInode64(path, out buffer)
                    : DarwinLStatInode64(path, out buffer);
            }
            else
            {
                ret = followSymlink
                    ? DarwinStatDefault(path, out buffer)
                    : DarwinLStatDefault(path, out buffer);
            }

            if (ret != 0)
            {
                return ret;
            }

            info.Device = unchecked((uint)buffer.Dev);
            info.Inode = (long)buffer.Ino;
            info.Mode = buffer.Mode;
            info.UserId = (int)buffer.Uid;
            info.GroupId = (int)buffer.Gid;
            info.HardlinkCount = buffer.Nlink;
            info.Size = buffer.Size;
            info.BlockSize = buffer.Blksize;
            info.NumberOfBlocks = buffer.Blocks;
            info.AccessTime = buffer.Atime.Seconds;
            info.ModifiedTime = buffer.Mtime.Seconds;
            info.StatusChangeTime = buffer.Ctime.Seconds;
            return 0;
        }

        #endregion
    }
}
