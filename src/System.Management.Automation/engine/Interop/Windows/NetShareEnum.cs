// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        internal const uint MAX_PREFERRED_LENGTH = uint.MaxValue;
        internal const uint STYPE_DISKTREE = 0u;
        internal const uint STYPE_MASK = 255u;

        [LibraryImport("Netapi32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial uint NetShareEnum(
            string? servername,
            uint level,
            out byte* bufptr,
            uint prefmaxlen,
            out uint entriesread,
            out uint totalentries,
            uint* resume_handle);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SHARE_INFO_1
        {
            public ushort* netname;
            public uint type;
            public ushort* remark;
        }

        internal static uint NetShareEnum<T>(string? servername, out T* pShareInfo, out int count) where T : unmanaged
        {
            uint level = (uint)GetLevelFromStructure<T>();
            uint result = NetShareEnum(servername, level, out byte* pBuffer, MAX_PREFERRED_LENGTH, out uint entriesRead, out _, null);
            pShareInfo = (T*)pBuffer;
            count = (int)entriesRead;
            return result;
        }

        private static int GetLevelFromStructure<T>()
        {
            if (typeof(T) == typeof(SHARE_INFO_1))
                return 1;

            throw new NotSupportedException();
        }
    }
}
