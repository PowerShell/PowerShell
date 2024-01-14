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

            ReadOnlySpan<char> driveName = stackalloc char[] { drive, ':', '\0' };
            Span<char> uncBuffer = stackalloc char[(int)bufferSize];

            char[]? rentedArray = null;
            try
            {
                while (true)
                {
                    int errorCode;
                    try
                    {
                        errorCode = WNetGetConnection(driveName, uncBuffer, ref bufferSize);
                    }
                    catch (System.DllNotFoundException)
                    {
                        s_WNetApiNotAvailable = true;
                        return ERROR_NOT_SUPPORTED;
                    }

                    if (errorCode == ERROR_MORE_DATA)
                    {
                        uncBuffer = rentedArray = ArrayPool<char>.Shared.Rent((int)bufferSize);
                    }
                    else
                    {
                        if (errorCode == ERROR_SUCCESS)
                        {
                            // Cannot rely on bufferSize as it's only set if
                            // the first call ended with ERROR_MORE_DATA,
                            // instead slice at the null terminator.
                            int nullIdx = uncBuffer.IndexOf('\0');
                            uncPath = uncBuffer.Slice(
                                0,
                                nullIdx == -1 ? uncBuffer.Length : nullIdx).ToString();
                        }

                        return errorCode;
                    }
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
    }
}
