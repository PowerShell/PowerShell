// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Management.Automation.Internal;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        private static bool s_WNetApiNotAvailable;

        [LibraryImport("mpr.dll", EntryPoint = "WNetGetConnectionW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int WNetGetConnection(ReadOnlySpan<char> localName, Span<char> remoteName, ref int remoteNameLength);

        internal static int GetUNCForNetworkDrive(char drive, out string? uncPath)
        {
            uncPath = null;
            if (s_WNetApiNotAvailable)
            {
                return ERROR_NOT_SUPPORTED;
            }

            ReadOnlySpan<char> driveName = [drive, ':', '\0'];
            int bufferSize = MAX_PATH;
            Span<char> uncBuffer = stackalloc char[MAX_PATH];
            if (InternalTestHooks.WNetGetConnectionBufferSize > 0 && InternalTestHooks.WNetGetConnectionBufferSize <= MAX_PATH)
            {
                bufferSize = InternalTestHooks.WNetGetConnectionBufferSize;
                uncBuffer = uncBuffer.Slice(0, bufferSize);
            }

            char[]? rentedArray = null;
            while (true)
            {
                int errorCode;
                try
                {
                    try
                    {
                        errorCode = WNetGetConnection(driveName, uncBuffer, ref bufferSize);
                    }
                    catch (DllNotFoundException)
                    {
                        s_WNetApiNotAvailable = true;
                        return ERROR_NOT_SUPPORTED;
                    }

                    if (errorCode == ERROR_SUCCESS)
                    {
                        // Cannot rely on bufferSize as it's only set if
                        // the first call ended with ERROR_MORE_DATA,
                        // instead slice at the null terminator.
                        unsafe
                        {
                            fixed (char* uncBufferPtr = uncBuffer)
                            {
                                uncPath = new string(uncBufferPtr);
                            }
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

                if (errorCode == ERROR_MORE_DATA)
                {
                    uncBuffer = rentedArray = ArrayPool<char>.Shared.Rent(bufferSize);
                }
                else
                {
                    return errorCode;
                }
            }
        }
    }
}
