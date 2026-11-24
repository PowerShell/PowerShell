// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        // getpwuid_r / getgrgid_r return their error number directly; ERANGE (34) signals that the
        // scratch buffer was too small. This value is identical on Linux and macOS.
        private const int ERANGE = 34;

        // Large enough to hold 'struct passwd' or 'struct group' on any supported platform; the
        // libc routine writes the whole structure so this must not be undersized. Only the first
        // member (pw_name / gr_name, a char*) is read, which is at offset 0 on every Unix platform.
        private const int RecordBufferSize = 256;

        [LibraryImport("libc", EntryPoint = "getpwuid_r")]
        private static partial int GetPwUidR(uint uid, IntPtr pwd, IntPtr buffer, nuint bufLen, out IntPtr result);

        [LibraryImport("libc", EntryPoint = "getgrgid_r")]
        private static partial int GetGrGidR(uint gid, IntPtr grp, IntPtr buffer, nuint bufLen, out IntPtr result);

        /// <summary>Resolve a user id to a user name using <c>getpwuid_r</c>.</summary>
        /// <param name="uid">The user id to look up.</param>
        /// <returns>The user name, or <see langword="null"/> if it could not be resolved.</returns>
        internal static string? GetPwUid(int uid)
        {
            return LookupName(isUser: true, id: unchecked((uint)uid));
        }

        /// <summary>Resolve a group id to a group name using <c>getgrgid_r</c>.</summary>
        /// <param name="gid">The group id to look up.</param>
        /// <returns>The group name, or <see langword="null"/> if it could not be resolved.</returns>
        internal static string? GetGrGid(int gid)
        {
            return LookupName(isUser: false, id: unchecked((uint)gid));
        }

        private static string? LookupName(bool isUser, uint id)
        {
            const int MaxBufLen = 1024 * 1024;
            int bufLen = 1024;

            IntPtr record = Marshal.AllocHGlobal(RecordBufferSize);
            try
            {
                while (true)
                {
                    IntPtr scratch = Marshal.AllocHGlobal(bufLen);
                    try
                    {
                        // Zero the record so a stale name pointer is never dereferenced.
                        for (int offset = 0; offset < RecordBufferSize; offset += IntPtr.Size)
                        {
                            Marshal.WriteIntPtr(record, offset, IntPtr.Zero);
                        }

                        IntPtr result;
                        int ret = isUser
                            ? GetPwUidR(id, record, scratch, (nuint)bufLen, out result)
                            : GetGrGidR(id, record, scratch, (nuint)bufLen, out result);

                        if (ret == ERANGE && bufLen < MaxBufLen)
                        {
                            bufLen *= 2;
                            continue;
                        }

                        if (ret != 0 || result == IntPtr.Zero)
                        {
                            return null;
                        }

                        // pw_name / gr_name is the first member (char*) of the structure.
                        IntPtr namePtr = Marshal.ReadIntPtr(record);
                        return Marshal.PtrToStringUTF8(namePtr);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(scratch);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(record);
            }
        }
    }
}
