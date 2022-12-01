// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Windows
    {
        [LibraryImport("api-ms-win-core-file-l2-1-0.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateHardLinkW(string lpFileName, string lpExistingFileName, void* lpSecurityAttributes);

        internal static bool CreateHardLink(string fileName, string existingFileName)
        {
            return CreateHardLinkW(fileName, existingFileName, null);
        }
    }
}
