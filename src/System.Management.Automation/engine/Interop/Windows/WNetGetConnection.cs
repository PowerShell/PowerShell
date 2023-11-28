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

        [LibraryImport("mpr.dll", EntryPoint = "WNetGetConnectionW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int WNetGetConnection(ReadOnlySpan<char> localName, Span<char> remoteName, ref uint remoteNameLength);

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

            ReadOnlySpan<char> driveName = stackalloc char[] { drive, ':', '\0' };
            Span<char> uncBuffer = stackalloc char[(int)bufferSize];
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
                // exclude null terminator
                uncPath = uncBuffer.Slice(0, (int)bufferSize - 1).ToString();
            }
            else if (errorCode == ERROR_MORE_DATA)
            {
                char[]? rentedArray = null;
                try
                {
                    uncBuffer = rentedArray = ArrayPool<char>.Shared.Rent((int)bufferSize);
                    errorCode = WNetGetConnection(driveName, uncBuffer, ref bufferSize);

                    if (errorCode == ERROR_SUCCESS)
                    {
                        // exclude null terminator
                        uncPath = uncBuffer.Slice(0, (int)bufferSize - 1).ToString();
                    }
                }
                finally
                {
                    if (rentedArray is not null)
                    {
                        ArrayPool<char>.Shared.Return(rentedArray);
                    }
                }
            }

            return errorCode;
        }
    }
}
