// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if !UNIX
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        // The FindExecutable API is defined in shellapi.h as
        // SHSTDAPI_(HINSTANCE) FindExecutableW(LPCWSTR lpFile, LPCWSTR lpDirectory, __out_ecount(MAX_PATH) LPWSTR lpResult);
        // HINSTANCE is void* so we need to use IntPtr (nint) as API return value.
        [LibraryImport("shell32.dll", EntryPoint = "FindExecutableW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial nint FindExecutableW(string fileName, string directoryPath, char* pathFound);

        internal static string? FindExecutable(string filename)
        {
            string? result = null;

            // HINSTANCE == PVOID == nint
            nint resultCode = 0;

            Span<char> buffer = stackalloc char[MAX_PATH];
            unsafe
            {
                fixed (char* lpBuffer = buffer)
                {
                    resultCode = FindExecutableW(filename, string.Empty, lpBuffer);

                    // If FindExecutable returns a result > 32, then it succeeded
                    // and we return the string that was found, otherwise we
                    // return null.
                    if (resultCode > 32)
                    {
                        result = Marshal.PtrToStringUni((IntPtr)lpBuffer);
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
#endif
