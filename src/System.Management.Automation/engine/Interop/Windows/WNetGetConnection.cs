// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Buffers;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        private static bool s_WNetApiNotAvailable;

        [LibraryImport("mpr.dll", EntryPoint = "WNetGetConnectionW")]
        internal static partial int WNetGetConnection(ReadOnlySpan<ushort> localName, Span<ushort> remoteName, ref uint remoteNameLength);

        internal static int GetUNCForNetworkDrive(char drive, out string? uncPath)
        {
            uncPath = null;
            if (s_WNetApiNotAvailable)
            {
                return ERROR_NOT_SUPPORTED;
            }

            uint bufferSize = MAX_PATH;

#if DEBUG
            // In Debug mode buffer size is initially set to 3 and if additional buffer is required, the
            // required buffer size is allocated and the WNetGetConnection API is executed with the newly
            // allocated buffer size.
            bufferSize = 3;
#endif

            // TODO: change ushort with char after LibraryImport will support 'ref char'
            // without applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute'
            // to the assembly.
            ReadOnlySpan<ushort> driveName = stackalloc ushort[] { drive, ':', '\0' };
            Span<ushort> uncBuffer = stackalloc ushort[(int)bufferSize];
            int errorCode = ERROR_NO_NETWORK;

            try
            {
                errorCode = WNetGetConnection(driveName, uncBuffer, ref bufferSize);
            }
            catch (System.DllNotFoundException)
            {
                s_WNetApiNotAvailable = true;
                return ERROR_NOT_SUPPORTED;
            }

            if (errorCode == ERROR_SUCCESS)
            {
                uncPath = ToString(uncBuffer.ToArray());
            }
            else if (errorCode == ERROR_MORE_DATA)
            {
                ushort[]? rentedArray = null;
                try
                {
                    uncBuffer = rentedArray = ArrayPool<ushort>.Shared.Rent((int)bufferSize);
                    errorCode = WNetGetConnection(driveName, uncBuffer, ref bufferSize);

                    if (errorCode == ERROR_SUCCESS)
                    {
                        uncPath = ToString(uncBuffer.ToArray());
                    }
                }
                finally
                {
                    if (rentedArray is not null)
                    {
                        ArrayPool<ushort>.Shared.Return(rentedArray);
                    }
                }
            }

            return errorCode;
        }

        private static string ToString(ushort[] buffer)
        {
            // trim trailing null character
            byte[] asBytes = new byte[(buffer.Length - 1) * sizeof(ushort)];
            Buffer.BlockCopy(buffer, 0, asBytes, 0, asBytes.Length);
            return System.Text.Encoding.Unicode.GetString(asBytes);
        }
    }
}
